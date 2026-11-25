/*---------------------------
| File: WorldFlagsTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Quests;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    // The facts about the world the player has caused: set one way, answered as kWorldFlag, saved by ID.
    public class WorldFlagsTests
    {
        private InventoryTestKit m_kit;
        private WorldFlags m_flags;
        private WorldFlag m_campEntered, m_campCleared;

        private int m_setEvents;

        /*------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for world flag testing --- |
        ------------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_campEntered = m_kit.Flag("camp_entered");
            m_campCleared = m_kit.Flag("camp_cleared");
            m_flags = m_kit.NewObject("Player").AddComponent<WorldFlags>();

            m_setEvents = 0;
            m_flags.OnFlagSet += _ => ++m_setEvents;
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
        | --- Setting --- |
        -----------------*/
        /*-----------------------------------------------------------------------------------------------------------------------
        | --- Set_RecordsTheFactOnce: Setting a flag records it and raises OnFlagSet once; setting it again changes nothing --- |
        -----------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Setting a flag records it and raises OnFlagSet once; setting it again changes nothing. Other flags stay unset.")]
        public void Set_RecordsTheFactOnce()
        {
            m_flags.Set(m_campEntered);
            m_flags.Set(m_campEntered);

            Assert.That(m_flags.IsSet(m_campEntered), Is.True);
            Assert.That(m_flags.IsSet(m_campCleared), Is.False);
            Assert.That(m_setEvents, Is.EqualTo(1));
        }


        /*--------------------
        | --- Conditions --- |
        --------------------*/
        /*--------------------------------------------------------------------------------------------------
        | --- WorldFlag_IsFalseUntilSet_ThenTrue: kWorldFlag is false until the flag is set, then true --- |
        --------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("kWorldFlag is false until the flag is set, then true.")]
        public void WorldFlag_IsFalseUntilSet_ThenTrue()
        {
            Assert.That(m_flags.Evaluate(ConditionType.kWorldFlag, new[] { m_campCleared.ID }), Is.False);

            m_flags.Set(m_campCleared);

            Assert.That(m_flags.Evaluate(ConditionType.kWorldFlag, new[] { m_campCleared.ID }), Is.True);
        }

        /*------------------------------------------------------------------------------------------------------------------------------------------
        | --- WorldFlag_UnknownOrMissingID_IsUnanswered: An ID no WorldFlag has, or no ID at all, is unanswered, so the condition fails closed --- |
        ------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An ID no WorldFlag has, or no ID at all, is unanswered (null), so the condition fails closed.")]
        public void WorldFlag_UnknownOrMissingID_IsUnanswered()
        {
            Assert.That(m_flags.Evaluate(ConditionType.kWorldFlag, new[] { "no_such_flag" }), Is.Null);
            Assert.That(m_flags.Evaluate(ConditionType.kWorldFlag, new[] { "" }), Is.Null);
            Assert.That(m_flags.Evaluate(ConditionType.kWorldFlag, new string[0]), Is.Null);
            Assert.That(m_flags.Evaluate(ConditionType.kWorldFlag, null), Is.Null);
        }

        /*--------------------------------------------------------------------------------------------------------------------
        | --- OtherConditions_AreNotAnswered: Conditions that aren't about world flags are left to other checkers (null) --- |
        --------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Conditions that aren't about world flags are left to other checkers (null).")]
        public void OtherConditions_AreNotAnswered()
        {
            Assert.That(m_flags.Evaluate(ConditionType.kHasQuest, new[] { "camp_cleared" }), Is.Null);
        }


        /*----------------
        | --- Saving --- |
        ----------------*/
        /*-----------------------------------------------------------------------------------------------------------------------------
        | --- CaptureThenRestore_IntoFreshFlags_RestoresTheSetFlags: The set flags survive a save and load; unset ones stay unset --- |
        -----------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("The set flags survive a save and load, and loading raises no OnFlagSet; unset ones stay unset.")]
        public void CaptureThenRestore_IntoFreshFlags_RestoresTheSetFlags()
        {
            m_flags.Set(m_campEntered);
            JToken saved = JToken.Parse(m_flags.CaptureState().ToString());
            WorldFlags fresh = m_kit.NewObject("FreshPlayer").AddComponent<WorldFlags>();
            int freshSetEvents = 0;
            fresh.OnFlagSet += _ => ++freshSetEvents;

            fresh.RestoreState(saved);

            Assert.That(fresh.IsSet(m_campEntered), Is.True);
            Assert.That(fresh.IsSet(m_campCleared), Is.False);
            Assert.That(freshSetEvents, Is.EqualTo(0), "loading isn't the world changing");
        }

        /*-------------------------------------------------------------------------------------------------------------
        | --- RestoreState_NonArray_IsIgnored: Save data that isn't a list is ignored: the current flags are kept --- |
        -------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Save data that isn't a list is ignored: the current flags are kept.")]
        public void RestoreState_NonArray_IsIgnored()
        {
            m_flags.Set(m_campEntered);

            m_flags.RestoreState(JValue.CreateNull());
            m_flags.RestoreState(new JObject { ["camp_cleared"] = true });

            Assert.That(m_flags.IsSet(m_campEntered), Is.True);
            Assert.That(m_flags.IsSet(m_campCleared), Is.False);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- RestoreState_UnknownOrUnreadableEntries_AreSkipped_TheRestLoad: An entry that isn't a known flag ID is skipped with a warning naming it; the rest load --- |
        ----------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("An entry that isn't a known flag ID is skipped with an editor warning naming it; the other flags load.")]
        public void RestoreState_UnknownOrUnreadableEntries_AreSkipped_TheRestLoad()
        {
            LogAssert.Expect(LogType.Warning, new Regex("flag_from_an_old_build"));

            m_flags.RestoreState(new JArray("flag_from_an_old_build", 42, m_campCleared.ID));

            Assert.That(m_flags.IsSet(m_campCleared), Is.True);
            Assert.That(((JArray)m_flags.CaptureState()).Count, Is.EqualTo(1));
        }
    }
}