/*---------------------------
| File: DialogueConversationTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Dialogues;
using PolyQuest.Quests;
using PolyQuest.Saving;
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    // Conversations on the real Player: its real QuestManager, DialogueHistory and the rest of its condition checkers.
    // Conversations start directly, without walking to the NPC.
    public class DialogueConversationTests
    {
        private const string kHeirloom = "A Family Heirloom";
        private const string kBanditCamp = "Clear the Bandit Camp";

        private PlayerRig m_rig;
        private DialogueTestKit m_kit;

        [SetUp]
        public void SetUp()
        {
            m_rig = new PlayerRig();
            m_kit = new DialogueTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
            m_rig.Dispose();
        }

        /* --- Helpers --- */

        private PlayerDialogueHandler Handler => m_rig.Player.GetComponent<PlayerDialogueHandler>();
        private DialogueHistory History => m_rig.Player.GetComponent<DialogueHistory>();
        private QuestManager Quests => m_rig.Player.GetComponent<QuestManager>();

        private static Quest LoadQuest(string name)
        {
            Quest quest = Quest.GetByName(name);
            Assert.That(quest, Is.Not.Null, $"fixture: the '{name}' quest asset is missing from Resources");
            return quest;
        }

        private AIDialogueHandler Npc(string speakerName, Dialogue dialogue)
            => m_kit.Npc(speakerName, dialogue, m_rig.Player.transform.position + new Vector3(2f, 0f, 0f));

        private void Talk(AIDialogueHandler npc) => DialogueTestKit.Talk(Handler, npc);

        private static Conjunction HasQuest(string questName, bool negate = false)
            => DialogueTestKit.When(ConditionType.kHasQuest, questName, negate);

        private string[] ChoiceTexts() => Handler.GetChoices().Select(c => c.Text).ToArray();

        /* ============================== Opening lines ============================== */

        [UnityTest]
        [Description("Opening lines are checked in authored order: the first one whose condition passes opens the conversation, even when a later one passes too.")]
        public IEnumerator OpeningLines_TheFirstWhoseConditionPasses_Wins()
        {
            yield return null;
            Quests.AddQuest(LoadQuest(kBanditCamp));
            DialogueNode heirloomOpener = m_kit.NpcLine("About the heirloom...", HasQuest(kHeirloom));
            DialogueNode banditOpener = m_kit.NpcLine("About the bandits...", HasQuest(kBanditCamp));
            DialogueNode fallback = m_kit.NpcLine("Good day.");
            AIDialogueHandler npc = Npc("Openers", m_kit.Dialogue("Openers Dialogue", heirloomOpener, banditOpener, fallback));

            Talk(npc);

            Assert.That(Handler.GetText(), Is.EqualTo(banditOpener.Text));
        }

        [UnityTest]
        [Description("An unconditioned last opening line opens the conversation when none of the conditioned ones pass.")]
        public IEnumerator OpeningLines_AnUnconditionedLastOne_CatchesEverythingElse()
        {
            yield return null;
            DialogueNode heirloomOpener = m_kit.NpcLine("About the heirloom...", HasQuest(kHeirloom));
            DialogueNode banditOpener = m_kit.NpcLine("About the bandits...", HasQuest(kBanditCamp));
            DialogueNode fallback = m_kit.NpcLine("Good day.");
            AIDialogueHandler npc = Npc("Openers", m_kit.Dialogue("Openers Dialogue", heirloomOpener, banditOpener, fallback));

            Talk(npc);

            Assert.That(Handler.GetText(), Is.EqualTo(fallback.Text));
        }

        [UnityTest]
        [Description("When no opening line's condition passes, no conversation starts: nothing fires, nothing is raised, and an editor warning names the dialogue asset.")]
        public IEnumerator NoOpeningLinePasses_NoConversationStarts_AndAWarningNamesTheDialogue()
        {
            yield return null;
            DialogueNode heirloomOpener = m_kit.NpcLine("About the heirloom...", HasQuest(kHeirloom));
            DialogueNode reply = m_kit.PlayerLine("I'll find it.");
            DialogueTestKit.Link(heirloomOpener, reply);
            m_kit.RecordEntersAndExits(heirloomOpener, reply);
            Dialogue dialogue = m_kit.Dialogue("Gated Dialogue", heirloomOpener, reply);
            AIDialogueHandler npc = Npc("Gated", dialogue);
            int started = 0;
            Handler.OnDialogueStarted += () => ++started;
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(dialogue.name)));

            Talk(npc);

            Assert.That(started, Is.EqualTo(0));
            Assert.That(m_kit.Fired, Is.Empty);
            Assert.That(Handler.IsActive(), Is.False);
        }

        [UnityTest]
        [Description("A dialogue with no lines starts no conversation and throws nothing; an editor warning names the dialogue asset.")]
        public IEnumerator EmptyDialogue_NoConversationStarts_AndNothingThrows()
        {
            yield return null;
            Dialogue dialogue = m_kit.Dialogue("Empty Dialogue");
            Assert.That(dialogue.Nodes.Any(), Is.False, "fixture: the dialogue must have no lines");
            AIDialogueHandler npc = Npc("Silent", dialogue);
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(dialogue.name)));

            Assert.DoesNotThrow(() => Talk(npc));

            Assert.That(Handler.IsActive(), Is.False);
        }

        /* ============================== Player choices ============================== */

        [UnityTest]
        [Description("On the Player's turn, the choices are exactly the replies whose conditions pass.")]
        public IEnumerator PlayersTurn_ChoicesAreTheRepliesWhoseConditionsPass()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("What brings you here?");
            DialogueNode askWork = m_kit.PlayerLine("Any work?");
            DialogueNode askHeirloom = m_kit.PlayerLine("About your heirloom...", HasQuest(kHeirloom));
            DialogueNode leave = m_kit.PlayerLine("Nothing, goodbye.");
            DialogueTestKit.Link(opener, askWork, askHeirloom, leave);
            Talk(Npc("Chooser", m_kit.Dialogue("Choices Dialogue", opener, askWork, askHeirloom, leave)));

            Assert.That(Handler.IsPlayerChoosing(), Is.True);
            Assert.That(ChoiceTexts(), Is.EquivalentTo(new[] { askWork.Text, leave.Text }));
        }

        [UnityTest]
        [Description("Selecting a choice leaves the current line, enters the choice, then moves on to the NPC's answer, firing each line's events in that order.")]
        public IEnumerator SelectChoice_LeavesTheLine_EntersTheChoice_ThenTheNpcAnswers()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("What brings you here?");
            DialogueNode askWork = m_kit.PlayerLine("Any work?");
            DialogueNode answer = m_kit.NpcLine("Talk to the captain.");
            DialogueTestKit.Link(opener, askWork);
            DialogueTestKit.Link(askWork, answer);
            m_kit.RecordEntersAndExits(opener, askWork, answer);
            Talk(Npc("Chooser", m_kit.Dialogue("Select Dialogue", opener, askWork, answer)));
            m_kit.Fired.Clear();

            Handler.SelectChoice(askWork);

            Assert.That(Handler.GetText(), Is.EqualTo(answer.Text));
            Assert.That(m_kit.Fired, Is.EqualTo(new[] { "exit:" + opener.Text, "enter:" + askWork.Text, "exit:" + askWork.Text, "enter:" + answer.Text }));
        }

        [UnityTest]
        [Description("Lines are re-checked against the latest state: a quest assigned on entering a line changes the choices that follow it.")]
        public IEnumerator EnteringALineThatAssignsAQuest_ChangesTheChoicesThatFollow()
        {
            yield return null;
            Quest heirloom = LoadQuest(kHeirloom);
            AssignQuestEvent assign = m_kit.Event<AssignQuestEvent>();
            PlayModeTestKit.Set(assign, "m_quest", heirloom);
            DialogueNode opener = m_kit.NpcLine("Find my family's sword.");
            DialogueTestKit.OnEnter(opener, assign);
            DialogueNode accepted = m_kit.PlayerLine("I'll look for it.", HasQuest(kHeirloom));
            DialogueNode notYet = m_kit.PlayerLine("Maybe later.", HasQuest(kHeirloom, negate: true));
            DialogueTestKit.Link(opener, accepted, notYet);

            Talk(Npc("Quest Giver", m_kit.Dialogue("Assign Dialogue", opener, accepted, notYet)));

            Assert.That(Quests.HasQuest(heirloom), Is.True);
            Assert.That(ChoiceTexts(), Is.EquivalentTo(new[] { accepted.Text }));
        }

        /* ============================== NPC lines ============================== */

        [UnityTest]
        [Description("When the NPC has several lines to follow with, Next always moves to one whose condition passes, never to one whose condition fails.")]
        public IEnumerator NpcTurn_Next_MovesToALineWhoseConditionPasses()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("Hmm.");
            DialogueNode first = m_kit.NpcLine("Lovely weather.");
            DialogueNode second = m_kit.NpcLine("Quiet day.");
            DialogueNode gated = m_kit.NpcLine("Found the sword yet?", HasQuest(kHeirloom));
            DialogueTestKit.Link(opener, first, second, gated);
            AIDialogueHandler npc = Npc("Chatter", m_kit.Dialogue("Random Dialogue", opener, first, second, gated));

            for (int seed = 0; seed < 16; ++seed)
            {
                Random.InitState(seed);
                Talk(npc);

                Handler.NextDialogueNode();

                Assert.That(new[] { first.Text, second.Text }, Has.Member(Handler.GetText()), $"seed {seed}");
                Handler.EndDialogue();
            }
        }

        [UnityTest]
        [Description("Next on the last line ends the conversation once, firing that line's exit events.")]
        public IEnumerator LastLine_Next_EndsTheConversation_FiringItsExitEvents()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("Safe travels.");
            m_kit.RecordEntersAndExits(opener);
            Talk(Npc("Farewell", m_kit.Dialogue("Farewell Dialogue", opener)));
            int ended = 0;
            Handler.OnDialogueEnded += () => ++ended;
            m_kit.Fired.Clear();

            Handler.NextDialogueNode();

            Assert.That(Handler.IsActive(), Is.False);
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(m_kit.Fired, Is.EqualTo(new[] { "exit:" + opener.Text }));
        }

        /* ============================== Leaving and history ============================== */

        [UnityTest]
        [Description("Quitting mid-conversation leaves the current line: its exit events fire and the NPC is recorded as spoken to.")]
        public IEnumerator Quit_MidConversation_FiresTheLinesExitEvents_AndRecordsTheNpc()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("Listen carefully.");
            DialogueNode more = m_kit.NpcLine("There's more.");
            DialogueTestKit.Link(opener, more);
            m_kit.RecordEntersAndExits(opener, more);
            AIDialogueHandler npc = Npc("Talker", m_kit.Dialogue("Quit Dialogue", opener, more));
            Talk(npc);
            m_kit.Fired.Clear();

            Handler.EndDialogue();

            Assert.That(Handler.IsActive(), Is.False);
            Assert.That(m_kit.Fired, Is.EqualTo(new[] { "exit:" + opener.Text }));
            Assert.That(History.HasSpokenTo(DialogueTestKit.HistoryKey(npc)), Is.True);
        }

        [UnityTest]
        [Description("Spoken-to is recorded only when the conversation ends: a line gated on it is unavailable during the first conversation and available in the next.")]
        public IEnumerator SpokenTo_IsRecordedWhenTheConversationEnds()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("Hello, stranger.");
            DialogueNode again = m_kit.PlayerLine("We've met before.");
            DialogueNode hello = m_kit.PlayerLine("Hello.");
            DialogueTestKit.Link(opener, again, hello);
            AIDialogueHandler npc = Npc("Elder", m_kit.Dialogue("History Dialogue", opener, again, hello));
            string key = DialogueTestKit.HistoryKey(npc);
            PlayModeTestKit.Set(again, "m_condition", DialogueTestKit.When(ConditionType.kHasSpokenTo, key));

            Talk(npc);
            Assert.That(History.HasSpokenTo(key), Is.False, "the first conversation has not ended yet");
            Assert.That(ChoiceTexts(), Is.EquivalentTo(new[] { hello.Text }));
            Handler.EndDialogue();

            Talk(npc);
            Assert.That(ChoiceTexts(), Is.EquivalentTo(new[] { again.Text, hello.Text }));
        }

        [UnityTest]
        [Description("Ending a conversation saves the game: loading that save into a fresh Player shows the NPC as spoken to.")]
        public IEnumerator EndingAConversation_SavesTheHistory()
        {
            yield return null;
            AIDialogueHandler npc = Npc("Saver", m_kit.Dialogue("Save Dialogue", m_kit.NpcLine("Remember me.")));
            string key = DialogueTestKit.HistoryKey(npc);
            Talk(npc);
            Handler.EndDialogue();

            m_rig.SpawnPlayer();
            yield return null;
            Assert.That(History.HasSpokenTo(key), Is.False, "fixture: the fresh Player must start with no history");
            SaveManager.Instance.Load();

            Assert.That(History.HasSpokenTo(key), Is.True);
        }

        /* ============================== Dialogue events ============================== */

        [UnityTest]
        [Description("A HealPlayerEvent on a line fully heals the Player when the line is entered.")]
        public IEnumerator HealPlayerEvent_FullyHealsThePlayer()
        {
            yield return null;
            float fullHealth = m_rig.Health.CurrentHealth;
            m_rig.Damage(fullHealth / 2f);
            Assert.That(m_rig.Health.CurrentHealth, Is.LessThan(fullHealth), "fixture: the Player must be hurt");
            DialogueNode opener = m_kit.NpcLine("Let me tend to those wounds.");
            DialogueTestKit.OnEnter(opener, m_kit.Event<HealPlayerEvent>());

            Talk(Npc("Healer", m_kit.Dialogue("Heal Dialogue", opener)));

            Assert.That(m_rig.Health.CurrentHealth, Is.EqualTo(fullHealth));
        }
    }
}