using UnityEngine;
//---------------------------------
using PolyQuest.Input;

namespace PolyQuest.Core
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Controls the camera to follow and rotate around a target.                              *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Keeps the camera positioned at the target's location each frame.                      *
     *      - Allows the player to rotate the camera around the target using input keys.            *
     *      - Provides configurable rotation speed and target assignment.                           *
     * -------------------------------------------------------------------------------------------- */

    public class FollowCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Transform m_target;
        [SerializeField] private float m_rotationSpeed = 100.0f;

        private PolyQuestInputActions m_inputActions;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_target, nameof(m_target));
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_inputActions = InputManager.Instance.InputActions;
        }

        /*---------------------------------------------------------------------------------
        | --- LateUpdate: Called upon every frame after the internal animation update --- |
        ---------------------------------------------------------------------------------*/
        private void LateUpdate()
        {
            transform.position = m_target.position;
        }

        /*---------------------------------------
        | --- Update: Called once per frame --- |
        ---------------------------------------*/
        private void Update()
        {
            HandleInput();
        }

        /*---------------------------------------------------
        | --- HandleInput: Control the Camera via Input --- |
        ---------------------------------------------------*/
        private void HandleInput()
        {
            float rotationInput = m_inputActions.Camera.Rotate.ReadValue<float>();
            if (Mathf.Abs(rotationInput) > 0.1f)
            {
                float rotationAmount = rotationInput * m_rotationSpeed * Time.deltaTime;
                transform.RotateAround(m_target.position, Vector3.up, rotationAmount);
            }
        }
    }
}