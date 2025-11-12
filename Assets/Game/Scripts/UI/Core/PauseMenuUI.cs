using UnityEngine;
//---------------------------------
using PolyQuest.Player;
using PolyQuest.SceneManagement;
using UnityEngine.UI;

namespace PolyQuest.UI.Core
{
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private PlayerController m_playerController;
        [SerializeField] private Button m_resumeButton;
        [SerializeField] private Button m_saveButton;
        [SerializeField] private Button m_quitButton;

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            Time.timeScale = 0f;
            m_playerController.enabled = false;
        }

        /*------------------------------------------------------------------------
        | --- OnDisable: Called when the object becomes disabled or inactive --- |
        ------------------------------------------------------------------------*/
        private void OnDisable()
        {
            Time.timeScale = 1f;
            m_playerController.enabled = true;
        }

        /*---------------------------------------------------------------------
        | --- Save: Invokes the SaveLoadController to save the game state --- |
        ---------------------------------------------------------------------*/
        public void Save()
        {
            SaveLoadController saveLoadController = FindFirstObjectByType<SaveLoadController>();
            if (saveLoadController != null)
            {
                saveLoadController.Save();
            }
        }

        /*------------------------------------------------------------
        | --- Quit: Saves the game and loads the main menu scene --- |
        ------------------------------------------------------------*/
        public void Quit()
        {
            SaveLoadController saveLoadController = FindFirstObjectByType<SaveLoadController>();
            if (saveLoadController != null)
            {
                m_playerController.enabled = false;
                m_resumeButton.interactable = false;
                m_saveButton.interactable = false;
                m_quitButton.interactable = false;

                saveLoadController.Save();
                saveLoadController.LoadMainMenu();
            }
        }
    }
}