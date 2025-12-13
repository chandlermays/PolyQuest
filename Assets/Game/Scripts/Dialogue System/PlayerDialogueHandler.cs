using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Tools;
using PolyQuest.Components;
using PolyQuest.Player;
using Unity.Cinemachine;

namespace PolyQuest.Dialogues
{
    /* --------------------------------------------------------------------------------------------
     * Role: Manages the flow of dialogue between the player and NPCs, including node progression. *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Initiates, tracks, and ends active dialogues with NPCs.                              *
     *      - Maintains the current dialogue node and handles node transitions.                    *
     *      - Evaluates dialogue node conditions and filters available responses.                  *
     *      - Triggers enter/exit actions and dialogue events as nodes are traversed.              *
     *      - Notifies listeners when the conversation state updates.                              *
     * ------------------------------------------------------------------------------------------- */
    public class PlayerDialogueHandler : MonoBehaviour
    {
        private Dialogue m_activeDialogue;
        private DialogueNode m_currentNode;
        private AIDialogueHandler m_activeNPC;
        private PlayerController m_playerController;
        private MovementComponent m_movementComponent;

        private const float kProximityThreshold = 4.0f;
        private bool m_inActiveDialogue = false;

        public event Action OnDialogueStarted;
        public event Action OnDialogueUpdated;
        public event Action OnDialogueEnded;

        public CinemachineCamera ActiveDialogueCamera => m_activeNPC != null ? m_activeNPC.DialogueCamera : null;

        /*---------------------------------------------------------------- 
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_playerController = GetComponent<PlayerController>();
            Utilities.CheckForNull(m_playerController, nameof(PlayerController));

            m_movementComponent = GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_movementComponent, nameof(MovementComponent));
        }

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (m_activeNPC == null || m_inActiveDialogue)
                return;

            if (Vector3.Distance(m_activeNPC.transform.position, transform.position) > kProximityThreshold)
            {
                m_movementComponent.MoveTo(m_activeNPC.transform.position);
            }
            else
            {
                m_movementComponent.Stop();
                BeginDialogue();
            }
        }

        /*----------------------------------------------------------- 
        | --- BeginDialogue: Starts the current active Dialogue --- |
        -----------------------------------------------------------*/
        private void BeginDialogue()
        {
            m_inActiveDialogue = true;
            m_currentNode = m_activeDialogue.GetRootNode(GetEvaluators());
            TriggerEnterAction();

            OnDialogueStarted?.Invoke();
            OnDialogueUpdated?.Invoke();
        }

        /*------------------------------------------------------------------------
        | --- BeginDialogueAction: Sets up a new Dialogue with the given NPC --- |
        ------------------------------------------------------------------------*/
        public void BeginDialogueAction(AIDialogueHandler newNPC, Dialogue newDialogue)
        {
            m_activeNPC = newNPC;
            m_activeDialogue = newDialogue;
        }

        /*-------------------------------------------------------------------
        | --- CancelDialogueAction: Cancels the current Dialogue action --- |
        -------------------------------------------------------------------*/
        public void CancelDialogueAction()
        {
            if (m_inActiveDialogue)
            {
                EndDialogue();
                return;
            }

            m_activeNPC = null;
            m_activeDialogue = null;
        }

        /*------------------------------------------------------- 
        | --- EndDialogue: Ends the current active Dialogue --- |
        -------------------------------------------------------*/
        public void EndDialogue()
        {
            m_inActiveDialogue = false;
            m_activeDialogue = null;
            TriggerExitAction();
            m_currentNode = null;
            m_activeNPC = null;

            OnDialogueEnded?.Invoke();
            OnDialogueUpdated?.Invoke();
        }

        /*-------------------------------------------------------- 
        | --- IsActive: Returns whether a Dialogue is active --- |
        --------------------------------------------------------*/
        public bool IsActive()
        {
            return m_activeDialogue != null;
        }

        /*------------------------------------------------------------------- 
        | --- Title: Returns the Name of the active NPC in the Dialogue --- |
        -------------------------------------------------------------------*/
        public string GetName()
        {
            return m_activeNPC.SpeakerName;
        }

        /*------------------------------------------------------------- 
        | --- Text: Returns the Text of the current Dialogue Node --- |
        -------------------------------------------------------------*/
        public string GetText()
        {
            if (m_currentNode != null)
            {
                return m_currentNode.Text;
            }

            return "";
        }

        /*----------------------------------------------------------------------- 
        | --- NextDialogueNode: Moves to the next Dialogue Node in the tree --- |
        -----------------------------------------------------------------------*/
        public void NextDialogueNode()
        {
            List<DialogueNode> validChildren = GetValidChildren();
            if (validChildren.Count == 0)
            {
                EndDialogue();
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, validChildren.Count);
            TriggerExitAction();
            m_currentNode = validChildren[randomIndex];
            TriggerEnterAction();
            OnDialogueUpdated?.Invoke();
        }

        /*------------------------------------------------------------------------------------ 
        | --- HasNextDialogueNode: Checks if there are any children for the current Node --- |
        ------------------------------------------------------------------------------------*/
        public bool HasNextDialogueNode()
        {
            return GetValidChildren().Count > 0;
        }

        /*------------------------------------------------------------------------------------ 
        | --- GetValidChildren: Returns a list of child nodes that meet their conditions --- |
        ------------------------------------------------------------------------------------*/
        private List<DialogueNode> GetValidChildren()
        {
            // Recompute the valid children every time to ensure conditions are checked against the latest state.
            List<DialogueNode> validChildren = new();
            IEnumerable<IConditionChecker> evaluators = GetEvaluators();

            foreach (DialogueNode child in m_activeDialogue.GetAllChildren(m_currentNode))
            {
                if (child.CheckCondition(evaluators))
                {
                    validChildren.Add(child);
                }
            }

            return validChildren;
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
            if (m_currentNode != null && m_currentNode.OnEnterAction != "")
            {
                Trigger(m_currentNode.OnEnterAction);
            }
        }

        /*-------------------------------------------------------------------------- 
        | --- TriggerExitAction: Triggers the OnExitAction of the current Node --- |
        --------------------------------------------------------------------------*/
        private void TriggerExitAction()
        {
            if (m_currentNode != null && m_currentNode.OnExitAction != "")
            {
                Trigger(m_currentNode.OnExitAction);
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