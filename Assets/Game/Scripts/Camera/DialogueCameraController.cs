/*---------------------------
File: DialogueCameraController.cs
Author: Chandler Mays
----------------------------*/
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
//---------------------------------
using PolyQuest.SceneManagement;
using PolyQuest.Dialogues;
using PolyQuest.Player;
using PolyQuest.Quests;

namespace PolyQuest.Core
{
    public class DialogueCameraController : MonoBehaviour
    {
        [SerializeField] private Camera m_mainCamera;
        [SerializeField] private PlayerController m_playerController;
        [SerializeField] private PlayerDialogueHandler m_playerDialogueHandler;
        [SerializeField] private DialogueUI m_dialogueUI;
        [SerializeField] private GameObject m_playerHUD;

        private CinemachineCamera m_dialogueCamera;
        private bool m_isDialogueCameraActive = false;
        private bool m_isTransitioning = false;

        private QuestManager m_questManager;

        /*---------------------------------------------------------------- 
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_mainCamera, nameof(m_mainCamera));
            Utilities.CheckForNull(m_playerController, nameof(m_playerController));
            Utilities.CheckForNull(m_playerDialogueHandler, nameof(m_playerDialogueHandler));
            Utilities.CheckForNull(m_dialogueUI, nameof(m_dialogueUI));
            Utilities.CheckForNull(m_playerHUD, nameof(m_playerHUD));

            m_questManager = m_playerController.GetComponent<QuestManager>();
            Utilities.CheckForNull(m_questManager, nameof(m_questManager));
        }

        /*--------------------------------------------------------------------- 
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_playerDialogueHandler.OnDialogueStarted += HandleDialogueStarted;
            m_playerDialogueHandler.OnDialogueEnded += HandleDialogueEnded;
        }

        /*--------------------------------------------------------------------------- 
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_playerDialogueHandler.OnDialogueStarted -= HandleDialogueStarted;
            m_playerDialogueHandler.OnDialogueEnded -= HandleDialogueEnded;
        }

        /*------------------------------------------------------------------------------------- 
        | --- HandleDialogueStarted: Start the Coroutine to switch to the dialogue camera --- |
        -------------------------------------------------------------------------------------*/
        private void HandleDialogueStarted()
        {
            m_dialogueCamera = m_playerDialogueHandler.ActiveDialogueCamera;
            if (m_dialogueCamera == null)
            {
                Debug.LogError("DialogueCameraController: ActiveDialogueCamera is null when dialogue started.");
                return;
            }

            StartCoroutine(SwitchCameraRoutine(toDialogue: true));
        }

        /*------------------------------------------------------------------------------------ 
        | --- HandleDialogueEnded: Start the Coroutine to switch back to the main camera --- |
        ------------------------------------------------------------------------------------*/
        private void HandleDialogueEnded()
        {
            StartCoroutine(SwitchCameraRoutine(toDialogue: false));
        }

        /*------------------------------------------------------------------------------------ 
        | --- SwitchCameraRoutine: Coroutine to handle camera switching with fade effect --- |
        ------------------------------------------------------------------------------------*/
        private IEnumerator SwitchCameraRoutine(bool toDialogue)
        {
            if (m_isTransitioning)
                yield break;

            m_isTransitioning = true;
            m_playerController.ToggleInput(false);
            InputGuard.Instance.LockInput();

            var fade = TransitionFade.Instance;

            if (fade != null)
            {
                yield return fade.FadeOut();
            }

            // Hide player after fade completes (when screen is black)
            if (toDialogue)
            {
                m_playerController.SetPlayerVisibility(false);
            }

            if (!toDialogue)
            {
                SetDialogueUIVisibility(false);
            }

            if (m_isDialogueCameraActive != toDialogue)
            {
                m_isDialogueCameraActive = toDialogue;
                ApplyCameraState();
                m_playerController.ClearHighlight();
            }

            if (fade != null)
            {
                if (toDialogue)
                {
                    yield return fade.Wait();
                    SetDialogueUIVisibility(true);
                }

                // Show player as fade begins coming back in
                if (!toDialogue)
                {
                    m_playerController.SetPlayerVisibility(true);
                    m_playerController.ToggleInput(true);
                    InputGuard.Instance.UnlockInput();
                }

                yield return fade.FadeIn();
            }
            else
            {
                if (toDialogue)
                {
                    SetDialogueUIVisibility(true);
                }
                else
                {
                    m_playerController.SetPlayerVisibility(true);
                }
            }

            m_isTransitioning = false;

            if (!toDialogue)
            {
                m_questManager.FlushCompletionNotifs();
            }
        }

        /*-------------------------------------------------------------------------- 
        | --- ApplyCameraState: Enable/Disable cameras based on dialogue state --- |
        --------------------------------------------------------------------------*/
        private void ApplyCameraState()
        {
            if (m_isDialogueCameraActive)
            {
                m_mainCamera.enabled = false;

                if (m_dialogueCamera != null)
                {
                    m_dialogueCamera.gameObject.SetActive(true);
                }
            }
            else
            {
                m_mainCamera.enabled = true;

                if (m_dialogueCamera != null)
                {
                    m_dialogueCamera.gameObject.SetActive(false);
                    m_dialogueCamera = null;
                }
            }
        }

        /*--------------------------------------------------------------- 
        | --- SetDialogueUIVisibility: Show or hide the dialogue UI --- |
        ---------------------------------------------------------------*/
        private void SetDialogueUIVisibility(bool visible)
        {
            m_dialogueUI.gameObject.SetActive(visible);
            m_playerHUD.SetActive(!visible);
        }
    }
}