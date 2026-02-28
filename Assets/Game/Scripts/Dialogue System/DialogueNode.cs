using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//---------------------------------
using PolyQuest.Tools;

namespace PolyQuest.Dialogues
{
    /* --------------------------------------------------------------------------------------------
     * Role: Represents a single node in a dialogue tree, containing dialogue text and logic.      *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores dialogue text, speaker information, and child node references.                *
     *      - Tracks node position for editor visualization.                                       *
     *      - Manages actions to trigger on entering or exiting the node.                          *
     *      - Supports adding, removing, and updating child nodes and node properties.             *
     *      - Evaluates conditions to determine if the node should be available.                   *
     *      - Integrates with Unity Editor for undo/redo and asset management.                     *
     * ------------------------------------------------------------------------------------------- */
    public class DialogueNode : ScriptableObject
    {
        [SerializeField] private bool m_isPlayerSpeaking = false;
        [SerializeField] private string m_text;
        [SerializeField] private List<string> m_children = new();
        [SerializeField] private Rect m_rect = new(0, 0, 200, 100);
        [SerializeField] private string m_onEnterAction;
        [SerializeField] private string m_onExitAction;
        [SerializeField] private Condition m_condition;

        /* --- Getter Methods --- */
        public bool IsPlayerSpeaking            => m_isPlayerSpeaking;
        public string Text                      => m_text;
        public List<string> Children            => m_children;
        public Rect Rect                        => m_rect;
        public string OnEnterAction             => m_onEnterAction;
        public string OnExitAction              => m_onExitAction;

#if UNITY_EDITOR
        /*-------------------------------------------------------------
        | --- SetPosition: Sets the position of the Dialogue Node --- |
        -------------------------------------------------------------*/
        public void SetPosition(Vector2 position)
        {
            // Record the change for undo functionality
            Undo.RecordObject(this, "Move Dialogue Node");
            m_rect.position = position;
            EditorUtility.SetDirty(this);
        }

        /*-----------------------------------------------------
        | --- SetText: Sets the text of the Dialogue Node --- |
        -----------------------------------------------------*/
        public void SetText(string text)
        {
            if (text != m_text)
            {
                // Record the change for undo functionality
                Undo.RecordObject(this, "Change Dialogue Node Text");
                m_text = text;
                EditorUtility.SetDirty(this);
            }
        }

        /*-----------------------------------------------------
        | --- AddChild: Adds a child to the Dialogue Node --- |
        -----------------------------------------------------*/
        public void AddChild(string childID)
        {
            if (!m_children.Contains(childID))
            {
                // Record the change for undo functionality
                Undo.RecordObject(this, "Add Child to Dialogue Node");
                m_children.Add(childID);
                EditorUtility.SetDirty(this);
            }
        }

        /*-------------------------------------------------------------
        | --- RemoveChild: Removes a child from the Dialogue Node --- |
        -------------------------------------------------------------*/
        public void RemoveChild(string childID)
        {
            if (m_children.Contains(childID))
            {
                // Record the change for undo functionality
                Undo.RecordObject(this, "Remove Child from Dialogue Node");
                m_children.Remove(childID);
                EditorUtility.SetDirty(this);
            }
        }

        /*----------------------------------------------------------
        | --- SetPlayerSpeaking: Sets the player speaking flag --- |
        ----------------------------------------------------------*/
        public void SetPlayerSpeaking(bool flag)
        {
            if (flag != m_isPlayerSpeaking)
            {
                // Record the change for undo functionality
                Undo.RecordObject(this, "Change Dialogue Speaker");
                m_isPlayerSpeaking = flag;
                EditorUtility.SetDirty(this);
            }
        }
#endif

        /*-----------------------------------------------------------------------------------
        | --- CheckCondition: Checks if the condition is met to execute a Dialogue Node --- |
        -----------------------------------------------------------------------------------*/
        public bool CheckCondition(IEnumerable<IConditionChecker> evaluators)
        {
            return m_condition.Check(evaluators);
        }
    }
}