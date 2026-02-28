using TMPro;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.UI.Core
{
    public class ContinueGameUI : MonoBehaviour
    {
        [SerializeField] private Transform m_contentRoot;
        [SerializeField] private GameObject m_buttonPrefab;
        [SerializeField] private Button m_playButton;
        [SerializeField] private Button m_deleteButton;

        private string m_selectedSaveFile;

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (SaveManager.Instance == null)
                return;

            RefreshSaveList();
            if (m_playButton != null)
                m_playButton.interactable = false;

            if (m_deleteButton != null)
                m_deleteButton.interactable = false;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            RefreshSaveList();
            if (m_playButton != null)
                m_playButton.interactable = false;

            if (m_deleteButton != null)
                m_deleteButton.interactable = false;
        }

        /*---------------------------------------------------------------------
        | --- SelectSaveFile: Handle selection of a save file from the UI --- |
        ---------------------------------------------------------------------*/
        public void SelectSaveFile(string saveFile)
        {
            m_selectedSaveFile = saveFile;
            if (m_playButton != null)
                m_playButton.interactable = true;

            if (m_deleteButton != null)
                m_deleteButton.interactable = true;
        }

        /*------------------------------------------------------------------
        | --- PlaySelected: Start the game with the selected save file --- |
        ------------------------------------------------------------------*/
        public void PlaySelected()
        {
            if (string.IsNullOrEmpty(m_selectedSaveFile))
                return;

            SaveManager.Instance.ContinueGame(m_selectedSaveFile);
        }

        /*---------------------------------------------------------------------
        | --- DeleteSelected: Delete the selected save and refresh the UI --- |
        ---------------------------------------------------------------------*/
        public void DeleteSelected()
        {
            if (string.IsNullOrEmpty(m_selectedSaveFile))
                return;

            SaveManager.Instance.Delete(m_selectedSaveFile);

            // Clear selection and refresh the list
            m_selectedSaveFile = null;
            RefreshSaveList();

            if (m_playButton != null)
                m_playButton.interactable = false;

            if (m_deleteButton != null)
                m_deleteButton.interactable = false;
        }

        /*-------------------------------------------------------------------
        | --- RefreshSaveList: Rebuild the list of available save files --- |
        -------------------------------------------------------------------*/
        private void RefreshSaveList()
        {
            // Clear existing buttons
            foreach (Transform child in m_contentRoot)
            {
                Destroy(child.gameObject);
            }

            // Rebuild from available save files
            foreach (string saveFile in SaveManager.Instance.ListSaveFiles())
            {
                GameObject buttonInstance = Instantiate(m_buttonPrefab, m_contentRoot);
                TMP_Text buttonText = buttonInstance.GetComponentInChildren<TMP_Text>();
                buttonText.text = saveFile;

                Button button = buttonInstance.GetComponent<Button>();
                button.onClick.AddListener(() => SelectSaveFile(saveFile));
            }
        }
    }
}