/*---------------------------
File: AIDialogueHandler.cs
Author: Chandler Mays
----------------------------*/
using Unity.Cinemachine;
using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Player;
using PolyQuest.UI.Core;
using PolyQuest.UI;

namespace PolyQuest.Dialogues
{
    /* -----------------------------------------------------------------------------------------------
     * Role: Handles initiating and managing dialogue interactions for AI-controlled NPCs.           *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Stores a reference to the Dialogue asset associated with the NPC.                      *
     *      - Implements IRaycastable to allow the player to interact with the NPC via raycast.      *
     *      - Sets the cursor to the dialogue icon when hovered.                                     *
     * --------------------------------------------------------------------------------------------- */
    public class AIDialogueHandler : MonoBehaviour, IInteractable
    {
        [SerializeField] private CinemachineCamera m_dialogueCamera;
        [SerializeField] private Dialogue m_dialogue;
        [SerializeField] private string m_speakerName;

        public string SpeakerName => m_speakerName;
        public CinemachineCamera DialogueCamera => m_dialogueCamera;

        private Outline m_outline;
        private WorldLabel m_worldLabel;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_dialogue, nameof(m_dialogue));

            m_outline = GetComponent<Outline>();
            Utilities.CheckForNull(m_outline, nameof(m_outline));

            m_worldLabel = GetComponent<WorldLabel>();
            Utilities.CheckForNull(m_worldLabel, nameof(m_worldLabel));

            m_worldLabel.SetLabel(m_speakerName);
        }

        /*-------------------------------------------------------------
        | --- GetCursorType: Returns the Cursor Type for Dialogue --- |
        -------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kDialogue;
        }

        /*----------------------------------------------------------------------------
        | --- HandleRaycast: The Behavior of the Raycast for Initiating Dialogue --- |
        ----------------------------------------------------------------------------*/
        public bool HandleInteraction(PlayerController playerController)
        {
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                playerController.GetComponent<PlayerDialogueHandler>().BeginDialogueAction(this, m_dialogue);
            }

            return true;
        }

        /*-------------------------------------------------------------------------------------
        | --- ToggleHighlight: Enables or Disables the Outline Component for Highlighting --- |
        -------------------------------------------------------------------------------------*/
        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
        }

        /*-----------------------------------------------------------------------------
        | --- ToggleLabel: Controls the visibility of the world label for the NPC --- |
        -----------------------------------------------------------------------------*/
        public void ToggleLabel(bool visible)
        {
            m_worldLabel.Toggle(visible);
        }
    }
}