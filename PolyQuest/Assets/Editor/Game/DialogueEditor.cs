using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace PolyQuest.Edit
{
    public class DialogueEditor : EditorWindow
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
            GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
        }

        /*----------------------------------------------
        | --- OnOpenAsset: Opens the Editor Window --- |
        ----------------------------------------------*/
        [OnOpenAssetAttribute(1)]
        public static bool OnOpenAsset(int ID, int line)
        {
            if (EditorUtility.InstanceIDToObject(ID) is Dialogue dialogue)
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
                var dialogueNodes = m_selectedDialogue.GetAllNodes();

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
                // Left Mouse Button -> Select Node
                if (Event.current.button == 0)
                {
                    m_draggingNode = GetNodeAtCursor(Event.current.mousePosition + m_scrollPosition);
                    if (m_draggingNode != null)
                    {
                        m_draggingOffset = m_draggingNode.GetRect().position - Event.current.mousePosition;
                        Selection.activeObject = m_draggingNode;
                    }
                    else
                    {
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
                // Left Mouse Button -> Drag Node
                if (Event.current.button == 0 && m_draggingNode != null)
                {
                    m_draggingNode.SetPosition(Event.current.mousePosition + m_draggingOffset);
                    GUI.changed = true;
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
                // Left Mouse Button -> Stop Dragging Node
                if (Event.current.button == 0 && m_draggingNode != null)
                {
                    m_draggingNode = null;
                }
                // Middle Mouse Button -> Stop Dragging Canvas
                else if (Event.current.button == 2 && m_draggingCanvas)
                {
                    m_draggingCanvas = false;
                }
            }
        }

        /*------------------------------------------------------------------------
        | --- DrawDialogueNode: Draws the Dialogue Node in the Editor Window --- |
        ------------------------------------------------------------------------*/
        private void DrawDialogueNode(DialogueNode node)
        {
            // Determine the style for the node based on whether it's a player speaking node or not
            GUIStyle nodeStyle = m_NPCNodeStyle;
            if (node.IsPlayerSpeaking())
            {
                nodeStyle = m_playerNodeStyle;
            }

            // Begin a new area for the GUI layout
            GUILayout.BeginArea(node.GetRect(), nodeStyle);

            // Draw the node's text field for the current text
            node.SetText(EditorGUILayout.TextField(node.GetText()));

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
            else if (m_linkedParentNode.GetChildren().Contains(node.name))
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
            Vector3 start = new Vector2(node.GetRect().xMax, node.GetRect().center.y);

            // Iterate through all child nodes of the current node to draw connections
            foreach (DialogueNode childNode in m_selectedDialogue.GetAllChildren(node))
            {
                // Define the ending point of the connection line as the left-center of the child node's rectangle
                Vector3 end = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);

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

            foreach (DialogueNode node in m_selectedDialogue.GetAllNodes())
            {
                if (node.GetRect().Contains(cursorPosition))
                {
                    foundNode = node;
                }
            }

            return foundNode;
        }
    }
}