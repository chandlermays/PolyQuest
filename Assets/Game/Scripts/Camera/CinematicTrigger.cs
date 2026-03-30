using System;
using Unity.Cinemachine;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Core
{
    public class CinematicTrigger : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera m_cinematicCamera;

        [Header("Settings")]
        [Tooltip("Seconds to hold at the end of the path before returning to the main camera.")]
        [SerializeField] private float m_holdDurationAtEnd = 3f;
        [Tooltip("If true, this trigger can only fire once.")]
        [SerializeField] private bool m_triggerOnce = true;
        [Tooltip("Speed at which the camera travels along the spline (normalized 0–1 per second).")]
        [SerializeField] private float m_dollySpeed = 0.1f;

        private const string kPlayerTag = "Player";

        public event Action OnCinematicStarted;

        private bool m_hasTriggered = false;

        public CinemachineCamera ActiveCinematicCamera => m_cinematicCamera;
        public float HoldDurationAtEnd => m_holdDurationAtEnd;
        public float DollySpeed => m_dollySpeed;

        /*---------------------------------------------------------------- 
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_cinematicCamera, nameof(m_cinematicCamera));

            m_cinematicCamera.gameObject.SetActive(false);
        }

        /*------------------------------------------------------------------------- 
        | --- OnTriggerEnter: Called when a collider enters this trigger zone --- |
        -------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            if (m_triggerOnce && m_hasTriggered)
                return;

            if (!other.CompareTag(kPlayerTag))
                return;

            m_hasTriggered = true;
            OnCinematicStarted?.Invoke();
        }
    }
}