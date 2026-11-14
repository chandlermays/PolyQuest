/*---------------------------
| File: DialogueHistoryTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Dialogues;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    // Which NPCs the Player has spoken to: the kHasSpokenTo condition and the save data.
    public class DialogueHistoryTests
    {
        private GameObject m_player;
        private DialogueHistory m_history;

        [SetUp]
        public void SetUp()
        {
            m_player = new GameObject("Player");
            m_history = m_player.AddComponent<DialogueHistory>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_player);
        }

        /* --- Helpers --- */

        private DialogueHistory FreshHistory()
        {
            GameObject other = new GameObject("FreshPlayer");
            other.transform.SetParent(m_player.transform);           // destroyed with the fixture
            return other.AddComponent<DialogueHistory>();
        }

        /* ============================== Recording ============================== */

        [Test]
        [Description("Recording a conversation marks that NPC, and only that NPC, as spoken to.")]
        public void RecordConversation_MarksOnlyThatNpc()
        {
            m_history.RecordConversation("cole");

            Assert.That(m_history.HasSpokenTo("cole"), Is.True);
            Assert.That(m_history.HasSpokenTo("elira"), Is.False);
        }

        /* ============================== kHasSpokenTo ============================== */

        [Test]
        [Description("kHasSpokenTo answers true for an NPC the Player has spoken to and false for one they haven't.")]
        public void HasSpokenTo_AnswersForTheNamedNpc()
        {
            m_history.RecordConversation("cole");

            Assert.That(m_history.Evaluate(ConditionType.kHasSpokenTo, new[] { "cole" }), Is.True);
            Assert.That(m_history.Evaluate(ConditionType.kHasSpokenTo, new[] { "elira" }), Is.False);
        }

        [Test]
        [Description("kHasSpokenTo without an NPC parameter is unanswered.")]
        public void HasSpokenTo_WithoutAParameter_IsUnanswered()
        {
            m_history.RecordConversation("cole");

            Assert.That(m_history.Evaluate(ConditionType.kHasSpokenTo, null), Is.Null);
            Assert.That(m_history.Evaluate(ConditionType.kHasSpokenTo, new string[0]), Is.Null);
        }

        [Test]
        [Description("DialogueHistory does not answer conditions other than kHasSpokenTo.")]
        public void NonDialogueCondition_IsNotAnswered()
        {
            m_history.RecordConversation("cole");

            Assert.That(m_history.Evaluate(ConditionType.kHasQuest, new[] { "cole" }), Is.Null);
        }

        /* ============================== Saving ============================== */

        [Test]
        [Description("Capturing the history and restoring it into a fresh one restores every NPC spoken to.")]
        public void CaptureThenRestore_IntoAFreshHistory_RestoresEveryNpc()
        {
            m_history.RecordConversation("cole");
            m_history.RecordConversation("elira");
            DialogueHistory fresh = FreshHistory();

            fresh.RestoreState(m_history.CaptureState());

            Assert.That(fresh.HasSpokenTo("cole"), Is.True);
            Assert.That(fresh.HasSpokenTo("elira"), Is.True);
            Assert.That(fresh.HasSpokenTo("flint"), Is.False);
        }

        [Test]
        [Description("Restoring a save replaces the current history: NPCs spoken to since that save are forgotten.")]
        public void RestoreState_ReplacesTheCurrentHistory()
        {
            m_history.RecordConversation("cole");
            JToken saved = m_history.CaptureState();
            m_history.RecordConversation("elira");

            m_history.RestoreState(saved);

            Assert.That(m_history.HasSpokenTo("cole"), Is.True);
            Assert.That(m_history.HasSpokenTo("elira"), Is.False);
        }

        [Test]
        [Description("Save data that isn't a list of NPCs is ignored, as if the save never held a history.")]
        public void RestoreState_NotAList_IsIgnored()
        {
            m_history.RecordConversation("cole");

            m_history.RestoreState(JValue.CreateNull());
            m_history.RestoreState(new JObject { ["cole"] = true });
            m_history.RestoreState(new JValue("elira"));

            Assert.That(m_history.HasSpokenTo("cole"), Is.True);
            Assert.That(m_history.HasSpokenTo("elira"), Is.False);
        }

        [Test]
        [Description("An unreadable entry in the saved history is skipped and the rest load.")]
        public void RestoreState_UnreadableEntry_IsSkipped_TheRestLoad()
        {
            m_history.RestoreState(new JArray("cole", new JObject { ["npc"] = "flint" }, "elira"));

            Assert.That(m_history.HasSpokenTo("cole"), Is.True);
            Assert.That(m_history.HasSpokenTo("elira"), Is.True);
        }
    }
}