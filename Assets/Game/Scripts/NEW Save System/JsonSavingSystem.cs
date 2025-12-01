using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
//---------------------------------

namespace PolyQuest.Saving
{
    public class JsonSavingSystem : MonoBehaviour
    {
        private const string kLastActiveSceneKey = "LastActiveScene";
        private const string kSaveFileExtension = ".sav";

        private static readonly HashSet<JsonSaveableEntity> m_registeredEntities = new();

        /*-----------------------------------------------------------------------
        | --- RegisterSaveableEntity: Register a Saveable Entity for Saving --- |
        -----------------------------------------------------------------------*/
        public static void RegisterSaveableEntity(JsonSaveableEntity entity)
        {
            m_registeredEntities.Add(entity);
        }

        /*----------------------------------------------------------------
        | --- UnregisterSaveableEntity: Unregister a Saveable Entity --- |
        ----------------------------------------------------------------*/
        public static void UnregisterSaveableEntity(JsonSaveableEntity entity)
        {
            m_registeredEntities.Remove(entity);
        }

        /*---------------------------------------------------------------
        | --- Save: Capture the current state and save it to a file --- |
        ---------------------------------------------------------------*/
        public void Save(string saveFile)
        {
            JObject state = LoadJsonFromFile(saveFile);
            CaptureAsToken(state);
            SaveFileAsJson(saveFile, state);
        }

        /*-----------------------------------------------------
        | --- SaveFileExists: Check if a save file exists --- |
        -----------------------------------------------------*/
        public bool SaveFileExists(string fileName)
        {
            string path = GetPathFromSaveFile(fileName);
            return File.Exists(path);
        }

        /*---------------------------------------------------
        | --- Load: Restore the state from a saved file --- |
        ---------------------------------------------------*/
        public void Load(string fileName)
        {
            RestoreFromToken(LoadJsonFromFile(fileName));
        }

        /*-----------------------------------------------
        | --- Delete: Remove a saved file from disk --- |
        -----------------------------------------------*/
        public void Delete(string fileName)
        {
            File.Delete(GetPathFromSaveFile(fileName));
        }

        /*----------------------------------------------------------------
        | --- LoadLastScene: Coroutine to Load the Last Active Scene --- |
        ----------------------------------------------------------------*/
        public IEnumerator LoadLastScene(string saveFile)
        {
            JObject state = LoadJsonFromFile(saveFile);
            IDictionary<string, JToken> stateDict = state;
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            if (stateDict.ContainsKey(kLastActiveSceneKey))
            {
                buildIndex = (int)stateDict[kLastActiveSceneKey];
            }
            yield return SceneManager.LoadSceneAsync(buildIndex);
            RestoreFromToken(state);
        }

        /*-------------------------------------------------------------
        | --- ListSaveFiles: List all existing save files on disk --- |
        -------------------------------------------------------------*/
        public IEnumerable<string> ListSaveFiles()
        {
            foreach (string path in Directory.EnumerateFiles(Application.persistentDataPath))
            {
                if (Path.GetExtension(path) == kSaveFileExtension)
                {
                    yield return Path.GetFileNameWithoutExtension(path);
                }
            }
        }

        /*-----------------------------------------------------------
        | --- LoadFile: Deserialize the state from a saved file --- |
        -----------------------------------------------------------*/
        private JObject LoadJsonFromFile(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);

            if (!File.Exists(path))
                return new JObject();

            using (var textReader = File.OpenText(path))
            {
                using (var reader = new JsonTextReader(textReader))
                {
                    reader.FloatParseHandling = FloatParseHandling.Double;
                    return JObject.Load(reader);
                }
            }
        }

        /*-------------------------------------------------------------
        | --- SaveFile: Serialize the state and save it to a file --- |
        -------------------------------------------------------------*/
        private void SaveFileAsJson(string saveFile, JObject state)
        {
            string path = GetPathFromSaveFile(saveFile);

            using (var textWriter = File.CreateText(path))
            {
                using (var writer = new JsonTextWriter(textWriter))
                {
                    writer.Formatting = Formatting.Indented;
                    state.WriteTo(writer);
                }
            }
        }

        /*------------------------------------------------------------------
        | --- CaptureState: Capture the state of all saveable entities --- |
        ------------------------------------------------------------------*/
        private void CaptureAsToken(JObject state)
        {
            IDictionary<string, JToken> stateDict = state;
            foreach (JsonSaveableEntity saveable in m_registeredEntities)
            {
                stateDict[saveable.UID] = saveable.CaptureAsJToken();
            }

            stateDict[kLastActiveSceneKey] = SceneManager.GetActiveScene().buildIndex;
        }

        /*------------------------------------------------------------------
        | --- RestoreState: Restore the state of all saveable entities --- |
        ------------------------------------------------------------------*/
        private void RestoreFromToken(JObject state)
        {
            IDictionary<string, JToken> stateDict = state;
            foreach (JsonSaveableEntity saveable in m_registeredEntities)
            {
                string ID = saveable.UID;
                if (stateDict.ContainsKey(ID))
                {
                    saveable.RestoreFromJToken(stateDict[ID]);
                }
            }
        }

        /*------------------------------------------------------------------
        | --- GetPathFromSaveFile: Get the full path for the save file --- |
        ------------------------------------------------------------------*/
        private string GetPathFromSaveFile(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName + kSaveFileExtension);
        }
    }
}