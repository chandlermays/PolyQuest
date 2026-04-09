using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//---------------------------------
using PolyQuest.SceneManagement;

namespace PolyQuest.Saving
{
    /* --------------------------------------------------------------------------------------------
     * Role: Coordinates saving and loading of game state, including scene restoration and input.  *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Listens for user input to trigger save and load actions.                             *
     *      - Interfaces with the SaveSystem to persist or restore game data.                      *
     *      - Manages scene fade transitions during load operations.                               *
     *      - Ensures the correct save file is used for all operations.                            *
     *      - Provides a simple API for other systems to invoke save/load functionality.           *
     * ------------------------------------------------------------------------------------------- */
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string kCurrentSaveKey = "CurrentSaveName";
        private const string kAutoSaveSuffix = "_autosave";

        /* --- kCurrentSaveKey Bindings --- */
        [SerializeField] private SceneField m_firstSceneIndex;
        [SerializeField] private SceneField m_menuSceneIndex;

        private SavingSystem m_saveSystem;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            m_saveSystem = GetComponent<SavingSystem>();
            Utilities.CheckForNull(m_saveSystem, nameof(m_saveSystem));
        }

        /*---------------------------------------------------
        | --- LoadLastScene: Load the last active scene --- |
        ---------------------------------------------------*/
        private IEnumerator LoadLastScene()
        {
            yield return TransitionFade.Instance.FadeOut();
            yield return m_saveSystem.LoadLastScene(GetCurrentSave());
            yield return TransitionFade.Instance.FadeIn();
        }

        /*----------------------------------------------------------
        | --- LoadFirstScene: Load the first scene of the game --- |
        ----------------------------------------------------------*/
        private IEnumerator LoadFirstScene()
        {
            yield return TransitionFade.Instance.FadeOut();
            yield return SceneManager.LoadSceneAsync(m_firstSceneIndex);
            yield return null; // Wait one frame for Awake/OnEnable/Start to complete
            Save();
            yield return TransitionFade.Instance.FadeIn();
        }

        /*-----------------------------------------------------
        | --- LoadMainMenuScene: Load the main menu scene --- |
        -----------------------------------------------------*/
        private IEnumerator LoadMainMenuScene()
        {
            yield return TransitionFade.Instance.FadeOut();
            yield return SceneManager.LoadSceneAsync(m_menuSceneIndex);
            yield return TransitionFade.Instance.FadeIn();
        }

        /*--------------------------------------------------------
        | --- SetCurrentSave: Set the current save file name --- |
        --------------------------------------------------------*/
        private void SetCurrentSave(string saveFile)
        {
            PlayerPrefs.SetString(kCurrentSaveKey, saveFile);
        }

        /*--------------------------------------------------------
        | --- GetCurrentSave: Get the current save file name --- |
        --------------------------------------------------------*/
        private string GetCurrentSave()
        {
            return PlayerPrefs.GetString(kCurrentSaveKey);
        }

        /*--------------------------------------------------------
        | --- Save: Perform the action of saving to the file --- |
        --------------------------------------------------------*/
        public void Save()
        {
            m_saveSystem.Save(GetCurrentSave());
        }

        /*--------------------------------------------------------------------------
        | --- AutoSave: Save to a separate slot without touching the main save --- |
        --------------------------------------------------------------------------*/
        public void AutoSave()
        {
            m_saveSystem.Save(GetCurrentSave() + kAutoSaveSuffix);
        }

        /*---------------------------------------------------------
        | --- Load: Perform the action of loading from a file --- |
        ---------------------------------------------------------*/
        public void Load()
        {
            m_saveSystem.Load(GetCurrentSave());
        }

        /*----------------------------------------------------------------------
        | --- Delete: Perform the action of deleting the current save file --- |
        ----------------------------------------------------------------------*/
        public void Delete()
        {
            m_saveSystem.Delete(GetCurrentSave());
        }

        /*----------------------------------------------------------------------
        | --- Delete: Perform the action of deleting a specified save file --- |
        ----------------------------------------------------------------------*/
        public void Delete(string saveFile)
        {
            m_saveSystem.Delete(saveFile);
        }

        /*------------------------------------------------------
        | --- ListSaveFiles: List all available save files --- |
        ------------------------------------------------------*/
        public IEnumerable<string> ListSaveFiles()
        {
            return m_saveSystem.ListSaveFiles();
        }

        /*--------------------------------------------------------
        | --- ContinueGame: Continue a Game from a save file --- |
        --------------------------------------------------------*/
        public void ContinueGame(string saveFile)
        {
            SetCurrentSave(saveFile);
            StartCoroutine(LoadLastScene());
        }

        /*----------------------------------------------------
        | --- NewGame: Start a New Game with a save file --- |
        ----------------------------------------------------*/
        public void NewGame(string saveFile)
        {
            if (String.IsNullOrEmpty(saveFile))
                return;

            SetCurrentSave(saveFile);
            StartCoroutine(LoadFirstScene());
        }

        /*------------------------------------------------
        | --- LoadMainMenu: Load the main menu scene --- |
        ------------------------------------------------*/
        public void LoadMainMenu()
        {
            StartCoroutine(LoadMainMenuScene());
        }

        /*-----------------------------------------------------------
        | --- RespawnCheckpoint: Respawn at the last checkpoint --- |
        -----------------------------------------------------------*/
        public void RespawnCheckpoint()
        {
            StartCoroutine(LoadLastScene());
        }
    }
}