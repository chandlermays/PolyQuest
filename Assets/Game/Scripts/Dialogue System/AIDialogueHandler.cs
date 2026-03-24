using Unity.Cinemachine;
using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Player;
using PolyQuest.UI.Core;

namespace PolyQuest.Dialogues
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Handles initiating and managing dialogue interactions for AI-controlled NPCs.           *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Stores a reference to the Dialogue asset associated with the NPC.                      *
     *      - Implements IRaycastable to allow the player to interact with the NPC via raycast.      *
     *      - Sets the cursor to the dialogue icon when hovered.                                     *
     * --------------------------------------------------------------------------------------------- */
    public class AIDialogueHandler : MonoBehaviour, IRaycastable
    {
        [SerializeField] private CinemachineCamera m_dialogueCamera;
        [SerializeField] private Dialogue m_dialogue;
        [SerializeField] private string m_speakerName;

        public string SpeakerName => m_speakerName;
        public CinemachineCamera DialogueCamera => m_dialogueCamera;

        private Outline m_outline;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_dialogue, nameof(m_dialogue));

            m_outline = GetComponent<Outline>();
            Utilities.CheckForNull(m_outline, nameof(m_outline));
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
        public bool HandleRaycast(PlayerController playerController)
        {
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                playerController.GetComponent<PlayerDialogueHandler>().BeginDialogueAction(this, m_dialogue);
            }

            return true;
        }

        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
        }
    }
}