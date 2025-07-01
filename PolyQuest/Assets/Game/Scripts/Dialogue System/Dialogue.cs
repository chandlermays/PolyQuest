using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PolyQuest
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "PolyQuest/Dialogue", order = 0)]
    public class Dialogue : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<DialogueNode> m_nodes = new();
        [SerializeField] private Vector2 m_newNodeOffset = new(300, 0);

        private readonly Dictionary<string, DialogueNode> m_nodeDictionary = new();

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

        /*-------------------------------------------------------------------------
        | --- GetAllNodes: Returns all of the Dialogue Nodes in the container --- |
        -------------------------------------------------------------------------*/
        public IEnumerable<DialogueNode> GetAllNodes()
        {
            return m_nodes;
        }

        /*-----------------------------------------------------
        | --- GetRootNode: Returns the Root Dialogue Node --- |
        -----------------------------------------------------*/
        public DialogueNode GetRootNode()
        {
            if (m_nodes == null || m_nodes.Count == 0)
            {
                return null;
            }
            return m_nodes[0];
        }

        /*-----------------------------------------------------------------
        | --- GetAllChildren: Returns all children of a Dialogue Node --- |
        -----------------------------------------------------------------*/
        public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
        {
            foreach (string childID in parentNode.GetChildren())
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

            foreach (DialogueNode node in GetAllNodes())
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
                newNode.SetPlayerSpeaking(!parent.IsPlayerSpeaking());
                newNode.SetPosition(parent.GetRect().position + m_newNodeOffset);
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
                foreach (DialogueNode node in GetAllNodes())
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