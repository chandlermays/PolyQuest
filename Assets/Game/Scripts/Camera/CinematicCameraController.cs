/*---------------------------
File: CineaticCameraController.cs
Author: Chandler Mays
----------------------------*/
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
//---------------------------------
using PolyQuest.SceneManagement;
using PolyQuest.Player;

namespace PolyQuest.Core
{
    public class CinematicCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera m_mainCamera;
        [SerializeField] private PlayerController m_playerController;
        [SerializeField] private CinematicTrigger m_cinematicTrigger;
        [SerializeField] private CanvasGroup m_uiCanvasGroup;

        private CinemachineCamera m_cinematicCamera;

        private bool m_isCinematicActive = false;
        private bool m_isTransitioning = false;

        /*---------------------------------------------------------------- 
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_mainCamera, nameof(m_mainCamera));
            Utilities.CheckForNull(m_playerController, nameof(m_playerController));
            Utilities.CheckForNull(m_cinematicTrigger, nameof(m_cinematicTrigger));
            Utilities.CheckForNull(m_uiCanvasGroup, nameof(m_uiCanvasGroup));
        }

        /*--------------------------------------------------------------------- 
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_cinematicTrigger.OnCinematicStarted += HandleCinematicStarted;
        }

        /*--------------------------------------------------------------------------- 
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_cinematicTrigger.OnCinematicStarted -= HandleCinematicStarted;
        }

        /*----------------------------------------------------------------------------- 
        | --- HandleCinematicStarted: Cache references then kick off the sequence --- |
        -----------------------------------------------------------------------------*/
        private void HandleCinematicStarted()
        {
            m_cinematicCamera = m_cinematicTrigger.ActiveCinematicCamera;

            if (m_cinematicCamera == null)
            {
                Debug.LogError("CinematicCameraController: cinematic camera or spline cart is null.");
                return;
            }

            StartCoroutine(CinematicSequenceRoutine());
        }

        /*------------------------------------------------------------------------------------------ 
        | --- CinematicSequenceRoutine: Full fade-switch-play-hold-switch-fade coroutine flow --- |
        ------------------------------------------------------------------------------------------*/
        private IEnumerator CinematicSequenceRoutine()
        {
            if (m_isTransitioning || m_isCinematicActive)
                yield break;

            m_isTransitioning = true;
            m_playerController.ToggleInput(false);
            InputGuard.Instance.LockInput();
            m_uiCanvasGroup.alpha = 0f; // Hide UI during cinematic

            var fade = TransitionFade.Instance;

            if (fade != null)
                yield return fade.FadeOut();

            // Hide the player and clear highlights to prevent them from showing up in the cinematic
            m_playerController.SetPlayerVisibility(false);
            m_playerController.ClearHighlight();

            m_isCinematicActive = true;
            ApplyCameraState();

            if (fade != null)
                yield return fade.FadeIn();

            m_isTransitioning = false;

            yield return PlaySplineCartToEnd();

            yield return new WaitForSeconds(m_cinematicTrigger.HoldDurationAtEnd);

            m_isTransitioning = true;

            if (fade != null)
                yield return fade.FadeOut();

            m_isCinematicActive = false;
            ApplyCameraState();

            // Restore player visibility after the cinematic ends
            m_playerController.SetPlayerVisibility(true);
            m_playerController.ToggleInput(true);
            InputGuard.Instance.UnlockInput();
            m_uiCanvasGroup.alpha = 1f; // Show UI after cinematic

            if (fade != null)
                yield return fade.FadeIn();

            m_isTransitioning = false;
        }

        /*-------------------------------------------------------------------------- 
        | --- PlaySplineCartToEnd: Drive the cart from 0 → 1 and await arrival --- |
        --------------------------------------------------------------------------*/
        private IEnumerator PlaySplineCartToEnd()
        {
            var splineDolly = m_cinematicCamera.GetComponent<CinemachineSplineDolly>();

            while (splineDolly.CameraPosition < 1f)
            {
                splineDolly.CameraPosition += m_cinematicTrigger.DollySpeed * Time.deltaTime;
                splineDolly.CameraPosition = Mathf.Clamp01(splineDolly.CameraPosition);
                yield return null;
            }
        }

        /*---------------------------------------------------------------------- 
        | --- ApplyCameraState: Toggle the camera based on cinematic state --- |
        ----------------------------------------------------------------------*/
        private void ApplyCameraState()
        {
            if (m_isCinematicActive)
            {
                m_mainCamera.enabled = false;

                if (m_cinematicCamera != null)
                    m_cinematicCamera.gameObject.SetActive(true);
            }
            else
            {
                m_mainCamera.enabled = true;

                if (m_cinematicCamera != null)
                {
                    m_cinematicCamera.gameObject.SetActive(false);
                    m_cinematicCamera = null;
                }
            }
        }
    }
}