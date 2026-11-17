/*---------------------------
| File: DialogueNPCTests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
//---------------------------------
using PolyQuest.Dialogues;
using PolyQuest.UI;
using PolyQuest.UI.Core;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace PolyQuest.Tests
{
    // How the Player engages an NPC: hovering, highlighting, its label, and walking up to talk.
    public class DialogueNPCTests
    {
        private PlayerRig m_rig;
        private DialogueTestKit m_kit;

        /*--------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for dialogue NPC testing --- |
        --------------------------------------------------------------------------------*/
        [SetUp]
        public void SetUp()
        {
            m_rig = new PlayerRig();
            m_kit = new DialogueTestKit();
        }

        /*-------------------------------------------------------------------
        | --- TearDown: Release test fixtures and game object resources --- |
        -------------------------------------------------------------------*/
        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
            m_rig.Dispose();
        }


        /*-----------------
        | --- Helpers --- |
        -----------------*/
        /*-----------------------------------------------------
        | --- Handler: The Player's PlayerDialogueHandler --- |
        -----------------------------------------------------*/
        private PlayerDialogueHandler Handler => m_rig.Player.GetComponent<PlayerDialogueHandler>();

        /*-----------------------------------------------------------------------------------
        | --- Npc: Creates an NPC with a one-line dialogue at an offset from the Player --- |
        -----------------------------------------------------------------------------------*/
        private AIDialogueHandler Npc(string speakerName, Vector3 offset)
            => m_kit.Npc(speakerName, m_kit.Dialogue(speakerName + " Dialogue", m_kit.NpcLine("Hello there.")),
                         m_rig.Player.transform.position + offset);

        /*-------------------------------------------------------------
        | --- LabelRoot: The root object of the NPC's world label --- |
        -------------------------------------------------------------*/
        private static GameObject LabelRoot(AIDialogueHandler npc)
            => PlayModeTestKit.Get<GameObject>(npc.GetComponent<WorldLabel>(), "m_labelRoot");


        /*---------------
        | --- Label --- |
        ---------------*/
        /*---------------------------------------------------------------------------------
        | --- Label_ShowsTheSpeakersName: An NPC's world label shows its display name --- |
        ---------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("An NPC's world label shows its display name.")]
        public IEnumerator Label_ShowsTheSpeakersName()
        {
            yield return null;
            AIDialogueHandler npc = Npc("Town Crier", new Vector3(2f, 0f, 0f));

            Assert.That(LabelRoot(npc).GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Town Crier"));
        }

        /*---------------------------------------------------------------------------------
        | --- Hover_ShowsTheDialogueCursor: Hovering an NPC shows the dialogue cursor --- |
        ---------------------------------------------------------------------------------*/
        [UnityTest]
        [Category("ClickModel")]
        [Description("Hovering an NPC shows the dialogue cursor.")]
        public IEnumerator Hover_ShowsTheDialogueCursor()
        {
            yield return null;
            AIDialogueHandler npc = Npc("Town Crier", new Vector3(2f, 0f, 0f));

            Assert.That(npc.GetCursorType(), Is.EqualTo(CursorSettings.CursorType.kDialogue));
        }

        /*------------------------------------------------------------------------------------------------------------------------------
        | --- Highlight_TurnsTheOutlineOnAndOff: Highlighting an NPC turns its outline on, and clearing the highlight turns it off --- |
        ------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Category("ClickModel")]
        [Description("Highlighting an NPC turns its outline on, and clearing the highlight turns it off.")]
        public IEnumerator Highlight_TurnsTheOutlineOnAndOff()
        {
            yield return null;
            AIDialogueHandler npc = Npc("Town Crier", new Vector3(2f, 0f, 0f));
            Outline outline = npc.GetComponent<Outline>();

            npc.ToggleHighlight(true);
            Assert.That(outline.enabled, Is.True);

            npc.ToggleHighlight(false);
            Assert.That(outline.enabled, Is.False);
        }

        /*--------------------------------------------------------------------------------------------
        | --- Label_ShowsAndHides: An NPC's label is hidden until shown, and can be hidden again --- |
        --------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Category("ClickModel")]
        [Description("An NPC's label is hidden until shown, and can be hidden again.")]
        public IEnumerator Label_ShowsAndHides()
        {
            yield return null;
            AIDialogueHandler npc = Npc("Town Crier", new Vector3(2f, 0f, 0f));
            Assert.That(LabelRoot(npc).activeSelf, Is.False);

            npc.ToggleLabel(true);
            Assert.That(LabelRoot(npc).activeSelf, Is.True);

            npc.ToggleLabel(false);
            Assert.That(LabelRoot(npc).activeSelf, Is.False);
        }


        /*----------------------------
        | --- Walking Up to Talk --- |
        ----------------------------*/
        /*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- TalkingToADistantNpc_WalksOver_ThenStartsTheConversation: Talking to a distant NPC walks the Player over, and the conversation starts only once the Player is within talking distance --- |
        -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Category("ClickModel")]
        [Description("Talking to a distant NPC walks the Player over, and the conversation starts only once the Player is within talking distance.")]
        public IEnumerator TalkingToADistantNpc_WalksOver_ThenStartsTheConversation()
        {
            yield return null;
            AIDialogueHandler npc = Npc("Far Away", new Vector3(10f, 0f, 0f));
            float distanceAtStart = -1f;
            Handler.OnDialogueStarted += () => distanceAtStart = Vector3.Distance(m_rig.Player.transform.position, npc.transform.position);

            Handler.BeginDialogueAction(npc, PlayModeTestKit.Get<Dialogue>(npc, "m_dialogue"));
            Assert.That(distanceAtStart, Is.LessThan(0f), "the conversation started before the Player walked over");

            for (float waited = 0f; distanceAtStart < 0f; waited += Time.deltaTime)
            {
                Assert.That(waited, Is.LessThan(15f), "the Player never reached the NPC");
                yield return null;
            }

            Assert.That(distanceAtStart, Is.LessThanOrEqualTo(3f));
            Assert.That(Handler.GetText(), Is.EqualTo("Hello there."));
        }
    }
}