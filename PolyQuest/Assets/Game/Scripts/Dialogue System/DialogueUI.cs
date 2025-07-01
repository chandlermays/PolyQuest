using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PolyQuest
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_dialogueText;
        [SerializeField] private Button m_nextButton;
        [SerializeField] private Button m_quitButton;
        private PlayerDialogueHandler m_newScript;

        private const string kPlayerTag = "Player";

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        void Start()
        {
            m_newScript = GameObject.FindGameObjectWithTag(kPlayerTag).GetComponent<PlayerDialogueHandler>();
            m_newScript.OnConversationUpdated += UpdateUI;
            m_nextButton.onClick.AddListener(m_newScript.NextDialogueNode);
            m_quitButton.onClick.AddListener(m_newScript.EndDialogue);

            UpdateUI();
        }

        /*-----------------------------------------------------------------
        | --- UpdateUI: Updates the UI with the current Dialogue Text --- |
        -----------------------------------------------------------------*/
        private void UpdateUI()
        {
            gameObject.SetActive(m_newScript.IsActive());
            if (!m_newScript.IsActive())
                return;

            m_dialogueText.text = m_newScript.GetText();
            m_nextButton.gameObject.SetActive(m_newScript.HasNextDialogueNode());
        }
    }
}