using PolyQuest.Tools;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Dialogues
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents a dialogue asset containing a tree of dialogue nodes for conversations.     *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Stores and manages a collection of DialogueNode objects forming a dialogue tree.      *
     *      - Provides access to all nodes, root node, and child nodes for dialogue traversal.      *
     *      - Supports editor operations for creating, deleting, and organizing dialogue nodes.     *
     *      - Ensures a valid root node exists and maintains node relationships.                    *
     *      - Handles serialization and asset management for dialogue editing and saving.           *
     * -------------------------------------------------------------------------------------------- */
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "PolyQuest/Dialogue", order = 0)]
    public class Dialogue : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<DialogueNode> m_nodes = new();
        [SerializeField] private Vector2 m_newNodeOffset = new(300, 0);

        private readonly Dictionary<string, DialogueNode> m_nodeDictionary = new();

        public IEnumerable<DialogueNode> Nodes => m_nodes;

#if UNITY_EDITOR
        /*------------------------------------------------------------------------
        | --- EnsureRootNode: Ensures that the Dialogue Tree has a Root Node --- |
        ------------------------------------------------------------------------*/
        private void EnsureRootNode()
        {
            if (m_nodes.Count == 0)
            {
                InstantiateNode(null);       // Creates a node with no parent (links) to ensure a valid Root Node
            }
        }
#endif

        /*--------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled or active --- |
        --------------------------------------------------------------------*/
        private void OnEnable()
        {
#if UNITY_EDITOR
            EnsureRootNode();
#endif
        }

        /*---------------------------------------------------------------------------------------------
        | --- OnValidate: Called when the script is loaded or a value is changed in the Inspector --- |
        ---------------------------------------------------------------------------------------------*/
        private void OnValidate()
        {
#if UNITY_EDITOR
            EnsureRootNode();
#endif
            m_nodeDictionary.Clear();
            foreach (DialogueNode node in m_nodes)
            {
                m_nodeDictionary[node.name] = node;
            }
        }

        /*-----------------------------------------------------
        | --- GetRootNode: Returns the Root Dialogue Node --- |
        -----------------------------------------------------*/
        public DialogueNode GetRootNode(IEnumerable<IConditionChecker> evaluators)
        {
            // Check all potential root nodes for valid conditions
            foreach (DialogueNode node in m_nodes)
            {
                if (IsRootNode(node) && node.CheckCondition(evaluators))
                {
                    return node;
                }
            }
            return m_nodes[0]; // Fallback to default root
        }

        /*-------------------------------------------------------
        | --- IsRootNode: Checks if a Dialogue Node is Root --- |
        -------------------------------------------------------*/
        private bool IsRootNode(DialogueNode node)
        {
            // A root node is one that no other node references as a child
            foreach (DialogueNode otherNode in m_nodes)
            {
                if (otherNode.Children.Contains(node.name))
                    return false;
            }
            return true;
        }

        /*-----------------------------------------------------------------
        | --- GetAllChildren: Returns all children of a Dialogue Node --- |
        -----------------------------------------------------------------*/
        public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
        {
            foreach (string childID in parentNode.Children)
            {
                if (m_nodeDictionary.TryGetValue(childID, out DialogueNode childNode))
                {
                    yield return childNode;
                }
            }
        }

#if UNITY_EDITOR
        /*---------------------------------------------------------------------------
        | --- CreateNode: Creates a new Dialogue Node and adds it to the Parent --- |
        ---------------------------------------------------------------------------*/
        public void CreateNode(DialogueNode parent)
        {
            DialogueNode newNode = InstantiateNode(parent);
            SaveNodeToAsset(newNode);

            Undo.RegisterCreatedObjectUndo(newNode, "Create Dialogue Node");
            Undo.RecordObject(this, "Added Dialogue Node");

            RegisterNode(newNode);

            AssetDatabase.SaveAssets();
        }

        /*---------------------------------------------------------------------------
        | --- DeleteNode: Deletes a Dialogue Node from the Dialogue "tree" list --- |
        ---------------------------------------------------------------------------*/
        public void DeleteNode(DialogueNode deletedNode)
        {
            Undo.RecordObject(this, "Deleted Dialogue Node");

            foreach (DialogueNode node in Nodes)
            {
                node.RemoveChild(deletedNode.name);
            }

            m_nodes.Remove(deletedNode);
            OnValidate();

            Undo.DestroyObjectImmediate(deletedNode);
            AssetDatabase.SaveAssets();
        }

        /*--------------------------------------------------------------------------------
        | --- InstantiateNode: Creates a new Dialogue Node and adds it to the Parent --- |
        --------------------------------------------------------------------------------*/
        private DialogueNode InstantiateNode(DialogueNode parent)
        {
            DialogueNode newNode = CreateInstance<DialogueNode>();
            newNode.name = Guid.NewGuid().ToString();

            if (parent != null)
            {
                parent.AddChild(newNode.name);
                newNode.SetPlayerSpeaking(!parent.IsPlayerSpeaking);
                newNode.SetPosition(parent.Rect.position + m_newNodeOffset);
            }

            return newNode;
        }

        /*------------------------------------------------------------------------
        | --- RegisterNode: Adds a Dialogue Node to the Dialogue "tree" list --- |
        ------------------------------------------------------------------------*/
        private void RegisterNode(DialogueNode newNode)
        {
            m_nodes.Add(newNode);
            OnValidate();
        }

        /*---------------------------------------------------------------
        | --- SaveNodeToAsset: Saves the node to the Dialogue Asset --- |
        ---------------------------------------------------------------*/
        private void SaveNodeToAsset(DialogueNode newNode)
        {
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.AddObjectToAsset(newNode, this);
            }
        }
#endif

        /*-----------------------------------------------------------------------------
        | --- OnBeforeSerialize: Callback just before Unity serializes the Object --- |
        -----------------------------------------------------------------------------*/
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (m_nodes.Count == 0)
            {
                DialogueNode newNode = InstantiateNode(null);       // Creates a node with no parent (links) to ensure a valid Root Node
                RegisterNode(newNode);
            }

            // If the Dialogue is an Asset, add all of the Nodes to it
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (DialogueNode node in Nodes)
                {
                    if (AssetDatabase.GetAssetPath(node) == "")
                    {
                        AssetDatabase.AddObjectToAsset(node, this);
                    }
                }
            }
#endif
        }

        /*-------------------------------------------------------------------------------
        | --- OnAfterDeserialize: Callback just after Unity deserializes the Object --- |
        -------------------------------------------------------------------------------*/
        public void OnAfterDeserialize()
        {
            //...
        }
    }
}