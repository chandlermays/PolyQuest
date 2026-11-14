/*---------------------------
| File: DialogueCameraTests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Dialogues;
using PolyQuest.Player;
using PolyQuest.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace PolyQuest.Tests
{
    // The dialogue camera's transitions, only as far as the Player getting stuck: input must be unlocked when a
    // conversation is over. Every transition in the game has a fade, so a TransitionFade is always present.
    public class DialogueCameraTests
    {
        private const float kFadeDuration = 0.25f;
        private const float kWaitDuration = 0.05f;
        private const float kSettleTime = 1.5f;             // comfortably longer than fade out + wait + fade in

        private PlayerRig m_rig;
        private DialogueTestKit m_kit;

        private InputGuard m_inputGuard;
        private CanvasGroup m_fadeCanvas;
        private GameObject m_hud;
        private DialogueTestKit.UIParts m_ui;
        private AIDialogueHandler m_npc;

        [SetUp]
        public void SetUp()
        {
            m_rig = new PlayerRig();
            m_kit = new DialogueTestKit();

            m_inputGuard = m_kit.Track(new GameObject("InputGuard")).AddComponent<InputGuard>();

            GameObject fadeObject = m_kit.Track(new GameObject("TransitionFade"));
            fadeObject.SetActive(false);
            m_fadeCanvas = fadeObject.AddComponent<CanvasGroup>();
            m_fadeCanvas.alpha = 0f;
            TransitionFade fade = fadeObject.AddComponent<TransitionFade>();
            PlayModeTestKit.Set(fade, "m_fadeDuration", kFadeDuration);
            PlayModeTestKit.Set(fade, "m_waitDuration", kWaitDuration);
            fadeObject.SetActive(true);

            m_hud = m_kit.Track(new GameObject("PlayerHUD"));
            PlayerDialogueHandler handler = m_rig.Player.GetComponent<PlayerDialogueHandler>();
            m_ui = m_kit.DialogueUI(handler);

            GameObject controllerObject = m_kit.Track(new GameObject("DialogueCameraController"));
            controllerObject.SetActive(false);
            DialogueCameraController controller = controllerObject.AddComponent<DialogueCameraController>();
            PlayModeTestKit.Set(controller, "m_mainCamera", Camera.main);
            PlayModeTestKit.Set(controller, "m_playerController", m_rig.Player.GetComponent<PlayerController>());
            PlayModeTestKit.Set(controller, "m_playerDialogueHandler", handler);
            PlayModeTestKit.Set(controller, "m_dialogueUI", m_ui.UI);
            PlayModeTestKit.Set(controller, "m_playerHUD", m_hud);
            controllerObject.SetActive(true);

            DialogueNode opener = m_kit.NpcLine("Listen carefully.");
            DialogueNode more = m_kit.NpcLine("There's more.");
            DialogueTestKit.Link(opener, more);
            m_npc = m_kit.Npc("Talker", m_kit.Dialogue("Camera Dialogue", opener, more),
                              m_rig.Player.transform.position + new Vector3(2f, 0f, 0f), withDialogueCamera: true);
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
            m_rig.Dispose();
        }

        /* --- Helpers --- */

        private PlayerDialogueHandler Handler => m_rig.Player.GetComponent<PlayerDialogueHandler>();

        private static IEnumerator WaitRealtime(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        /* ============================== Tests ============================== */

        [UnityTest]
        [Description("After a conversation ends and the camera has returned, input is unlocked.")]
        public IEnumerator AfterTheConversationEnds_InputIsUnlocked()
        {
            yield return null;
            DialogueTestKit.Talk(Handler, m_npc);
            yield return WaitRealtime(kSettleTime);

            Handler.EndDialogue();
            yield return WaitRealtime(kSettleTime);

            Assert.That(m_inputGuard.IsInputLocked, Is.False);
        }

        [UnityTest]
        [Description("Quitting while the screen is still fading in to the conversation exits cleanly: input is unlocked and the HUD is back.")]
        public IEnumerator QuittingDuringTheFadeIn_ExitsCleanly()
        {
            yield return null;                              // DialogueUI.Start hides the panel before the conversation
            DialogueTestKit.Talk(Handler, m_npc);
            for (float waited = 0f; !m_ui.UI.gameObject.activeSelf; waited += Time.unscaledDeltaTime)
            {
                Assert.That(waited, Is.LessThan(kSettleTime), "fixture: the dialogue panel never appeared");
                yield return null;
            }
            Assert.That(m_fadeCanvas.alpha, Is.GreaterThan(0f), "fixture: the fade-in must still be running");

            m_ui.Quit.onClick.Invoke();
            yield return WaitRealtime(kSettleTime);

            Assert.That(m_inputGuard.IsInputLocked, Is.False);
            Assert.That(m_hud.activeSelf, Is.True);
        }
    }
}