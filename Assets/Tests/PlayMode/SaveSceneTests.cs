/*---------------------------
| File: SaveSceneTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Saving;
using PolyQuest.SceneManagement;
using PolyQuest.UI.Core;

namespace PolyQuest.Tests
{
    // Saves in real scenes with the real SaveManager and fade: Continue and respawn when nothing can be loaded, and
    // New Game never touching an existing playthrough.
    public class SaveSceneTests
    {
        private const string kMainMenu = "Main Menu";
        private const string kFirstScene = "Route 1";
        private const string kSlot = "__polyquest_savescene_test__";
        private const string kCurrentSaveKey = "CurrentSaveName";
        private const string kCorruptText = "{ this is not json";
        private const float kContinueSettle = 7f;          // fade out (2 s) + load + fade in (2 s), with room to spare
        private const float kRespawnSettle = 12f;          // the 1.5 s respawn delay on top of that

        private bool m_hadSaveKey;
        private string m_previousSaveName;

        [SetUp]
        public void SetUp()
        {
            m_hadSaveKey = PlayerPrefs.HasKey(kCurrentSaveKey);
            m_previousSaveName = PlayerPrefs.GetString(kCurrentSaveKey);
            PlayerPrefs.SetString(kCurrentSaveKey, kSlot);
            DeleteSlotFiles();
            PlayerRig.DestroyPersistentObjects();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Unload the scene so its objects don't leak into the tests that run after this class
            Scene empty = SceneManager.CreateScene("SaveScene_Empty_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            List<Scene> loaded = new();
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != empty)
                    loaded.Add(scene);
            }
            foreach (Scene scene in loaded)
                yield return SceneManager.UnloadSceneAsync(scene);

            PlayerRig.DestroyPersistentObjects();
            DeleteSlotFiles();
            if (m_hadSaveKey)
                PlayerPrefs.SetString(kCurrentSaveKey, m_previousSaveName);
            else
                PlayerPrefs.DeleteKey(kCurrentSaveKey);
        }

        /* --- Helpers --- */

        private static string Dir => Application.persistentDataPath;
        private static string PathFor(string slot) => Path.Combine(Dir, slot + ".sav");

        private static void DeleteSlotFiles()
        {
            foreach (string path in Directory.GetFiles(Dir, kSlot + "*"))
                File.Delete(path);
        }

        private static IEnumerator LoadScene(string name)
        {
            yield return SceneManager.LoadSceneAsync(name);
            yield return null;
            Assert.That(SaveManager.Instance, Is.Not.Null, $"fixture: {name} did not spawn a SaveManager");
            Assert.That(TransitionFade.Instance, Is.Not.Null, $"fixture: {name} did not spawn a TransitionFade");
        }

        private static void CorruptSaveAndBackup(string slot)
        {
            File.WriteAllText(PathFor(slot), kCorruptText);
            File.WriteAllText(PathFor(slot) + ".bak", kCorruptText);
        }

        private static float FadeAlpha => TransitionFade.Instance.GetComponent<CanvasGroup>().alpha;

        private static MainMenuUI MainMenu()
        {
            MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            Assert.That(menu, Is.Not.Null, "fixture: the Main Menu has no MainMenuUI");
            return menu;
        }

        // An old playthrough with state in a scene the new game hasn't visited
        private static string WriteOldPlaythrough(string slot)
        {
            JObject old = new JObject { ["old-world-chest"] = new JObject { ["PolyQuest.Inventories.Wallet"] = 500 } };
            File.WriteAllText(PathFor(slot), old.ToString());
            return File.ReadAllText(PathFor(slot));
        }

        /* ============================== Nothing can be loaded ============================== */

        [UnityTest]
        [Description("Continue with a save and backup that are both unreadable stays at the main menu, fades back in and logs a warning naming the slot; the screen is never left black.")]
        public IEnumerator Continue_WithNothingReadable_StaysAtTheMainMenu_FadedIn()
        {
            yield return LoadScene(kMainMenu);
            CorruptSaveAndBackup(kSlot);
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(kSlot)));

            SaveManager.Instance.ContinueGame(kSlot);
            yield return new WaitForSecondsRealtime(kContinueSettle);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(kMainMenu));
            Assert.That(FadeAlpha, Is.EqualTo(0f).Within(1e-3f), "the screen was left faded out");
        }

        [UnityTest]
        [Description("Respawning with a save and backup that are both unreadable returns to the main menu, faded in, and logs a warning naming the slot.")]
        public IEnumerator Respawn_WithNothingReadable_ReturnsToTheMainMenu_FadedIn()
        {
            yield return LoadScene(kFirstScene);
            GameObject player = GameObject.FindWithTag("Player");
            Assert.That(player, Is.Not.Null, "fixture: the scene has no Player");
            GameObject bandit = new GameObject("Bandit");
            PlayModeTestKit.Set(bandit.AddComponent<FactionComponent>(), "m_faction", Faction.kEnemy);
            CorruptSaveAndBackup(kSlot);
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(kSlot)));

            player.GetComponent<HealthComponent>().TakeDamage(bandit, 100000f);
            yield return new WaitForSecondsRealtime(kRespawnSettle);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(kMainMenu));
            Assert.That(FadeAlpha, Is.EqualTo(0f).Within(1e-3f), "the screen was left faded out");
        }

        /* ============================== New Game ============================== */

        [UnityTest]
        [Description("New Game with the name of an existing playthrough is refused with the menu's error message: no game starts and the old save is untouched.")]
        public IEnumerator NewGame_WithANameInUse_IsRefused()
        {
            yield return LoadScene(kMainMenu);
            string name = kSlot + "Taken";
            string oldSave = WriteOldPlaythrough(name);
            MainMenuUI menu = MainMenu();
            PlayModeTestKit.Get<TMP_InputField>(menu, "m_newGameText").text = name;

            menu.NewGame();
            yield return new WaitForSecondsRealtime(kContinueSettle);

            Assert.That(PlayModeTestKit.Get<GameObject>(menu, "m_errorMessage").activeSelf, Is.True, "no error message");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(kMainMenu));
            Assert.That(File.ReadAllText(PathFor(name)), Is.EqualTo(oldSave));
        }

        [UnityTest]
        [Description("New Game with a name that can't be a file name is refused with the menu's error message instead of failing.")]
        public IEnumerator NewGame_WithANameThatCantBeAFileName_IsRefused()
        {
            yield return LoadScene(kMainMenu);
            MainMenuUI menu = MainMenu();
            PlayModeTestKit.Get<TMP_InputField>(menu, "m_newGameText").text = "hero/slot";

            menu.NewGame();
            yield return new WaitForSecondsRealtime(kContinueSettle);

            Assert.That(PlayModeTestKit.Get<GameObject>(menu, "m_errorMessage").activeSelf, Is.True, "no error message");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(kMainMenu));
        }

        [UnityTest]
        [Description("Whatever the menu does, the SaveManager never merges a new game into an existing save: either nothing starts and the old save is untouched, or the new game's save holds none of the old game's state.")]
        public IEnumerator NewGame_NeverMergesIntoAnExistingSave()
        {
            yield return LoadScene(kMainMenu);
            string name = kSlot + "Merge";
            string oldSave = WriteOldPlaythrough(name);
            LogAssert.ignoreFailingMessages = true;         // for this test only: a refusal may be reported

            SaveManager.Instance.NewGame(name);
            yield return new WaitForSecondsRealtime(kContinueSettle);

            if (SceneManager.GetActiveScene().name == kFirstScene)
            {
                JObject newSave = JObject.Parse(File.ReadAllText(PathFor(name)));
                Assert.That(newSave.ContainsKey("old-world-chest"), Is.False, "the new game inherited the old game's state");
            }
            else
            {
                Assert.That(File.ReadAllText(PathFor(name)), Is.EqualTo(oldSave), "a refused new game changed the old save");
            }
        }
    }
}