/*---------------------------
| File: SaveFileTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Saving;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace PolyQuest.Tests
{
    // The save files themselves: backups, falling back to them, moving unreadable files aside, merging other scenes'
    // state, manual saves vs autosaves, deleting and listing playthroughs, and one bad entity or component costing
    // only itself. Uses a real SaveManager and SavingSystem on a throwaway slot, with test-made entities holding a Wallet.
    public class SaveFileTests
    {
        private const string kSlot = "__polyquest_savefile_test__";
        private const string kCurrentSaveKey = "CurrentSaveName";
        private const string kWalletKey = "PolyQuest.Inventories.Wallet";
        private const string kCorruptText = "{ this is not json";

        private bool m_hadSaveKey;
        private string m_previousSaveName;
        private GameObject m_saveSystem;
        private readonly List<GameObject> m_entities = new();

        /*-----------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for save file testing --- |
        -----------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_hadSaveKey = PlayerPrefs.HasKey(kCurrentSaveKey);
            m_previousSaveName = PlayerPrefs.GetString(kCurrentSaveKey);
            PlayerPrefs.SetString(kCurrentSaveKey, kSlot);
            DeleteSlotFiles();
            PlayerRig.DestroyPersistentObjects();
            StartSaveSystem();
        }

        /*-------------------------------------------------------------------
        | --- TearDown: Release test fixtures and game object resources --- |
        -------------------------------------------------------------------*/
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject entity in m_entities)
            {
                if (entity != null)
                    Object.DestroyImmediate(entity);
            }
            m_entities.Clear();
            Object.DestroyImmediate(m_saveSystem);
            DeleteSlotFiles();
            if (m_hadSaveKey)
                PlayerPrefs.SetString(kCurrentSaveKey, m_previousSaveName);
            else
                PlayerPrefs.DeleteKey(kCurrentSaveKey);
        }


        /*-----------------
        | --- Helpers --- |
        -----------------*/
        /*----------------------------------------
        | --- Saves: The current SaveManager --- |
        ----------------------------------------*/
        private static SaveManager Saves => SaveManager.Instance;

        /*---------------------------------------------------
        | --- Dir: The folder save files are written to --- |
        ---------------------------------------------------*/
        private static string Dir => Application.persistentDataPath;

        /*-------------------------------------------------------
        | --- MainPath: Path of the test slot's manual save --- |
        -------------------------------------------------------*/
        private static string MainPath => Path.Combine(Dir, kSlot + ".sav");

        /*----------------------------------------------------------
        | --- MainBackupPath: Path of the manual save's backup --- |
        ----------------------------------------------------------*/
        private static string MainBackupPath => MainPath + ".bak";

        /*----------------------------------------------------
        | --- AutoPath: Path of the test slot's autosave --- |
        ----------------------------------------------------*/
        private static string AutoPath => Path.Combine(Dir, kSlot + "_autosave.sav");

        /*-------------------------------------------------------
        | --- AutoBackupPath: Path of the autosave's backup --- |
        -------------------------------------------------------*/
        private static string AutoBackupPath => AutoPath + ".bak";

        /*-----------------------------------------------------------------------------------------------------------
        | --- SlotFiles: Every file whose name starts with the test slot, including slots made by the list test --- |
        -----------------------------------------------------------------------------------------------------------*/
        private static string[] SlotFiles() => Directory.GetFiles(Dir, kSlot + "*");

        /*--------------------------------------------------------------
        | --- DeleteSlotFiles: Deletes every file of the test slot --- |
        --------------------------------------------------------------*/
        private static void DeleteSlotFiles()
        {
            foreach (string path in SlotFiles())
                File.Delete(path);
        }

        /*-----------------------------------------------------------------------------------------------------
        | --- MovedAsideFiles: Unreadable files moved aside, which keep the slot's name and say "corrupt" --- |
        -----------------------------------------------------------------------------------------------------*/
        private static string[] MovedAsideFiles()
            => SlotFiles().Where(p => Path.GetFileName(p).ToLowerInvariant().Contains("corrupt")).ToArray();

        /*----------------------------------------------------------------
        | --- StartSaveSystem: Starts a SaveManager and SavingSystem --- |
        ----------------------------------------------------------------*/
        private void StartSaveSystem()
        {
            m_saveSystem = new GameObject("TestSaveSystem");
            m_saveSystem.AddComponent<SavingSystem>();
            m_saveSystem.AddComponent<SaveManager>();
            Assert.That(SaveManager.Instance, Is.Not.Null, "fixture: the SaveManager did not start");
        }

        /*--------------------------------------------------------------------------------------------------------------------
        | --- RestartSaveSystem: Destroys and restarts the save system, as if the game had been closed and started again --- |
        --------------------------------------------------------------------------------------------------------------------*/
        private void RestartSaveSystem()
        {
            Object.DestroyImmediate(m_saveSystem);
            StartSaveSystem();
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------
        | --- Entity: Creates a saveable entity with a Wallet holding the given silver, optionally with a component that throws on restore --- |
        --------------------------------------------------------------------------------------------------------------------------------------*/
        private Wallet Entity(string uid, int silver, bool throwsOnRestore = false)
        {
            GameObject entity = new GameObject("Entity " + uid);
            entity.SetActive(false);
            m_entities.Add(entity);
            if (throwsOnRestore)
                entity.AddComponent<ThrowingSaveable>();
            Wallet wallet = entity.AddComponent<Wallet>();
            wallet.UpdateSilver(silver);
            PlayModeTestKit.Set(entity.AddComponent<SaveableEntity>(), "m_uniqueIdentifier", uid);
            entity.SetActive(true);
            return wallet;
        }

        /*----------------------------------------------------------------------------
        | --- EmptyEntity: Creates a saveable entity with no saveable components --- |
        ----------------------------------------------------------------------------*/
        private GameObject EmptyEntity(string uid)
        {
            GameObject entity = new GameObject("Entity " + uid);
            entity.SetActive(false);
            m_entities.Add(entity);
            PlayModeTestKit.Set(entity.AddComponent<SaveableEntity>(), "m_uniqueIdentifier", uid);
            entity.SetActive(true);
            return entity;
        }

        /*--------------------------------------------
        | --- SetSilver: Sets a wallet's balance --- |
        --------------------------------------------*/
        private static void SetSilver(Wallet wallet, int silver) => wallet.UpdateSilver(silver - wallet.CurrentSilver);

        /*--------------------------------------------------------------------
        | --- ReadFile: Parses a save file, failing if it isn't readable --- |
        --------------------------------------------------------------------*/
        private static JObject ReadFile(string path)
        {
            JObject file = null;
            Assert.DoesNotThrow(() => file = JObject.Parse(File.ReadAllText(path)), $"{Path.GetFileName(path)} is not readable");
            return file;
        }

        /*--------------------------------------------------------------------
        | --- SavedSilver: The silver saved for an entity in a save file --- |
        --------------------------------------------------------------------*/
        private static int SavedSilver(JObject file, string uid) => file[uid][kWalletKey].ToObject<int>();

        /*---------------------------------------------------------
        | --- Corrupt: Overwrites a file with unreadable text --- |
        ---------------------------------------------------------*/
        private static void Corrupt(string path) => File.WriteAllText(path, kCorruptText);

        /*---------------------------------------------------------------------------------------
        | --- Later: Waits long enough for any file-time resolution to tell two saves apart --- |
        ---------------------------------------------------------------------------------------*/
        private static IEnumerator Later() { yield return new WaitForSecondsRealtime(1.1f); }


        /*-----------------
        | --- Backups --- |
        -----------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Save_KeepsThePreviousSaveAsTheBackup: Saving keeps the previous good save as slot.sav.bak, and leaves no other file behind: no temporary file survives the swap --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("Saving keeps the previous good save as slot.sav.bak, and leaves no other file behind: no temporary file survives the swap.")]
        public IEnumerator Save_KeepsThePreviousSaveAsTheBackup()
        {
            yield return null;
            Wallet wallet = Entity("hero", 10);
            Saves.Save();
            SetSilver(wallet, 20);

            Saves.Save();

            Assert.That(SavedSilver(ReadFile(MainPath), "hero"), Is.EqualTo(20));
            Assert.That(File.Exists(MainBackupPath), Is.True, "no backup was kept");
            Assert.That(SavedSilver(ReadFile(MainBackupPath), "hero"), Is.EqualTo(10));
            Assert.That(SlotFiles().Select(Path.GetFileName), Is.EquivalentTo(new[] { kSlot + ".sav", kSlot + ".sav.bak" }));
        }

        /*--------------------------------------------------------------------------------------------------------
        | --- AutoSave_KeepsItsOwnBackup: The autosave keeps its own backup, the same way the main save does --- |
        --------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("The autosave keeps its own backup, the same way the main save does.")]
        public IEnumerator AutoSave_KeepsItsOwnBackup()
        {
            yield return null;
            Wallet wallet = Entity("hero", 10);
            Saves.AutoSave();
            SetSilver(wallet, 20);

            Saves.AutoSave();

            Assert.That(File.Exists(AutoBackupPath), Is.True, "no backup was kept");
            Assert.That(SavedSilver(ReadFile(AutoBackupPath), "hero"), Is.EqualTo(10));
        }


        /*------------------------------------
        | --- Loading an Unreadable Save --- |
        ------------------------------------*/
        /*-----------------------------------------------------------------------------------------------------------------------------
        | --- SaveTwiceThenCorruptTheMainFile: Saves twice, corrupts the main save, then changes the silver so a load can be seen --- |
        -----------------------------------------------------------------------------------------------------------------------------*/
        private Wallet SaveTwiceThenCorruptTheMainFile()
        {
            Wallet wallet = Entity("hero", 10);
            Saves.Save();
            SetSilver(wallet, 20);
            Saves.Save();
            Corrupt(MainPath);
            SetSilver(wallet, 99);
            return wallet;
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Load_UnreadableSave_FallsBackToTheBackup_AndWarns: Loading when the main save is unreadable falls back to its backup and logs a warning naming the slot --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("Loading when the main save is unreadable falls back to its backup and logs a warning naming the slot.")]
        public IEnumerator Load_UnreadableSave_FallsBackToTheBackup_AndWarns()
        {
            yield return null;
            Wallet wallet = SaveTwiceThenCorruptTheMainFile();
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(kSlot)));

            Assert.DoesNotThrow(() => Saves.Load());

            Assert.That(wallet.CurrentSilver, Is.EqualTo(10));
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Load_UnreadableSave_IsMovedAside_Intact: An unreadable save found while loading is moved aside, kept intact for bug reports, not overwritten or deleted --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("An unreadable save found while loading is moved aside, kept intact for bug reports, not overwritten or deleted.")]
        public IEnumerator Load_UnreadableSave_IsMovedAside_Intact()
        {
            yield return null;
            SaveTwiceThenCorruptTheMainFile();
            LogAssert.ignoreFailingMessages = true;         // for this test only: the warning is pinned by the fallback test

            try { Saves.Load(); } catch { /* the fallback test reports a throw */ }

            Assert.That(MovedAsideFiles().Select(File.ReadAllText), Has.Member(kCorruptText));
        }


        /*----------------------------------------
        | --- Saving Over an Unreadable Save --- |
        ----------------------------------------*/
        /*------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- SaveOverAnUnreadableSave_KeepsOtherScenesState_FromThisSession: Saving over an unreadable save keeps other scenes' state from the last save made this session, and moves the unreadable file aside --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("Saving over an unreadable save keeps other scenes' state from the last save made this session, and moves the unreadable file aside.")]
        public IEnumerator SaveOverAnUnreadableSave_KeepsOtherScenesState_FromThisSession()
        {
            yield return null;
            Wallet hero = Entity("hero", 10);
            Wallet otherScene = Entity("other-scene-chest", 7);
            Saves.Save();
            Object.DestroyImmediate(otherScene.gameObject);      // its scene unloads
            Corrupt(MainPath);
            SetSilver(hero, 50);
            LogAssert.ignoreFailingMessages = true;         // for this test only: how the unreadable file is reported isn't pinned here

            Assert.DoesNotThrow(() => Saves.Save());

            JObject file = ReadFile(MainPath);
            Assert.That(SavedSilver(file, "hero"), Is.EqualTo(50));
            Assert.That(file.ContainsKey("other-scene-chest"), Is.True, "the other scene's state was lost");
            Assert.That(MovedAsideFiles().Select(File.ReadAllText), Has.Member(kCorruptText));
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- AfterARestart_SaveOverAnUnreadableSave_KeepsOtherScenesState_FromTheBackup: After a restart, saving over an unreadable save keeps other scenes' state from the backup --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("After a restart, saving over an unreadable save keeps other scenes' state from the backup.")]
        public IEnumerator AfterARestart_SaveOverAnUnreadableSave_KeepsOtherScenesState_FromTheBackup()
        {
            yield return null;
            Wallet hero = Entity("hero", 10);
            Wallet otherScene = Entity("other-scene-chest", 7);
            Saves.Save();
            Saves.Save();                                   // the backup now holds the chest too
            Object.DestroyImmediate(otherScene.gameObject);
            RestartSaveSystem();
            Corrupt(MainPath);
            SetSilver(hero, 50);
            LogAssert.ignoreFailingMessages = true;         // for this test only: how the unreadable file is reported isn't pinned here

            Assert.DoesNotThrow(() => Saves.Save());

            JObject file = ReadFile(MainPath);
            Assert.That(SavedSilver(file, "hero"), Is.EqualTo(50));
            Assert.That(file.ContainsKey("other-scene-chest"), Is.True, "the other scene's state was lost");
        }


        /*----------------------------------
        | --- Manual Saves & Autosaves --- |
        ----------------------------------*/
        /*-----------------------------------------------------------------------------------------------------------------------------
        | --- Load_UsesTheNewerOfTheManualSaveAndTheAutosave: Loading uses whichever of the manual save and the autosave is newer --- |
        -----------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("Loading uses whichever of the manual save and the autosave is newer.")]
        public IEnumerator Load_UsesTheNewerOfTheManualSaveAndTheAutosave()
        {
            yield return null;
            Wallet wallet = Entity("hero", 10);
            Saves.Save();
            yield return Later();
            SetSilver(wallet, 20);
            Saves.AutoSave();
            SetSilver(wallet, 99);

            Saves.Load();
            Assert.That(wallet.CurrentSilver, Is.EqualTo(20), "the autosave is newer");

            yield return Later();
            SetSilver(wallet, 30);
            Saves.Save();
            SetSilver(wallet, 99);

            Saves.Load();
            Assert.That(wallet.CurrentSilver, Is.EqualTo(30), "the manual save is newer");
        }


        /*----------------------------
        | --- Deleting & Listing --- |
        ----------------------------*/
        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Delete_RemovesEveryFileOfThePlaythrough_ButKeepsMovedAsideFiles: Deleting a playthrough removes its manual save, autosave and both backups, and keeps any moved-aside unreadable file --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("Deleting a playthrough removes its manual save, autosave and both backups, and keeps any moved-aside unreadable file.")]
        public IEnumerator Delete_RemovesEveryFileOfThePlaythrough_ButKeepsMovedAsideFiles()
        {
            yield return null;
            string movedAside = Path.Combine(Dir, kSlot + ".corrupt-20260101-000000");
            foreach (string path in new[] { MainPath, MainBackupPath, AutoPath, AutoBackupPath })
                File.WriteAllText(path, "{}");
            File.WriteAllText(movedAside, kCorruptText);

            Saves.Delete(kSlot);

            Assert.That(SlotFiles(), Is.EquivalentTo(new[] { movedAside }));
        }

        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- ListSaveFiles_ShowsEachPlaythroughOnce: The saves list shows each playthrough once, whether it has a manual save, an autosave or both, and never lists backups or moved-aside files --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("The saves list shows each playthrough once, whether it has a manual save, an autosave or both, and never lists backups or moved-aside files.")]
        public IEnumerator ListSaveFiles_ShowsEachPlaythroughOnce()
        {
            yield return null;
            string both = kSlot + "Both", manual = kSlot + "Manual", auto = kSlot + "Auto";
            foreach (string name in new[] { both + ".sav", both + ".sav.bak", both + "_autosave.sav", both + "_autosave.sav.bak",
                                             manual + ".sav", auto + "_autosave.sav", both + ".corrupt-20260101-000000" })
                File.WriteAllText(Path.Combine(Dir, name), "{}");

            IEnumerable<string> listed = Saves.ListSaveFiles().Where(s => s.StartsWith(kSlot));

            Assert.That(listed, Is.EquivalentTo(new[] { both, manual, auto }));
        }


        /*-----------------------------------------
        | --- One Bad Entry Costs Only Itself --- |
        -----------------------------------------*/
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- EmptyOrUnreadableEntityEntries_AreSkipped_TheRestLoad: Entities with no saveable components save and load, and an entity entry that isn't an object is skipped; every other entity still loads --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("Entities with no saveable components save and load, and an entity entry that isn't an object is skipped; every other entity still loads.")]
        public IEnumerator EmptyOrUnreadableEntityEntries_AreSkipped_TheRestLoad()
        {
            yield return null;
            EmptyEntity("signpost");
            Wallet damaged = Entity("damaged", 5);
            Wallet hero = Entity("hero", 40);
            Saves.Save();
            JObject file = ReadFile(MainPath);
            file["damaged"] = 5;
            File.WriteAllText(MainPath, file.ToString());
            SetSilver(hero, 0);
            SetSilver(damaged, 3);

            Assert.DoesNotThrow(() => Saves.Load());

            Assert.That(hero.CurrentSilver, Is.EqualTo(40));
            Assert.That(damaged.CurrentSilver, Is.EqualTo(3));
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- AComponentThatThrowsOnRestore_CostsOnlyItself: A component whose RestoreState throws costs only itself: the other components on its entity and every other entity still restore --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("A component whose RestoreState throws costs only itself: the other components on its entity and every other entity still restore.")]
        public IEnumerator AComponentThatThrowsOnRestore_CostsOnlyItself()
        {
            yield return null;
            Wallet sameEntity = Entity("buggy", 10, throwsOnRestore: true);
            Wallet otherEntity = Entity("hero", 20);
            Saves.Save();
            SetSilver(sameEntity, 0);
            SetSilver(otherEntity, 0);
            LogAssert.ignoreFailingMessages = true;         // for this test only: how the failure is reported isn't pinned here

            Assert.DoesNotThrow(() => Saves.Load());

            Assert.That(sameEntity.CurrentSilver, Is.EqualTo(10), "the other component on the same entity");
            Assert.That(otherEntity.CurrentSilver, Is.EqualTo(20), "another entity");
        }
    }
}