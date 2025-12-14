using TMPro;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------

// Future Feature: Add support for dialogue choices and complex branching paths.

namespace PolyQuest.Dialogues
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Manages the user interface for in-game dialogue, displaying text and handling input.   *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Displays the current dialogue text to the player.                                     *
     *      - Shows or hides the dialogue UI based on conversation state.                           *
     *      - Handles user input for progressing or quitting dialogue (Next/Quit buttons).          *
     *      - Subscribes to dialogue events and updates the UI accordingly.                         *
     *      - Connects UI elements to the PlayerDialogueHandler for dialogue flow control.          *
     * -------------------------------------------------------------------------------------------- */
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_dialogueSpeaker;
        [SerializeField] private TextMeshProUGUI m_dialogueText;
        [SerializeField] private Button m_nextButton;
        [SerializeField] private Button m_quitButton;
        [SerializeField] private PlayerDialogueHandler m_playerDialogueHandler;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_dialogueSpeaker, nameof(m_dialogueSpeaker));
            Utilities.CheckForNull(m_dialogueText, nameof(m_dialogueText));
            Utilities.CheckForNull(m_nextButton, nameof(m_nextButton));
            Utilities.CheckForNull(m_quitButton, nameof(m_quitButton));
            Utilities.CheckForNull(m_playerDialogueHandler, nameof(m_playerDialogueHandler));
         
            m_nextButton.onClick.AddListener(m_playerDialogueHandler.NextDialogueNode);
            m_quitButton.onClick.AddListener(m_playerDialogueHandler.EndDialogue);
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_playerDialogueHandler.OnDialogueUpdated += UpdateUI;
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            m_playerDialogueHandler.OnDialogueUpdated -= UpdateUI;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        void Start()
        {
            gameObject.SetActive(m_playerDialogueHandler.IsActive());
        }

        /*-----------------------------------------------------------------
        | --- UpdateUI: Updates the UI with the current Dialogue Text --- |
        -----------------------------------------------------------------*/
        private void UpdateUI()
        {
            if (!m_playerDialogueHandler.IsActive())
                return;

            m_dialogueSpeaker.text = m_playerDialogueHandler.GetName();
            m_dialogueText.text = m_playerDialogueHandler.GetText();
            m_nextButton.gameObject.SetActive(m_playerDialogueHandler.HasNextDialogueNode());
        }
    }
}