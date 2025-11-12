using UnityEngine;
using TMPro;
//---------------------------------
using PolyQuest.SceneManagement;

namespace PolyQuest.UI.Core
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_newGameText;

        private SaveLoadController m_saveLoadController;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_saveLoadController = FindFirstObjectByType<SaveLoadController>();
        }

        /*-------------------------------------------------------
        | --- NewGame: Start a New Game with the given name --- |
        -------------------------------------------------------*/
        public void NewGame()
        {
            m_saveLoadController.NewGame(m_newGameText.text);
        }

        /*----------------------------------------
        | --- QuitGame: Quit the application --- | 
        ----------------------------------------*/
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}