/*---------------------------
| File: SaveManager.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//---------------------------------
using PolyQuest.Components;
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
        private const string kPlayerTag = "Player";

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

        /*----------------------------------------------------------------------------------------------------
        | --- ContinueRoutine: Loads the playthrough; with nothing readable, stays at the menu, faded in --- |
        ----------------------------------------------------------------------------------------------------*/
        private IEnumerator ContinueRoutine()
        {
            yield return TransitionFade.Instance.FadeOut();

            bool loaded = false;
            yield return m_saveSystem.LoadLastScene(GetCurrentSave(), result => loaded = result);
            if (!loaded)
            {
                Debug.LogWarning($"SaveManager: couldn't continue '{GetCurrentSave()}': nothing in it could be read. Staying at the main menu.");
            }

            // Never leave the screen black, whatever happened
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
            AutoSave();         // The playthrough's first save is an automatic one; only the pause menu writes the manual save
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

        /*--------------------------------------------------------------------
        | --- Save: Write the manual save; only the pause menu does this --- |
        --------------------------------------------------------------------*/
        public void Save()
        {
            m_saveSystem.Save(GetCurrentSave());
        }

        /*-------------------------------------------------------------------------------------------------------------
        | --- AutoSave: Write the autosave, for every automatic save (level-up, quests, shops, portals, dialogue) --- |
        -------------------------------------------------------------------------------------------------------------*/
        public void AutoSave()
        {
            m_saveSystem.AutoSave(GetCurrentSave());
        }

        /*--------------------------------------------------------------------------------------------------------
        | --- Load: Restore from the newer of the manual save and the autosave; false if nothing can be read --- |
        --------------------------------------------------------------------------------------------------------*/
        public bool Load()
        {
            return m_saveSystem.Load(GetCurrentSave());
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
            StartCoroutine(ContinueRoutine());
        }

        /*----------------------------------------------------------------------------------------------------------------------
        | --- NewGame: Start a New Game; refused (false) for a name in use or one that can't be a file name, never a merge --- |
        ----------------------------------------------------------------------------------------------------------------------*/
        public bool NewGame(string saveFile)
        {
            if (!SavingSystem.IsValidSaveName(saveFile))
            {
                Debug.LogWarning($"SaveManager: a new game can't be named '{saveFile}': it can't be used as a file name.");
                return false;
            }

            if (m_saveSystem.SaveFileExists(saveFile))
            {
                Debug.LogWarning($"SaveManager: a new game can't be named '{saveFile}': a playthrough with that name already exists.");
                return false;
            }

            m_saveSystem.ForgetState(saveFile);
            SetCurrentSave(saveFile);
            StartCoroutine(LoadFirstScene());
            return true;
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
            StartCoroutine(RespawnRoutine());
        }

        /*---------------------------------------------------------------------------------------------
        | --- RespawnRoutine: Reloads the last save, then brings the Player back at full strength --- |
        ---------------------------------------------------------------------------------------------*/
        private IEnumerator RespawnRoutine()
        {
            yield return TransitionFade.Instance.FadeOut();

            bool loaded = false;
            yield return m_saveSystem.LoadLastScene(GetCurrentSave(), result => loaded = result);
            if (loaded)
            {
                // Only after the load has completely finished, so the restored health and mana can't overwrite the refill
                RefillPlayer();
            }
            else
            {
                Debug.LogWarning($"SaveManager: couldn't respawn from '{GetCurrentSave()}': nothing in it could be read. Returning to the main menu.");
                yield return SceneManager.LoadSceneAsync(m_menuSceneIndex);
            }

            // Never leave the screen black, whatever happened
            yield return TransitionFade.Instance.FadeIn();
        }

        /*-----------------------------------------------------------------
        | --- RefillPlayer: Sets the Player's health and mana to full --- |
        -----------------------------------------------------------------*/
        private static void RefillPlayer()
        {
            GameObject player = GameObject.FindWithTag(kPlayerTag);
            if (player == null)
                return;

            if (player.TryGetComponent(out HealthComponent health))
                health.FullyReplenishHealth();

            if (player.TryGetComponent(out ManaComponent mana))
                mana.FullyReplenishMana();
        }
    }
}