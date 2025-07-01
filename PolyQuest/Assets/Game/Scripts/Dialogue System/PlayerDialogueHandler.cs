using System;
using System.Collections.Generic;
using System.Linq;  // TO-DO: Remove this. Replace with my own implementations.
using UnityEngine;

namespace PolyQuest
{
    public class PlayerDialogueHandler : MonoBehaviour
    {
        private Dialogue m_activeDialogue;
        private DialogueNode m_currentNode;
        private AIDialogueHandler m_activeNPC;

        public event Action OnConversationUpdated;

        /*---------------------------------------------------------------------- 
        | --- BeginDialogue: Begins a new Dialogue with the given Dialogue --- |
        ----------------------------------------------------------------------*/
        public void BeginDialogue(AIDialogueHandler newNPC, Dialogue newDialogue)
        {
            m_activeNPC = newNPC;
            m_activeDialogue = newDialogue;
            m_currentNode = m_activeDialogue.GetRootNode();
            TriggerEnterAction();
            OnConversationUpdated?.Invoke();
        }

        /*---------------------------------------------------------------- 
        | --- EndDialogue: Ends the current active Dialogue (if any) --- |
        ----------------------------------------------------------------*/
        public void EndDialogue()
        {
            m_activeDialogue = null;
            TriggerExitAction();
            m_currentNode = null;
            m_activeNPC = null;
            OnConversationUpdated?.Invoke();
        }

        /*------------------------------------------------------------ 
        | --- IsActive: Checks if a Dialogue is currently active --- |
        ------------------------------------------------------------*/
        public bool IsActive()
        {
            return m_activeDialogue != null;
        }

        /*---------------------------------------------------------------------- 
        | --- GetText: Returns the Text of the Active Dialogue's Root Node --- |
        ----------------------------------------------------------------------*/
        public string GetText()
        {
            if (m_currentNode != null)
            {
                return m_currentNode.GetText();
            }

            return "";
        }

        /*----------------------------------------------------------------------- 
        | --- NextDialogueNode: Moves to the next Dialogue Node in the tree --- |
        -----------------------------------------------------------------------*/
        public void NextDialogueNode()
        {
            DialogueNode[] children = FilterNodesByCondition(m_activeDialogue.GetAllChildren(m_currentNode)).ToArray();
            int randomIndex = UnityEngine.Random.Range(0, children.Count());
            TriggerExitAction();
            m_currentNode = children[randomIndex];
            TriggerEnterAction();
            OnConversationUpdated?.Invoke();
        }

        /*------------------------------------------------------------------------------------ 
        | --- HasNextDialogueNode: Checks if there are any children for the current Node --- |
        ------------------------------------------------------------------------------------*/
        public bool HasNextDialogueNode()
        {
            return FilterNodesByCondition(m_activeDialogue.GetAllChildren(m_currentNode)).Count() > 0;
        }

        /*---------------------------------------------------------------------------------------------- 
        | --- FilterNodesByCondition: Filters the children of the current Node based on conditions --- |
        ----------------------------------------------------------------------------------------------*/
        private IEnumerable<DialogueNode> FilterNodesByCondition(IEnumerable<DialogueNode> inputNode)
        {
            foreach (DialogueNode node in inputNode)
            {
                if (node.CheckCondition(GetEvaluators()))
                {
                    yield return node;
                }
            }
        }

        /*------------------------------------------------------------------------- 
        | --- GetEvaluators: Returns all Condition Checkers on the active NPC --- |
        -------------------------------------------------------------------------*/
        private IEnumerable<IConditionChecker> GetEvaluators()
        {
            return GetComponents<IConditionChecker>();
        }

        /*---------------------------------------------------------------------------- 
        | --- TriggerEnterAction: Triggers the OnEnterAction of the current Node --- |
        ----------------------------------------------------------------------------*/
        private void TriggerEnterAction()
        {
            if (m_currentNode != null & m_currentNode.GetOnEnterAction() != "")
            {
                Trigger(m_currentNode.GetOnEnterAction());
            }
        }

        /*-------------------------------------------------------------------------- 
        | --- TriggerExitAction: Triggers the OnExitAction of the current Node --- |
        --------------------------------------------------------------------------*/
        private void TriggerExitAction()
        {
            if (m_currentNode != null & m_currentNode.GetOnExitAction() != "")
            {
                Trigger(m_currentNode.GetOnExitAction());
            }
        }

        /*-------------------------------------------------------------------------------------------- 
        | --- Trigger: Triggers the given action for all DialogueEventTriggers on the active NPC --- |
        --------------------------------------------------------------------------------------------*/
        private void Trigger(string action)
        {
            if (action == "")
                return;

            foreach (DialogueEventTrigger trigger in m_activeNPC.GetComponents<DialogueEventTrigger>())
            {
                trigger.Trigger(action);
            }
        }
    }
}