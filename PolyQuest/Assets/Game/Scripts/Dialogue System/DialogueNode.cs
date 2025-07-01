using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PolyQuest
{
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
        public bool IsPlayerSpeaking() => m_isPlayerSpeaking;
        public string GetText() => m_text;
        public List<string> GetChildren() => m_children;
        public Rect GetRect() => m_rect;
        public string GetOnEnterAction() => m_onEnterAction;
        public string GetOnExitAction() => m_onExitAction;

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