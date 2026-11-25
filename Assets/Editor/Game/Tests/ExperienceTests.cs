/*---------------------------
| File: ExperienceTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Tests
{
    // Gaining XP, leveling up, the XP bar's per-level values and the save data. XP is a running total; the table holds
    // the total needed for each level. At the max level XP still adds up, but the bar and any rewards past the cap
    // aren't designed yet, so they aren't tested here.
    public class ExperienceTests
    {
        private const float kTolerance = 1e-4f;

        private InventoryTestKit m_kit;
        private BaseStats m_stats;
        private Experience m_experience;
        private int m_levelUps, m_xpChanges;

        /*------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for experience testing --- |
        ------------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_kit.IsolateSaves();                                       // leveling up auto-saves
            (m_stats, m_experience) = NewPlayer("Player");
            m_levelUps = m_xpChanges = 0;
            m_experience.OnLevelUp += () => ++m_levelUps;
            m_experience.OnExperienceChanged += () => ++m_xpChanges;
        }

        /*-------------------------------------------------------------------
        | --- TearDown: Release test fixtures and game object resources --- |
        -------------------------------------------------------------------*/
        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }


        /*-----------------
        | --- Helpers --- |
        -----------------*/
        /*----------------------------------------------------------------------------------------------------------------------------
        | --- NewPlayer: Creates a Player with BaseStats on a test progression and an Experience component with its level-up VFX --- |
        ----------------------------------------------------------------------------------------------------------------------------*/
        private (BaseStats, Experience) NewPlayer(string name)
        {
            PlayerProgression progression = m_kit.Track(ScriptableObject.CreateInstance<PlayerProgression>());
            SerializedTestUtil.SetIntArray(progression, "m_maxHealthAmounts", 100, 110, 121, 133, 146);
            SerializedTestUtil.SetIntArray(progression, "m_maxExperienceAmounts", 100, 250, 475, 813, 1300);
            SerializedTestUtil.SetIntArray(progression, "m_maxAttributePoints", 4, 8, 12, 16, 20);

            GameObject player = m_kit.NewObject(name);
            BaseStats stats = player.AddComponent<BaseStats>();
            SerializedTestUtil.SetObject(stats, "m_baseProgression", progression);

            GameObject vfx = m_kit.NewObject(name + " LevelUpVFX");
            vfx.transform.SetParent(player.transform);
            Experience experience = player.AddComponent<Experience>();
            SerializedTestUtil.SetObject(experience, "m_levelUpVFX", vfx.AddComponent<ParticleSystem>());
            InventoryTestKit.InvokeAwake(experience);
            return (stats, experience);
        }


        /*--------------------
        | --- Gaining XP --- |
        --------------------*/
        /*---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- GainExperience_BelowTheGoal_AddsIt_WithoutLevelingUp: XP below the next level's goal is added to the total and raises OnExperienceChanged, with no level-up --- |
        ---------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("XP below the next level's goal is added to the total and raises OnExperienceChanged, with no level-up.")]
        public void GainExperience_BelowTheGoal_AddsIt_WithoutLevelingUp()
        {
            m_experience.GainExperience(60f);

            Assert.That(m_experience.ExperiencePoints, Is.EqualTo(60f).Within(kTolerance));
            Assert.That(m_xpChanges, Is.EqualTo(1));
            Assert.That(m_stats.Level, Is.EqualTo(1));
            Assert.That(m_levelUps, Is.EqualTo(0));
        }

        /*------------------------------------------------------------------------------------------------------------------
        | --- ReachingTheGoal_LevelsUpOnce: Reaching the next level's XP goal levels up once and raises OnLevelUp once --- |
        ------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Reaching the next level's XP goal levels up once and raises OnLevelUp once.")]
        public void ReachingTheGoal_LevelsUpOnce()
        {
            m_experience.GainExperience(60f);

            m_experience.GainExperience(40f);

            Assert.That(m_stats.Level, Is.EqualTo(2));
            Assert.That(m_levelUps, Is.EqualTo(1));
        }

        /*--------------------------------------------------------------------------------------------------------------------
        | --- XpForSeveralLevels_LevelsUpOncePerLevel: XP enough for several levels at once gives one level-up per level --- |
        --------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("XP enough for several levels at once gives one level-up per level.")]
        public void XpForSeveralLevels_LevelsUpOncePerLevel()
        {
            m_experience.GainExperience(300f);

            Assert.That(m_stats.Level, Is.EqualTo(3));
            Assert.That(m_levelUps, Is.EqualTo(2));
            Assert.That(m_experience.ExperiencePoints, Is.EqualTo(300f).Within(kTolerance), "XP is a running total");
        }

        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- GainExperience_AtMaxLevel_AddsToTheTotal_WithoutLevelingUp: At the max level, XP is added to the total and nothing else happens: no level past the cap, no level-up --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Reaching the max level stops there, and XP gained at the max level is added to the total with no level past the cap and no OnLevelUp.")]
        public void GainExperience_AtMaxLevel_AddsToTheTotal_WithoutLevelingUp()
        {
            m_experience.GainExperience(1300f);                 // exactly the last row: reaches the max level, 5
            Assert.That(m_stats.Level, Is.EqualTo(m_stats.MaxLevel));
            Assert.That(m_levelUps, Is.EqualTo(m_stats.MaxLevel - 1));
            m_levelUps = 0;

            m_experience.GainExperience(5000f);

            Assert.That(m_stats.Level, Is.EqualTo(m_stats.MaxLevel));
            Assert.That(m_levelUps, Is.EqualTo(0));
            Assert.That(m_experience.ExperiencePoints, Is.EqualTo(6300f).Within(kTolerance));
        }


        /*--------------------
        | --- The XP Bar --- |
        --------------------*/
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CurrentLevelXp_IsProgressWithinTheLevel: The XP bar shows progress within the current level: XP past the previous level's total, out of the XP between that total and this level's --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [TestCase(40f, 40f, 100f)]
        [TestCase(130f, 30f, 150f)]
        [TestCase(300f, 50f, 225f)]
        [Description("The XP bar shows progress within the current level: XP past the previous level's total, out of the XP between that total and this level's.")]
        public void CurrentLevelXp_IsProgressWithinTheLevel(float totalXp, float expectedProgress, float expectedRequired)
        {
            m_experience.GainExperience(totalXp);

            Assert.That(m_experience.GetCurrentLevelXP, Is.EqualTo(expectedProgress).Within(kTolerance));
            Assert.That(m_experience.GetCurrentLevelXPRequired, Is.EqualTo(expectedRequired).Within(kTolerance));
        }


        /*----------------
        | --- Saving --- |
        ----------------*/
        /*------------------------------------------------------------------------------------------------------------------------------------------------
        | --- CaptureThenRestore_RestoresXpAndLevel: Capturing XP and level and restoring them into a fresh Player restores both, without a level-up --- |
        ------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Capturing XP and level and restoring them into a fresh Player restores both, without a level-up.")]
        public void CaptureThenRestore_RestoresXpAndLevel()
        {
            m_experience.GainExperience(300f);
            (BaseStats freshStats, Experience fresh) = NewPlayer("FreshPlayer");
            int freshLevelUps = 0;
            fresh.OnLevelUp += () => ++freshLevelUps;

            fresh.RestoreState(m_experience.CaptureState());

            Assert.That(fresh.ExperiencePoints, Is.EqualTo(300f).Within(kTolerance));
            Assert.That(freshStats.Level, Is.EqualTo(3));
            Assert.That(freshLevelUps, Is.EqualTo(0));
        }

        /*---------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_Unreadable_IsIgnored: Experience save data that can't be read is ignored: XP and level are unchanged --- |
        ---------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Experience save data that can't be read is ignored: XP and level are unchanged.")]
        public void RestoreState_Unreadable_IsIgnored()
        {
            m_experience.GainExperience(130f);

            m_experience.RestoreState(new JValue("lots"));
            m_experience.RestoreState(new JObject { ["experiencePoints"] = "lots", ["level"] = 4 });
            m_experience.RestoreState(new JObject { ["experiencePoints"] = 500f, ["level"] = "four" });

            Assert.That(m_experience.ExperiencePoints, Is.EqualTo(130f).Within(kTolerance));
            Assert.That(m_stats.Level, Is.EqualTo(2));
        }
    }
}