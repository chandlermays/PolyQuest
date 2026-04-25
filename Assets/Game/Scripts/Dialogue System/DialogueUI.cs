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
        [SerializeField] private Transform m_choiceRoot; // (better name?)
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
            // NOTE: In a dialogue, it moves node to node, and they're either player or NPC nodes. This is configured by a boolean checkbox via DialogueNode.cs
            // As it currently stands, a dialogue asset only has nodes where it's the NPC speaking -- I did not mark any nodes as the player speaking.
            // I know want to incorporate choices for the player, which means we'll need to be creating nodes and marking them as 'player' nodes.
            // A representation of the dialogue UI I want to have is as follows:
            // [NPC Name]
            // [Dialogue Text]
            //
            // [Choice 1]
            // [Choice 2]
            // ...

            // I want the choices to appear as buttons below the dialogue text and I want the player to be able to click on them to select their response.
            // While the choices are visible, I want to ensure that the text of the dialogue is still visible of what the NPC had just said.

            if (!m_playerDialogueHandler.IsActive())
                return;

            m_dialogueSpeaker.text = m_playerDialogueHandler.GetName();
            m_dialogueText.text = m_playerDialogueHandler.GetText();
            m_nextButton.gameObject.SetActive(m_playerDialogueHandler.HasNextDialogueNode());
            m_endButton.gameObject.SetActive(!m_playerDialogueHandler.HasNextDialogueNode());

            foreach (Transform choice in m_choiceRoot)
            {
                Destroy(choice.gameObject);
            }

            //        foreach (string choiceText in get the player choices from playerdialoguehandler)
            //        {
            //            GameObject choiceInstance = Instantiate(m_choicePrefab, m_choiceRoot);
            //            var textComp = choiceInstance.GetComponentInChildren<TextMeshProUGUI>().text = choiceText;
            //            textComp.text = choiceText;
            //        }
        }
    }
}