using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
//---------------------------------
using PolyQuest.SceneManagement;
using PolyQuest.Dialogues;

namespace PolyQuest.Core
{
    public class DialogueCameraController : MonoBehaviour
    {
        [SerializeField] private Camera m_mainCamera;
        [SerializeField] private CinemachineCamera m_dialogueCamera;
        [SerializeField] private PlayerDialogueHandler m_playerDialogueHandler;
        [SerializeField] private DialogueUI m_dialogueUI;

        private bool m_isDialogueCameraActive = false;
        private bool m_isTransitioning = false;

        /*---------------------------------------------------------------- 
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_mainCamera, nameof(m_mainCamera));
            Utilities.CheckForNull(m_dialogueCamera, nameof(m_dialogueCamera));
            Utilities.CheckForNull(m_playerDialogueHandler, nameof(m_playerDialogueHandler));
            Utilities.CheckForNull(m_dialogueUI, nameof(m_dialogueUI));
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

            var fade = TransitionFade.Instance;

            if (fade != null)
            {
                yield return fade.FadeOut();
            }

            if (!toDialogue)
            {
                SetDialogueUIVisibility(false);
            }

            if (m_isDialogueCameraActive != toDialogue)
            {
                m_isDialogueCameraActive = toDialogue;
                ApplyCameraState();
            }

            if (fade != null)
            {
                if (toDialogue)
                {
                    yield return fade.Wait();
                    SetDialogueUIVisibility(true);
                }
                yield return fade.FadeIn();
            }
            else
            {
                if (toDialogue)
                {
                    SetDialogueUIVisibility(true);
                }
            }

            m_isTransitioning = false;
        }

        /*-------------------------------------------------------------------------- 
        | --- ApplyCameraState: Enable/Disable cameras based on dialogue state --- |
        --------------------------------------------------------------------------*/
        private void ApplyCameraState()
        {
            if (m_isDialogueCameraActive)
            {
                m_mainCamera.enabled = false;
                m_dialogueCamera.gameObject.SetActive(true);
            }
            else
            {
                m_mainCamera.enabled = true;
                m_dialogueCamera.gameObject.SetActive(false);
            }
        }

        /*--------------------------------------------------------------- 
        | --- SetDialogueUIVisibility: Show or hide the dialogue UI --- |
        ---------------------------------------------------------------*/
        private void SetDialogueUIVisibility(bool visible)
        {
            m_dialogueUI.gameObject.SetActive(visible);
        }
    }
}