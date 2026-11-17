/*---------------------------
| File: DialogueUITests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
//---------------------------------
using PolyQuest.Dialogues;
using PolyQuest.Tools;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PolyQuest.Tests
{
    // The dialogue panel's logic: choice buttons on the Player's turn, Next or End otherwise, and the button wiring.
    // Panel visibility belongs to the dialogue camera and is not tested here.
    public class DialogueUITests
    {
        private const string kHeirloom = "A Family Heirloom";

        private PlayerRig m_rig;
        private DialogueTestKit m_kit;

        /*-------------------------------------------------------------------------------
        | --- SetUp: Initialize and clean up tests fixtures for dialogue UI testing --- |
        -------------------------------------------------------------------------------*/
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

        /*--------------------------------------------------------------
        | --- Open: Builds the panel, then starts the conversation --- |
        --------------------------------------------------------------*/
        private DialogueTestKit.UIParts Open(string speakerName, Dialogue dialogue)
        {
            DialogueTestKit.UIParts ui = m_kit.DialogueUI(Handler);
            AIDialogueHandler npc = m_kit.Npc(speakerName, dialogue, m_rig.Player.transform.position + new Vector3(2f, 0f, 0f));
            DialogueTestKit.Talk(Handler, npc);
            return ui;
        }

        /*--------------------------------------------
        | --- Label: The text on a choice button --- |
        --------------------------------------------*/
        private static string Label(GameObject choice) => choice.GetComponentInChildren<TextMeshProUGUI>(true).text;


        /*-----------------------
        | --- Player's Turn --- |
        -----------------------*/
        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- PlayersTurn_ShowsAChoiceButtonPerAvailableReply_AndHidesNextAndEnd: On the Player's turn the panel shows one choice button per available reply, labelled with its text, and hides Next and End --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("On the Player's turn the panel shows one choice button per available reply, labelled with its text, and hides Next and End.")]
        public IEnumerator PlayersTurn_ShowsAChoiceButtonPerAvailableReply_AndHidesNextAndEnd()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("What brings you here?");
            DialogueNode askWork = m_kit.PlayerLine("Any work?");
            DialogueNode askHeirloom = m_kit.PlayerLine("About your heirloom...", DialogueTestKit.When(ConditionType.kHasQuest, kHeirloom));
            DialogueNode leave = m_kit.PlayerLine("Nothing, goodbye.");
            DialogueTestKit.Link(opener, askWork, askHeirloom, leave);

            DialogueTestKit.UIParts ui = Open("Chooser", m_kit.Dialogue("Choices Dialogue", opener, askWork, askHeirloom, leave));
            yield return null;

            Assert.That(ui.Choices().Select(Label), Is.EquivalentTo(new[] { askWork.Text, leave.Text }));
            Assert.That(ui.Choices().All(c => c.GetComponent<Button>() != null), Is.True);
            Assert.That(ui.Next.gameObject.activeSelf, Is.False);
            Assert.That(ui.End.gameObject.activeSelf, Is.False);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- ClickingAChoice_MovesTheConversationOn_AndClearsTheChoices: Clicking a choice button selects that reply: the conversation moves on to the NPC's answer and the old choice buttons are removed --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("Clicking a choice button selects that reply: the conversation moves on to the NPC's answer and the old choice buttons are removed.")]
        public IEnumerator ClickingAChoice_MovesTheConversationOn_AndClearsTheChoices()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("What brings you here?");
            DialogueNode askWork = m_kit.PlayerLine("Any work?");
            DialogueNode leave = m_kit.PlayerLine("Nothing, goodbye.");
            DialogueNode answer = m_kit.NpcLine("Talk to the captain.");
            DialogueTestKit.Link(opener, askWork, leave);
            DialogueTestKit.Link(askWork, answer);
            DialogueTestKit.UIParts ui = Open("Chooser", m_kit.Dialogue("Click Dialogue", opener, askWork, leave, answer));
            yield return null;
            GameObject askWorkButton = ui.Choices().Single(c => Label(c) == askWork.Text);

            askWorkButton.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(Handler.GetText(), Is.EqualTo(answer.Text));
            Assert.That(ui.Text.text, Is.EqualTo(answer.Text));
            Assert.That(ui.Choices(), Is.Empty);
        }


        /*--------------------
        | --- NPC's Turn --- |
        --------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------------------
        | --- NpcTurn_WithMoreToSay_ShowsNext_NotEnd: On the NPC's turn with more to say, the panel shows Next, hides End, and shows no choices --- |
        -------------------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("On the NPC's turn with more to say, the panel shows Next, hides End, and shows no choices.")]
        public IEnumerator NpcTurn_WithMoreToSay_ShowsNext_NotEnd()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("Listen carefully.");
            DialogueNode more = m_kit.NpcLine("There's more.");
            DialogueTestKit.Link(opener, more);

            DialogueTestKit.UIParts ui = Open("Talker", m_kit.Dialogue("Next Dialogue", opener, more));
            yield return null;

            Assert.That(ui.Next.gameObject.activeSelf, Is.True);
            Assert.That(ui.End.gameObject.activeSelf, Is.False);
            Assert.That(ui.Choices(), Is.Empty);
        }

        /*-------------------------------------------------------------------------------------------------------------------------------
        | --- NpcTurn_OnTheLastLine_ShowsEnd_NotNext: On the NPC's last line, the panel shows End, hides Next, and shows no choices --- |
        -------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("On the NPC's last line, the panel shows End, hides Next, and shows no choices.")]
        public IEnumerator NpcTurn_OnTheLastLine_ShowsEnd_NotNext()
        {
            yield return null;
            DialogueTestKit.UIParts ui = Open("Farewell", m_kit.Dialogue("End Dialogue", m_kit.NpcLine("Safe travels.")));
            yield return null;

            Assert.That(ui.End.gameObject.activeSelf, Is.True);
            Assert.That(ui.Next.gameObject.activeSelf, Is.False);
            Assert.That(ui.Choices(), Is.Empty);
        }

        /*----------------------------------------------------------------------------------------------------------------------------------
        | --- ShowsTheSpeakersNameAndTheCurrentLine: The panel shows the NPC's display name as the speaker and the current line's text --- |
        ----------------------------------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("The panel shows the NPC's display name as the speaker and the current line's text.")]
        public IEnumerator ShowsTheSpeakersNameAndTheCurrentLine()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("Welcome to Stonewick.");

            DialogueTestKit.UIParts ui = Open("Town Crier", m_kit.Dialogue("Name Dialogue", opener));

            Assert.That(ui.Speaker.text, Is.EqualTo("Town Crier"));
            Assert.That(ui.Text.text, Is.EqualTo(opener.Text));
        }


        /*-----------------
        | --- Buttons --- |
        -----------------*/
        /*-----------------------------------------------------------------------------------------------------------
        | --- NextButton_AdvancesTheConversation: The Next button moves the conversation to the NPC's next line --- |
        -----------------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("The Next button moves the conversation to the NPC's next line.")]
        public IEnumerator NextButton_AdvancesTheConversation()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("Listen carefully.");
            DialogueNode more = m_kit.NpcLine("There's more.");
            DialogueTestKit.Link(opener, more);
            DialogueTestKit.UIParts ui = Open("Talker", m_kit.Dialogue("Next Dialogue", opener, more));

            ui.Next.onClick.Invoke();

            Assert.That(Handler.GetText(), Is.EqualTo(more.Text));
            Assert.That(ui.Text.text, Is.EqualTo(more.Text));
        }

        /*-----------------------------------------------------------------------------
        | --- EndButton_EndsTheConversation: The End button ends the conversation --- |
        -----------------------------------------------------------------------------*/
        [UnityTest]
        [Description("The End button ends the conversation.")]
        public IEnumerator EndButton_EndsTheConversation()
        {
            yield return null;
            DialogueTestKit.UIParts ui = Open("Farewell", m_kit.Dialogue("End Dialogue", m_kit.NpcLine("Safe travels.")));

            ui.End.onClick.Invoke();

            Assert.That(Handler.IsActive(), Is.False);
        }

        /*-----------------------------------------------------------------------------------------------------
        | --- QuitButton_EndsTheConversation: The Quit button ends the conversation from the middle of it --- |
        -----------------------------------------------------------------------------------------------------*/
        [UnityTest]
        [Description("The Quit button ends the conversation from the middle of it.")]
        public IEnumerator QuitButton_EndsTheConversation()
        {
            yield return null;
            DialogueNode opener = m_kit.NpcLine("Listen carefully.");
            DialogueNode more = m_kit.NpcLine("There's more.");
            DialogueTestKit.Link(opener, more);
            DialogueTestKit.UIParts ui = Open("Talker", m_kit.Dialogue("Quit Dialogue", opener, more));

            ui.Quit.onClick.Invoke();

            Assert.That(Handler.IsActive(), Is.False);
        }
    }
}