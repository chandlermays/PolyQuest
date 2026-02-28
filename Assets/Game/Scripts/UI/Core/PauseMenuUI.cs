using UnityEngine;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Player;
using PolyQuest.Saving;

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
            SaveManager.Instance.Save();
        }

        /*------------------------------------------------------------
        | --- Quit: Saves the game and loads the main menu scene --- |
        ------------------------------------------------------------*/
        public void Quit()
        {
            m_playerController.enabled = false;
            m_resumeButton.interactable = false;
            m_saveButton.interactable = false;
            m_quitButton.interactable = false;

            SaveManager.Instance.Save();
            SaveManager.Instance.LoadMainMenu();
        }
    }
}