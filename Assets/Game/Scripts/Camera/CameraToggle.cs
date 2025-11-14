using Unity.Cinemachine;
using UnityEngine;

namespace PolyQuest.Core
{
    public class CameraToggle : MonoBehaviour
    {
        [SerializeField] private Camera m_mainCamera;
        [SerializeField] private CinemachineCamera m_dialogueCamera;

        private bool m_isDialogueCameraActive = false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleCamera();
            }
        }

        private void ToggleCamera()
        {
            m_isDialogueCameraActive = !m_isDialogueCameraActive;

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
    }
}