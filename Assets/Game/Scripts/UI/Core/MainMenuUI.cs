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
            if (string.IsNullOrWhiteSpace(m_newGameText.text))
            {
                m_errorMessage.SetActive(true);
                return;
            }

            m_errorMessage.SetActive(false);
            SaveManager.Instance.NewGame(m_newGameText.text);
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