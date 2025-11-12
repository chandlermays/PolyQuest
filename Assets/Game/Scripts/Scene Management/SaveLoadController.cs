using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.SceneManagement
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
    public class SaveLoadController : MonoBehaviour
    {
        private const string kCurrentSaveKey = "CurrentSaveName";

        /* --- kCurrentSaveKey Bindings --- */
        [SerializeField] private KeyCode kSaveKey = KeyCode.F5;
        [SerializeField] private KeyCode kLoadKey = KeyCode.F9;
        [SerializeField] private SceneField m_firstSceneIndex;
        [SerializeField] private SceneField m_menuSceneIndex;

        private SaveSystem m_saveSystem;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_saveSystem = GetComponent<SaveSystem>();
            Utilities.CheckForNull(m_saveSystem, nameof(m_saveSystem));
        }

        /*---------------------------------------------------
        | --- LoadLastScene: Load the last active scene --- |
        ---------------------------------------------------*/
        private IEnumerator LoadLastScene()
        {
            TransitionFade transitionFade = FindFirstObjectByType<TransitionFade>();

            yield return transitionFade.FadeOut();
            yield return m_saveSystem.LoadLastScene(GetCurrentSave());
            yield return transitionFade.FadeIn();
        }

        /*----------------------------------------------------------
        | --- LoadFirstScene: Load the first scene of the game --- |
        ----------------------------------------------------------*/
        private IEnumerator LoadFirstScene()
        {
            TransitionFade transitionFade = FindFirstObjectByType<TransitionFade>();

            yield return transitionFade.FadeOut();
            yield return SceneManager.LoadSceneAsync(m_firstSceneIndex);
            yield return transitionFade.FadeIn();
        }

        /*-----------------------------------------------------
        | --- LoadMainMenuScene: Load the main menu scene --- |
        -----------------------------------------------------*/
        private IEnumerator LoadMainMenuScene()
        {
            TransitionFade transitionFade = FindFirstObjectByType<TransitionFade>();

            yield return transitionFade.FadeOut();
            yield return SceneManager.LoadSceneAsync(m_menuSceneIndex);
            yield return transitionFade.FadeIn();
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (Input.GetKeyDown(kSaveKey))
            {
                Save();
            }
            if (Input.GetKeyDown(kLoadKey))
            {
                Load();
            }
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
        | --- Save: Perform the action of Saving to the File --- |
        --------------------------------------------------------*/
        public void Save()
        {
            m_saveSystem.Save(GetCurrentSave());
        }

        /*---------------------------------------------------------
        | --- Load: Perform the action of Loading from a File --- |
        ---------------------------------------------------------*/
        public void Load()
        {
            m_saveSystem.Load(GetCurrentSave());
        }

        /*------------------------------------------------------------
        | --- Delete: Perform the action of Deleting a Save File --- |
        ------------------------------------------------------------*/
        public void Delete()
        {
            m_saveSystem.Delete(GetCurrentSave());
        }

        /*------------------------------------------------------
        | --- ListSaveFiles: List all available save files --- |
        ------------------------------------------------------*/
        public IEnumerable<string> ListSaveFiles()
        {
            return m_saveSystem.ListSaveFiles();
        }

        /*--------------------------------------------------------
        | --- ContinueGame: Continue a Game from a Save File --- |
        --------------------------------------------------------*/
        public void ContinueGame(string saveFile)
        {
            SetCurrentSave(saveFile);
            StartCoroutine(LoadLastScene());
        }

        /*----------------------------------------------------
        | --- NewGame: Start a New Game with a Save File --- |
        ----------------------------------------------------*/
        public void NewGame(string saveFile)
        {
            if (String.IsNullOrEmpty(saveFile))
                return;

            SetCurrentSave(saveFile);
            StartCoroutine(LoadFirstScene());
            Save();
        }

        /*------------------------------------------------
        | --- LoadMainMenu: Load the Main Menu Scene --- |
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