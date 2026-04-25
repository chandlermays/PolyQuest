/*---------------------------
File: DialogueUI.cs
Author: Chandler Mays
----------------------------*/
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
        [SerializeField] private PlayerDialogueHandler m_playerDialogueHandler;
        [SerializeField] private TextMeshProUGUI m_dialogueSpeaker;
        [SerializeField] private TextMeshProUGUI m_dialogueText;
        [SerializeField] private Transform m_choiceRoot;
        [SerializeField] private GameObject m_choicePrefab;
        [SerializeField] private Button m_nextButton;
        [SerializeField] private Button m_endButton;
        [SerializeField] private Button m_quitButton;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_dialogueSpeaker, nameof(m_dialogueSpeaker));
            Utilities.CheckForNull(m_dialogueText, nameof(m_dialogueText));
            Utilities.CheckForNull(m_nextButton, nameof(m_nextButton));
            Utilities.CheckForNull(m_endButton, nameof(m_endButton));
            Utilities.CheckForNull(m_quitButton, nameof(m_quitButton));
            Utilities.CheckForNull(m_playerDialogueHandler, nameof(m_playerDialogueHandler));
         
            m_nextButton.onClick.AddListener(m_playerDialogueHandler.NextDialogueNode);
            m_endButton.onClick.AddListener(m_playerDialogueHandler.EndDialogue);
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

            m_nextButton.onClick.RemoveListener(m_playerDialogueHandler.NextDialogueNode);
            m_endButton.onClick.RemoveListener(m_playerDialogueHandler.EndDialogue);
            m_quitButton.onClick.RemoveListener(m_playerDialogueHandler.EndDialogue);
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
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // Always show the NPC's name and current dialogue text
            m_dialogueSpeaker.text = m_playerDialogueHandler.GetName();
            m_dialogueText.text = m_playerDialogueHandler.GetText();

            // Clear existing choice buttons
            foreach (Transform choice in m_choiceRoot)
            {
                Destroy(choice.gameObject);
            }

            // Check if player needs to make a choice
            bool isPlayerChoosing = m_playerDialogueHandler.IsPlayerChoosing();

            if (isPlayerChoosing)
            {
                // Hide Next/End buttons when showing player choices
                m_nextButton.gameObject.SetActive(false);
                m_endButton.gameObject.SetActive(false);

                // Create choice buttons for each valid player option
                foreach (DialogueNode choice in m_playerDialogueHandler.GetChoices())
                {
                    GameObject choiceInstance = Instantiate(m_choicePrefab, m_choiceRoot);
                    TextMeshProUGUI textComp = choiceInstance.GetComponentInChildren<TextMeshProUGUI>();

                    if (textComp != null)
                    {
                        textComp.text = choice.Text;
                    }

                    Button choiceButton = choiceInstance.GetComponent<Button>();
                    if (choiceButton != null)
                    {
                        // Capture the choice node in a local variable for the lambda
                        DialogueNode selectedChoice = choice;
                        choiceButton.onClick.AddListener(() => OnChoiceSelected(selectedChoice));
                    }
                }
            }
            else
            {
                // Normal NPC dialogue flow - show Next or End button
                bool hasNext = m_playerDialogueHandler.HasNextDialogueNode();
                m_nextButton.gameObject.SetActive(hasNext);
                m_endButton.gameObject.SetActive(!hasNext);
            }
        }

        /*-----------------------------------------------------------------------------
        | --- OnChoiceSelected: Handles when the player selects a dialogue choice --- |
        -----------------------------------------------------------------------------*/
        private void OnChoiceSelected(DialogueNode chosenNode)
        {
            m_playerDialogueHandler.SelectChoice(chosenNode);
        }
    }
}