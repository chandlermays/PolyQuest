/*---------------------------
| File: SavingSystem.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
//---------------------------------

namespace PolyQuest.Saving
{
    using SaveState = IDictionary<string, JToken>;

    /* ---------------------------------------------------------------------------------------------
     * Role: Writes and reads a playthrough's save files, and restores the world from them.        *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Keeps two slots per playthrough: the manual save (pause menu) and the autosave.      *
     *      - Writes safely: a temporary file, the previous good file kept as .bak, then a swap.   *
     *      - Loads the newer of the two slots, falling back to backups and moving unreadable      *
     *        files aside (slot.corrupt-<timestamp>), and reports failure instead of throwing.     *
     *      - Keeps other scenes' state across saves, from memory or the backup, never from a     *
     *        file it can't read.                                                                  *
     *      - Restores each entity and component on its own, so one bad entry costs only itself.  *
     * ------------------------------------------------------------------------------------------- */
    public class SavingSystem : MonoBehaviour
    {
        private const string kLastActiveSceneKey = "LastActiveScene";
        private const string kSaveFileExtension = ".sav";
        private const string kBackupExtension = ".bak";
        private const string kTemporaryExtension = ".tmp";
        private const string kCorruptMarker = ".corrupt-";
        public const string kAutoSaveSuffix = "_autosave";

        private static readonly HashSet<SaveableEntity> m_registeredEntities = new();

        // The whole state of each playthrough as last loaded or saved this session, other scenes included.
        // Saving merges into this, so it never has to read (and trust) the file it's about to replace.
        private readonly Dictionary<string, JObject> m_knownState = new();

        // The slot this session wrote last for each playthrough: it's the newer one even when file times can't tell
        // two saves made within the same second apart
        private readonly Dictionary<string, string> m_lastWrittenSlot = new();

        // True while a load is putting saved state back, component by component. Systems that react to changes
        // (re-checking quests, clearing encounters) wait for RestoreFinished instead of acting on a half-loaded world.
        public static bool IsRestoring { get; private set; } = false;
        public static event Action RestoreFinished;

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

        /*------------------------------------------------------------------------------------------------
        | --- IsValidSaveName: Whether a playthrough name can be used as a file name on any platform --- |
        ------------------------------------------------------------------------------------------------*/
        public static bool IsValidSaveName(string saveName)
        {
            if (string.IsNullOrWhiteSpace(saveName) || saveName != saveName.Trim() || saveName.EndsWith("."))
                return false;

            // Windows' rules are the strictest; checked everywhere so a save made on one machine works on another
            const string kForbidden = "<>:\"/\\|?*";
            if (saveName.Any(c => c < 32 || kForbidden.IndexOf(c) >= 0) || saveName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;

            string device = saveName.Split('.')[0].ToUpperInvariant();
            string[] reserved = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                                  "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            if (reserved.Contains(device))
                return false;

            // A name ending like an autosave would share files with another playthrough
            return !saveName.EndsWith(kAutoSaveSuffix, StringComparison.OrdinalIgnoreCase);
        }

        /*---------------------------------------------------------------------
        | --- Save: Write the manual save (only the pause menu does this) --- |
        ---------------------------------------------------------------------*/
        public void Save(string playthrough)
        {
            WriteSlot(playthrough, playthrough);
        }

        /*--------------------------------------------------------------------------
        | --- AutoSave: Write the autosave slot, leaving the manual save alone --- |
        --------------------------------------------------------------------------*/
        public void AutoSave(string playthrough)
        {
            WriteSlot(playthrough, playthrough + kAutoSaveSuffix);
        }

        /*---------------------------------------------------------------------------------
        | --- SaveFileExists: Whether a playthrough has any save, manual or automatic --- |
        ---------------------------------------------------------------------------------*/
        public bool SaveFileExists(string playthrough)
        {
            return SlotsOf(playthrough).Any(slot => File.Exists(PathFor(slot)) || File.Exists(PathFor(slot) + kBackupExtension));
        }

        /*----------------------------------------------------------------------------------------------------------------------
        | --- Load: Restore the world from the newer of the playthrough's saves; false (and untouched) if none can be read --- |
        ----------------------------------------------------------------------------------------------------------------------*/
        public bool Load(string playthrough)
        {
            if (!TryReadPlaythrough(playthrough, out JObject state))
                return false;

            RestoreEntities(state);
            return true;
        }

        /*-----------------------------------------------------------------------------------------------------
        | --- ForgetState: Drops what's remembered about a playthrough, so a new one never merges into it --- |
        -----------------------------------------------------------------------------------------------------*/
        public void ForgetState(string playthrough)
        {
            if (playthrough == null)
                return;

            m_knownState.Remove(playthrough);
            m_lastWrittenSlot.Remove(playthrough);
        }

        /*---------------------------------------------------------------------------------------------------
        | --- Delete: Remove the playthrough's saves and backups; files moved aside are kept on purpose --- |
        ---------------------------------------------------------------------------------------------------*/
        public void Delete(string playthrough)
        {
            foreach (string slot in SlotsOf(playthrough))
            {
                string path = PathFor(slot);
                foreach (string file in new[] { path, path + kBackupExtension, path + kTemporaryExtension })
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
            ForgetState(playthrough);
        }

        /*--------------------------------------------------------------------------------------------------------
        | --- LoadLastScene: Loads the saved scene and restores it; onFinished(false) if nothing can be read --- |
        --------------------------------------------------------------------------------------------------------*/
        public IEnumerator LoadLastScene(string playthrough, Action<bool> onFinished = null)
        {
            // Read before leaving the current scene: with nothing readable, the game stays exactly as it is
            if (!TryReadPlaythrough(playthrough, out JObject state))
            {
                onFinished?.Invoke(false);
                yield break;
            }

            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            JToken savedScene = state[kLastActiveSceneKey];
            if (savedScene != null && savedScene.Type == JTokenType.Integer)
            {
                int savedIndex = savedScene.Value<int>();
                if (savedIndex >= 0 && savedIndex < SceneManager.sceneCountInBuildSettings)
                    buildIndex = savedIndex;
            }

            yield return SceneManager.LoadSceneAsync(buildIndex);
            RestoreEntities(state);
            onFinished?.Invoke(true);
        }

        /*---------------------------------------------------------------------------------------------------
        | --- ListSaveFiles: Each playthrough once, whatever mix of saves, autosaves and backups it has --- |
        ---------------------------------------------------------------------------------------------------*/
        public IEnumerable<string> ListSaveFiles()
        {
            HashSet<string> playthroughs = new();
            foreach (string path in Directory.EnumerateFiles(Application.persistentDataPath))
            {
                string fileName = Path.GetFileName(path);
                string slot;
                if (fileName.EndsWith(kSaveFileExtension))
                    slot = fileName.Substring(0, fileName.Length - kSaveFileExtension.Length);
                else if (fileName.EndsWith(kSaveFileExtension + kBackupExtension))
                    slot = fileName.Substring(0, fileName.Length - (kSaveFileExtension + kBackupExtension).Length);
                else
                    continue;       // temporary and moved-aside files aren't playthroughs

                if (slot.EndsWith(kAutoSaveSuffix))
                    slot = slot.Substring(0, slot.Length - kAutoSaveSuffix.Length);

                if (playthroughs.Add(slot))
                    yield return slot;
            }
        }

        /*---------------------------------------------------------------------------------------------------
        | --- WriteSlot: Merge the world into the playthrough's state and write it safely; never throws --- |
        ---------------------------------------------------------------------------------------------------*/
        private void WriteSlot(string playthrough, string slot)
        {
            string path = PathFor(slot);
            string temporary = path + kTemporaryExtension;
            string backup = path + kBackupExtension;

            try
            {
                // The file being replaced: a good one becomes the backup; an unreadable one is moved aside, never trusted
                JObject previous = null;
                if (File.Exists(path) && !TryReadFile(path, out previous))
                {
                    string movedTo = MoveAside(path, slot);
                    Debug.LogWarning($"SavingSystem: save '{slot}' couldn't be read, so it was moved aside to '{Path.GetFileName(movedTo)}' and a new save was written.");
                }

                // Other scenes' state comes from memory, else from the last good file (the one about to become the backup)
                JObject state = m_knownState.TryGetValue(playthrough, out JObject known) ? (JObject)known.DeepClone() : null;
                if (state == null && previous == null && File.Exists(backup))
                    TryReadFile(backup, out previous);
                state ??= previous ?? new JObject();

                CaptureEntities(state);
                WriteFile(temporary, state);

                if (File.Exists(path))
                    Swap(temporary, path, backup);
                else
                    File.Move(temporary, path);         // the very first save: nothing to replace

                m_knownState[playthrough] = state;
                m_lastWrittenSlot[playthrough] = slot;
            }
            catch (Exception exception)
            {
                // Saving runs from level-ups, quests, shops and portals: it must never break the game
                Debug.LogError($"SavingSystem: couldn't write save '{slot}'. The game goes on; the previous save is untouched. {exception.Message}");
                TryDelete(temporary);
            }
        }

        /*------------------------------------------------------------------------------------------------------------------
        | --- TryReadPlaythrough: The newer readable save of a playthrough, falling back to backups and the older slot --- |
        ------------------------------------------------------------------------------------------------------------------*/
        private bool TryReadPlaythrough(string playthrough, out JObject state)
        {
            state = null;
            if (string.IsNullOrEmpty(playthrough))
            {
                Debug.LogWarning("SavingSystem: no save is selected, so nothing was loaded.");
                return false;
            }

            // Newest first: the slot this session wrote last, else whichever file was written last (a tie goes to the manual save)
            m_lastWrittenSlot.TryGetValue(playthrough, out string writtenThisSession);
            IEnumerable<string> slots = SlotsOf(playthrough)
                .Where(slot => File.Exists(PathFor(slot)) || File.Exists(PathFor(slot) + kBackupExtension))
                .OrderByDescending(slot => slot == writtenThisSession)
                .ThenByDescending(slot => LastWritten(slot))
                .ThenBy(slot => slot.EndsWith(kAutoSaveSuffix));

            foreach (string slot in slots)
            {
                string path = PathFor(slot);
                if (File.Exists(path))
                {
                    if (TryReadFile(path, out state))
                        break;

                    string movedTo = MoveAside(path, slot);
                    Debug.LogWarning($"SavingSystem: save '{slot}' couldn't be read; it was moved aside to '{Path.GetFileName(movedTo)}'. Trying its backup.");
                }

                string backup = path + kBackupExtension;
                if (File.Exists(backup))
                {
                    if (TryReadFile(backup, out state))
                    {
                        Debug.LogWarning($"SavingSystem: loaded the backup of save '{slot}'.");
                        break;
                    }

                    string movedTo = MoveAside(backup, slot + kBackupExtension);
                    Debug.LogWarning($"SavingSystem: the backup of save '{slot}' couldn't be read either; it was moved aside to '{Path.GetFileName(movedTo)}'.");
                }
            }

            if (state == null)
            {
                Debug.LogWarning($"SavingSystem: nothing readable was found for save '{playthrough}', so nothing was loaded.");
                return false;
            }

            m_knownState[playthrough] = (JObject)state.DeepClone();
            return true;
        }

        /*---------------------------------------------------------------------
        | --- CaptureEntities: Capture the state of all saveable entities --- |
        ---------------------------------------------------------------------*/
        private void CaptureEntities(SaveState state)
        {
            foreach (SaveableEntity saveable in m_registeredEntities)
            {
                state[saveable.UID] = saveable.Capture();
            }
            state[kLastActiveSceneKey] = SceneManager.GetActiveScene().buildIndex;
        }

        /*---------------------------------------------------------------------------------------------------
        | --- RestoreEntities: Restore every entity; an empty or unreadable entry is skipped, not fatal --- |
        ---------------------------------------------------------------------------------------------------*/
        private void RestoreEntities(SaveState state)
        {
            List<SaveableEntity> restored = new();
            IsRestoring = true;
            try
            {
                // A copy: restoring can spawn or destroy entities (drops, pickups) while this runs
                foreach (SaveableEntity saveable in m_registeredEntities.ToList())
                {
                    if (saveable == null || !state.TryGetValue(saveable.UID, out JToken entry))
                        continue;

                    // An entity with no saveable components saves null; anything that isn't an object can't be restored
                    if (entry is not JObject)
                        continue;

                    saveable.Restore(entry);
                    restored.Add(saveable);
                }

                // Only once everything is restored: values that depend on other components (health, mana) are put back exactly
                foreach (SaveableEntity saveable in restored)
                {
                    if (saveable != null)
                    {
                        saveable.CompleteRestore();
                    }
                }
            }
            finally
            {
                IsRestoring = false;
            }

            // The load is completely finished: now the world may react to it
            RestoreFinished?.Invoke();
        }

        /*--------------------------------------------------------------------------------------------
        | --- TryReadFile: Reads a save file; false if it's missing, unreadable or not an object --- |
        --------------------------------------------------------------------------------------------*/
        private static bool TryReadFile(string path, out JObject state)
        {
            state = null;
            try
            {
                using var textReader = File.OpenText(path);
                using var reader = new JsonTextReader(textReader);
                reader.FloatParseHandling = FloatParseHandling.Double;
                state = JObject.Load(reader);
                return true;
            }
            catch (Exception exception) when (exception is JsonException || exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        /*---------------------------------------------------------------
        | --- WriteFile: Serialize the state and write it to a file --- |
        ---------------------------------------------------------------*/
        private static void WriteFile(string path, JObject state)
        {
            using var textWriter = File.CreateText(path);
            using var writer = new JsonTextWriter(textWriter);
            writer.Formatting = Formatting.Indented;
            state.WriteTo(writer);
        }

        /*-------------------------------------------------------------------------------------
        | --- Swap: Puts the new file in place, keeping the one it replaces as the backup --- |
        -------------------------------------------------------------------------------------*/
        private static void Swap(string temporary, string path, string backup)
        {
            try
            {
                File.Replace(temporary, path, backup);
            }
            catch (PlatformNotSupportedException)
            {
                if (File.Exists(backup))
                    File.Delete(backup);
                File.Move(path, backup);
                File.Move(temporary, path);
            }
        }

        /*---------------------------------------------------------------------------------------------------------------------
        | --- MoveAside: Keeps an unreadable file intact as <slot>.corrupt-<timestamp>, for bug reports; returns its path --- |
        ---------------------------------------------------------------------------------------------------------------------*/
        private static string MoveAside(string path, string slot)
        {
            string baseName = Path.Combine(Application.persistentDataPath, slot + kCorruptMarker + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            string destination = baseName;
            for (int i = 2; File.Exists(destination); ++i)
                destination = $"{baseName}-{i}";

            File.Move(path, destination);
            return destination;
        }

        /*---------------------------------------------------------------
        | --- TryDelete: Deletes a file if it's there, never throws --- |
        ---------------------------------------------------------------*/
        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception) { /* a leftover temporary file is harmless */ }
        }

        /*--------------------------------------------------------------------------------------------
        | --- LastWritten: When a slot was last written (its backup's time if only that is left) --- |
        --------------------------------------------------------------------------------------------*/
        private static DateTime LastWritten(string slot)
        {
            string path = PathFor(slot);
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : File.GetLastWriteTimeUtc(path + kBackupExtension);
        }

        /*----------------------------------------------------------------------
        | --- SlotsOf: The two slots of a playthrough: manual and autosave --- |
        ----------------------------------------------------------------------*/
        private static string[] SlotsOf(string playthrough) => new[] { playthrough, playthrough + kAutoSaveSuffix };

        /*-----------------------------------------------------------
        | --- PathFor: Get the full path for a slot's save file --- |
        -----------------------------------------------------------*/
        private static string PathFor(string slot)
        {
            return Path.Combine(Application.persistentDataPath, slot + kSaveFileExtension);
        }
    }
}