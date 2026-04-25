/*---------------------------
File: DialogueEditorWindow.cs
Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
//---------------------------------
using PolyQuest.Dialogues;

namespace PolyQuest.Edit
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Provides a custom Unity Editor window for visually editing Dialogue assets and nodes.  *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Displays and manages a node-based editor for Dialogue and DialogueNode assets.        *
     *      - Allows creation, deletion, linking, and editing of dialogue nodes via GUI.            *
     *      - Supports dragging nodes and panning the canvas for intuitive navigation.              *
     *      - Draws connections between parent and child nodes using Bezier curves.                 *
     *      - Integrates with Unity's selection and asset systems for seamless workflow.            *
     *      - Applies custom styles for NPC and player nodes for visual clarity.                    *
     *      - Supports marquee (rubber-band) selection and group dragging of nodes.                 *
     * -------------------------------------------------------------------------------------------- */
    public class DialogueEditorWindow : EditorWindow
    {
        [NonSerialized] private DialogueNode m_draggingNode;
        [NonSerialized] private DialogueNode m_createdNode;
        [NonSerialized] private DialogueNode m_deletedNode;
        [NonSerialized] private DialogueNode m_linkedParentNode;
        [NonSerialized] private GUIStyle m_NPCNodeStyle;
        [NonSerialized] private GUIStyle m_playerNodeStyle;
        [NonSerialized] private Vector2 m_draggingOffset;
        [NonSerialized] private Vector2 m_draggingCanvasOffset;
        [NonSerialized] private bool m_draggingCanvas;

        // Marquee selection
        [NonSerialized] private bool m_isSelectionDragging;
        [NonSerialized] private Vector2 m_selectionDragStart;
        [NonSerialized] private Rect m_selectionRect;

        // Group dragging
        [NonSerialized] private HashSet<DialogueNode> m_selectedNodes = new();
        [NonSerialized] private Dictionary<DialogueNode, Vector2> m_groupDragOffsets = new();
        [NonSerialized] private bool m_isDraggingGroup;

        private Dialogue m_selectedDialogue;
        private Vector2 m_scrollPosition;
        private const float kCanvasWidth = 4000f;
        private const float kCanvasHeight = 4000f;
        private const float kBackgroundSize = 50f;  // Size for background m_texture

        /*-----------------------------------------------------
        | --- ShowEditorWindow: Creates the Editor Window --- |
        -----------------------------------------------------*/
        [MenuItem("Window/Dialogue Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogueEditorWindow), false, "Dialogue Editor");
        }

        /*----------------------------------------------
        | --- OnOpenAsset: Opens the Editor Window --- |
        ----------------------------------------------*/
        [OnOpenAssetAttribute(1)]
        public static bool OnOpenAsset(int ID, int line)
        {
            if (EditorUtility.EntityIdToObject(ID) is Dialogue dialogue)
            {
                ShowEditorWindow();
                return true;
            }
            return false;
        }

        /*----------------------------------------------------------------
        | --- OnEnable: Adds Selection Listener to the Editor Window --- |
        ----------------------------------------------------------------*/
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;

            /* --- NPC Node Style --- */
            m_NPCNodeStyle = new GUIStyle();
            m_NPCNodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            m_NPCNodeStyle.normal.textColor = Color.white;
            m_NPCNodeStyle.padding = new RectOffset(20, 20, 20, 20);
            m_NPCNodeStyle.border = new RectOffset(12, 12, 12, 12);

            /* --- Player Node Style --- */
            m_playerNodeStyle = new GUIStyle();
            m_playerNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
            m_playerNodeStyle.normal.textColor = Color.white;
            m_playerNodeStyle.padding = new RectOffset(20, 20, 20, 20);
            m_playerNodeStyle.border = new RectOffset(12, 12, 12, 12);
        }

        /*---------------------------------------------------------------------------
        | --- OnSelectionChanged: Handles Selection Change of the Dialogue Node --- |
        ---------------------------------------------------------------------------*/
        private void OnSelectionChanged()
        {
            if (Selection.activeObject is Dialogue newDialogue)
            {
                m_selectedDialogue = newDialogue;
                Repaint();
            }
        }

        /*----------------------------------------
        | --- OnGUI: Draws the Editor Window --- |
        ----------------------------------------*/
        private void OnGUI()
        {
            if (m_selectedDialogue == null)
            {
                EditorGUILayout.LabelField("No Dialogue Selected.");
            }
            else
            {
                ProcessEvents();

                // Begin an automatically laid out scrollview
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

                // Draw the background m_texture for the canvas
                Rect canvas = GUILayoutUtility.GetRect(kCanvasWidth, kCanvasHeight);
                Texture2D backgroundTexture = Resources.Load("Editor Window") as Texture2D;
                Rect textureCoords = new(0, 0, kCanvasWidth / kBackgroundSize, kCanvasHeight / kBackgroundSize);
                GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, textureCoords);

                // Cache the dialogue nodes to avoid getting them twice
                var dialogueNodes = m_selectedDialogue.Nodes;

                // Draw the connections first to avoid drawing over the nodes
                foreach (DialogueNode node in dialogueNodes)
                {
                    DrawConnections(node);
                }

                foreach (DialogueNode node in dialogueNodes)
                {
                    DrawDialogueNode(node);
                }

                EditorGUILayout.EndScrollView();

                // Draw the marquee selection box on top of everything (window space)
                DrawSelectionBox();

                // Handle the creation of a node
                if (m_createdNode != null)
                {
                    m_selectedDialogue.CreateNode(m_createdNode);
                    m_createdNode = null;
                }

                // Handle the deletion of a node
                if (m_deletedNode != null)
                {
                    m_selectedDialogue.DeleteNode(m_deletedNode);
                    m_deletedNode = null;
                }
            }
        }

        /*---------------------------------------------------------------------------
        | --- ProcessEvents: Handles Mouse Dragging Events in the Editor Window --- |
        ---------------------------------------------------------------------------*/
        private void ProcessEvents()
        {
            // Handle mouse down events
            if (Event.current.type == EventType.MouseDown)
            {
                // Left Mouse Button
                if (Event.current.button == 0)
                {
                    DialogueNode hitNode = GetNodeAtCursor(Event.current.mousePosition + m_scrollPosition);

                    if (hitNode != null)
                    {
                        if (m_selectedNodes.Contains(hitNode))
                        {
                            // Clicked an already-selected node → start group drag
                            m_isDraggingGroup = true;
                            m_groupDragOffsets.Clear();
                            foreach (DialogueNode selectedNode in m_selectedNodes)
                            {
                                m_groupDragOffsets[selectedNode] = selectedNode.Rect.position - Event.current.mousePosition;
                            }
                        }
                        else
                        {
                            // Clicked an unselected node → single-node drag (clears any prior selection)
                            m_selectedNodes.Clear();
                            m_draggingNode = hitNode;
                            m_draggingOffset = m_draggingNode.Rect.position - Event.current.mousePosition;
                            Selection.activeObject = m_draggingNode;
                        }
                    }
                    else
                    {
                        // Clicked empty canvas → begin marquee selection
                        m_selectedNodes.Clear();
                        m_draggingNode = null;
                        m_isSelectionDragging = true;
                        m_selectionDragStart = Event.current.mousePosition;
                        m_selectionRect = new Rect(m_selectionDragStart, Vector2.zero);
                        Selection.activeObject = m_selectedDialogue;
                    }
                }
                // Middle Mouse Button -> Drag Canvas
                else if (Event.current.button == 2)
                {
                    m_draggingCanvas = true;
                    m_draggingCanvasOffset = Event.current.mousePosition + m_scrollPosition;
                }
            }
            // Handle mouse drag events
            else if (Event.current.type == EventType.MouseDrag)
            {
                // Left Mouse Button → update whichever drag mode is active
                if (Event.current.button == 0)
                {
                    if (m_isDraggingGroup)
                    {
                        // Move all nodes in the selection together
                        foreach (DialogueNode selectedNode in m_selectedNodes)
                        {
                            selectedNode.SetPosition(Event.current.mousePosition + m_groupDragOffsets[selectedNode]);
                        }
                        GUI.changed = true;
                    }
                    else if (m_draggingNode != null)
                    {
                        // Move single node
                        m_draggingNode.SetPosition(Event.current.mousePosition + m_draggingOffset);
                        GUI.changed = true;
                    }
                    else if (m_isSelectionDragging)
                    {
                        // Expand the marquee rectangle as the mouse moves
                        m_selectionRect = GetRectFromPoints(m_selectionDragStart, Event.current.mousePosition);
                        GUI.changed = true;
                    }
                }
                // Middle Mouse Button -> Drag Canvas
                else if (Event.current.button == 2 && m_draggingCanvas)
                {
                    m_scrollPosition = m_draggingCanvasOffset - Event.current.mousePosition;
                    GUI.changed = true;
                }
            }
            // Handle mouse up events
            else if (Event.current.type == EventType.MouseUp)
            {
                // Left Mouse Button
                if (Event.current.button == 0)
                {
                    if (m_isDraggingGroup)
                    {
                        // Stop group drag
                        m_isDraggingGroup = false;
                        m_groupDragOffsets.Clear();
                    }
                    else if (m_draggingNode != null)
                    {
                        // Stop single-node drag
                        m_draggingNode = null;
                    }
                    else if (m_isSelectionDragging)
                    {
                        // Finalise marquee — collect all nodes whose rects overlap the selection box
                        m_isSelectionDragging = false;
                        m_selectedNodes.Clear();

                        // Convert the window-space selection rect to canvas (scroll) space
                        Rect canvasSelectionRect = new Rect(
                            m_selectionRect.position + m_scrollPosition,
                            m_selectionRect.size
                        );

                        foreach (DialogueNode node in m_selectedDialogue.Nodes)
                        {
                            if (canvasSelectionRect.Overlaps(node.Rect))
                            {
                                m_selectedNodes.Add(node);
                            }
                        }

                        m_selectionRect = default;
                        GUI.changed = true;
                    }
                }
                // Middle Mouse Button -> Stop Dragging Canvas
                else if (Event.current.button == 2 && m_draggingCanvas)
                {
                    m_draggingCanvas = false;
                }
            }
        }

        /*-----------------------------------------------------------------
        | --- DrawSelectionBox: Renders the Marquee Rectangle Overlay --- |
        -----------------------------------------------------------------*/
        private void DrawSelectionBox()
        {
            if (!m_isSelectionDragging || m_selectionRect.size == Vector2.zero) return;

            // Semi-transparent fill
            EditorGUI.DrawRect(m_selectionRect, new Color(0.2f, 0.6f, 1f, 0.15f));

            // Solid border
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Vector3 tl = new Vector3(m_selectionRect.xMin, m_selectionRect.yMin);
            Vector3 tr = new Vector3(m_selectionRect.xMax, m_selectionRect.yMin);
            Vector3 br = new Vector3(m_selectionRect.xMax, m_selectionRect.yMax);
            Vector3 bl = new Vector3(m_selectionRect.xMin, m_selectionRect.yMax);
            Handles.DrawLines(new Vector3[] { tl, tr, tr, br, br, bl, bl, tl });

            GUI.changed = true;
        }

        /*------------------------------------------------------------------------
        | --- DrawDialogueNode: Draws the Dialogue Node in the Editor Window --- |
        ------------------------------------------------------------------------*/
        private void DrawDialogueNode(DialogueNode node)
        {
            // Determine the style for the node based on whether it's a player speaking node or not
            GUIStyle nodeStyle = m_NPCNodeStyle;
            if (node.IsPlayerSpeaking)
            {
                nodeStyle = m_playerNodeStyle;
            }

            // Highlight selected nodes with a yellow tint
            if (m_selectedNodes.Contains(node))
            {
                GUI.color = new Color(1f, 0.92f, 0.4f);
            }

            // Begin a new area for the GUI layout
            GUILayout.BeginArea(node.Rect, nodeStyle);

            GUI.color = Color.white;

            // Draw the node's text field for the current text
            node.SetText(EditorGUILayout.TextField(node.Text));

            // Set the Layout in a Horizontal Orientation
            GUILayout.BeginHorizontal();

            // Button -> Delete Node
            if (GUILayout.Button("x"))
            {
                m_deletedNode = node;
            }

            // Button -> Linking
            if (m_linkedParentNode == null)
            {
                // Set Connection
                if (GUILayout.Button("Link"))
                {
                    m_linkedParentNode = node;
                }
            }
            else if (m_linkedParentNode == node)
            {
                // Cancel Connection
                if (GUILayout.Button("Cancel"))
                {
                    m_linkedParentNode = null;
                }
            }
            else if (m_linkedParentNode.Children.Contains(node.name))
            {
                // Unlink Connection
                if (GUILayout.Button("Unlink"))
                {
                    m_linkedParentNode.RemoveChild(node.name);
                    m_linkedParentNode = null;
                }
            }
            else
            {
                // Connect to Child
                if (GUILayout.Button("Child"))
                {
                    m_linkedParentNode.AddChild(node.name);
                    m_linkedParentNode = null;
                }
            }

            // Button -> Create Node
            if (GUILayout.Button("+"))
            {
                m_createdNode = node;
            }

            GUILayout.EndHorizontal();

            // End the area for the GUI layout
            GUILayout.EndArea();
        }

        /*--------------------------------------------------------------------------------------------
        | --- DrawConnections: Draws the Connections between Dialogue Nodes in the Editor Window --- |
        --------------------------------------------------------------------------------------------*/
        private void DrawConnections(DialogueNode node)
        {
            // Define the starting point of the connection line as the right-center of the current node's rectangle
            Vector3 start = new Vector2(node.Rect.xMax, node.Rect.center.y);

            // Iterate through all child nodes of the current node to draw connections
            foreach (DialogueNode childNode in m_selectedDialogue.GetAllChildren(node))
            {
                // Define the ending point of the connection line as the left-center of the child node's rectangle
                Vector3 end = new Vector2(childNode.Rect.xMin, childNode.Rect.center.y);

                // Calculate an offset vector to create a smooth curve for the connection
                Vector3 connectionOffset = end - start;

                // Ensure the offset only affects the horizontal direction (x-axis)
                connectionOffset.y = 0;

                // Scale the horizontal offset to control the curvature of the connection
                connectionOffset.x *= 0.8f;

                // Draw a Bezier curve between the start and end points with the calculated offset
                Handles.DrawBezier(
                    start,                          // Starting point of the curve
                    end,                            // Ending point of the curve
                    start + connectionOffset,       // Control point for the curve near the start
                    end - connectionOffset,         // Control point for the curve near the end
                    Color.cyan,                     // Color of the curve
                    null,                           // Texture for the curve (null means no m_texture)
                    5f                              // Thickness of the curve
                );
            }
        }

        /*---------------------------------------------------------------------------
        | --- GetNodeAtCursor: Returns the Dialogue Node at the Cursor Position --- |
        ---------------------------------------------------------------------------*/
        private DialogueNode GetNodeAtCursor(Vector2 cursorPosition)
        {
            DialogueNode foundNode = null;

            foreach (DialogueNode node in m_selectedDialogue.Nodes)
            {
                if (node.Rect.Contains(cursorPosition))
                {
                    foundNode = node;
                }
            }

            return foundNode;
        }

        /*--------------------------------------------------------------------------
        | --- GetRectFromPoints: Builds a valid Rect from two arbitrary points --- |
        --------------------------------------------------------------------------*/
        private static Rect GetRectFromPoints(Vector2 a, Vector2 b)
        {
            return new Rect(
                Mathf.Min(a.x, b.x),
                Mathf.Min(a.y, b.y),
                Mathf.Abs(a.x - b.x),
                Mathf.Abs(a.y - b.y)
            );
        }
    }
}