using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
//---------------------------------

namespace PolyQuest.Saving
{
    /* --------------------------------------------------------------------------------------------
     * Role: Manages the saving and loading of game state, acting as the central save system.      *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Registers and tracks all saveable entities in the game.                              *
     *      - Captures the state of all entities and serializes it to disk.                        *
     *      - Restores entity states from saved files during load operations.                      *
     *      - Handles scene persistence and loading the last active scene.                         *
     *      - Provides utility methods for file management and serialization.                      *
     * ------------------------------------------------------------------------------------------- */
    using SaveState = Dictionary<string, object>;

    public class SaveSystem : MonoBehaviour
    {
        private const string kLastActiveSceneKey = "LastActiveScene";
        private const string kSaveFileExt = ".sav";

        private static readonly HashSet<SaveableEntity> m_registeredEntities = new();

        /*-----------------------------------------------------------------------
        | --- RegisterSaveableEntity: Register a Saveable Entity for Saving --- |
        -----------------------------------------------------------------------*/
        public static void RegisterSaveableEntity(SaveableEntity entity)
        {
            m_registeredEntities.Add(entity);
        }

        /*----------------------------------------------------------------
        | --- UnregisterSaveableEntity: Unregister a Saveable Entity --- |
        ----------------------------------------------------------------*/
        public static void UnregisterSaveableEntity(SaveableEntity entity)
        {
            m_registeredEntities.Remove(entity);
        }

        /*---------------------------------------------------------------
        | --- Save: Capture the current state and save it to a file --- |
        ---------------------------------------------------------------*/
        public void Save(string fileName)
        {
            SaveState state = LoadFile(fileName);
            CaptureState(state);
            SaveFile(fileName, state);
        }

        /*-----------------------------------------------
        | --- Delete: Remove a saved file from disk --- |
        -----------------------------------------------*/
        public void Delete(string fileName)
        {
            File.Delete(GetPathFromSaveFile(fileName));
        }

        /*---------------------------------------------------
        | --- Load: Restore the state from a saved file --- |
        ---------------------------------------------------*/
        public void Load(string fileName)
        {
            RestoreState(LoadFile(fileName));
        }
    
        /*----------------------------------------------------------------
        | --- LoadLastScene: Coroutine to Load the Last Active Scene --- |
        ----------------------------------------------------------------*/
        public IEnumerator LoadLastScene(string fileName)
        {
            SaveState state = LoadFile(fileName);
            int buildIndex = SceneManager.GetActiveScene().buildIndex;

            if (state.ContainsKey(kLastActiveSceneKey))
            {
                buildIndex = (int)state[kLastActiveSceneKey];
            }

            yield return SceneManager.LoadSceneAsync(buildIndex);
            RestoreState(state);
        }

        /*-----------------------------------------------------
        | --- SaveFileExists: Check if a save file exists --- |
        -----------------------------------------------------*/
        public bool SaveFileExists(string fileName)
        {
            string path = GetPathFromSaveFile(fileName);
            return File.Exists(path);
        }

        /*-------------------------------------------------------------
        | --- SaveFile: Serialize the state and save it to a file --- |
        -------------------------------------------------------------*/
        private void SaveFile(string fileName, object state)
        {
            string path = GetPathFromSaveFile(fileName);
            using FileStream stream = File.Open(path, FileMode.Create);
            BinaryFormatter formatter = new();
            formatter.Serialize(stream, state);
        }

        /*-------------------------------------------------------------
        | --- ListSaveFiles: List all existing save files on disk --- |
        -------------------------------------------------------------*/
        public IEnumerable<string> ListSaveFiles()
        {
            foreach (string path in Directory.EnumerateFiles(Application.persistentDataPath))
            {
                if (Path.GetExtension(path) == kSaveFileExt)
                {
                    yield return Path.GetFileNameWithoutExtension(path);
                }
            }
        }

        /*-----------------------------------------------------------
        | --- LoadFile: Deserialize the state from a saved file --- |
        -----------------------------------------------------------*/
        private SaveState LoadFile(string fileName)
        {
            string path = GetPathFromSaveFile(fileName);

            if (!File.Exists(path))
            {
                return new SaveState();
            }

            using FileStream stream = File.Open(path, FileMode.Open);
            BinaryFormatter formatter = new();
            return (SaveState)formatter.Deserialize(stream);
        }

        /*------------------------------------------------------------------
        | --- CaptureState: Capture the state of all saveable entities --- |
        ------------------------------------------------------------------*/
        private void CaptureState(SaveState state)
        {
            foreach (SaveableEntity saveable in m_registeredEntities)
            {
                state[saveable.UID] = saveable.CaptureState();
            }

            state[kLastActiveSceneKey] = SceneManager.GetActiveScene().buildIndex;
        }

        /*------------------------------------------------------------------
        | --- RestoreState: Restore the state of all saveable entities --- |
        ------------------------------------------------------------------*/
        private void RestoreState(SaveState state)
        {
            foreach (SaveableEntity saveable in m_registeredEntities)
            {
                string ID = saveable.UID;
                if (state.ContainsKey(ID))
                {
                    saveable.RestoreState(state[ID]);
                }
            }
        }

        /*------------------------------------------------------------------
        | --- GetPathFromSaveFile: Get the full path for the save file --- |
        ------------------------------------------------------------------*/
        private string GetPathFromSaveFile(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName + kSaveFileExt);
        }
    }
}