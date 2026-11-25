/*---------------------------
| File: MainMenuUI.cs
| Author: Chandler Mays
----------------------------*/
using TMPro;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.UI.Core
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_newGameText;
        [SerializeField] private GameObject m_errorMessage;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            // hide the error message at the start
            m_errorMessage.SetActive(false);

            // hide if the user starts typing
            m_newGameText.onValueChanged.AddListener(_ => m_errorMessage.SetActive(false));
        }

        /*-------------------------------------------------------
        | --- NewGame: Start a New Game with the given name --- |
        -------------------------------------------------------*/
        public void NewGame()
        {
            // An empty name, a name in use, or one that can't be a file name: refused with the error message
            bool started = !string.IsNullOrWhiteSpace(m_newGameText.text) && SaveManager.Instance.NewGame(m_newGameText.text);
            m_errorMessage.SetActive(!started);
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