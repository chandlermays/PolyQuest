using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
//---------------------------------

namespace PolyQuest.Saving
{
    using SaveState = Dictionary<string, object>;

    public class SaveSystem : MonoBehaviour
    {
        private const string kLastActiveSceneKey = "LastActiveScene";
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
            if (state.ContainsKey(kLastActiveSceneKey))
            {
                int index = (int)state[kLastActiveSceneKey];  // Retrieve the last scene index

                if (index != SceneManager.GetActiveScene().buildIndex)
                {
                    yield return SceneManager.LoadSceneAsync(index);
                }
            }
            RestoreState(state);  // Restore the state after loading the scene
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
                state[saveable.GetUniqueIdentifier()] = saveable.CaptureState();
            }

            state[kLastActiveSceneKey] = SceneManager.GetActiveScene().buildIndex;  // Save the last scene index
        }

        /*------------------------------------------------------------------
        | --- RestoreState: Restore the state of all saveable entities --- |
        ------------------------------------------------------------------*/
        private void RestoreState(SaveState state)
        {
            foreach (SaveableEntity saveable in m_registeredEntities)
            {
                string ID = saveable.GetUniqueIdentifier();
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
            return Path.Combine(Application.persistentDataPath, fileName + ".sav");
        }
    }
}