using UnityEngine;
using UnityEngine.InputSystem;
//---------------------------------

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
        [SerializeField] private InputActionReference m_rotateAction;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_target, nameof(m_target));
            Utilities.CheckForNull(m_rotateAction, nameof(m_rotateAction));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_rotateAction.action.Enable();
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_rotateAction.action.Disable();
        }

        /*------------------------------------------------------------------------------------------------------------
        | --- LateUpdate: Called upon every frame AFTER the internal animation update (Unity Order of Execution) --- |
        ------------------------------------------------------------------------------------------------------------*/
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
            float rotationInput = m_rotateAction.action.ReadValue<float>();
            if (Mathf.Abs(rotationInput) > 0.1f)
            {
                float rotationAmount = rotationInput * m_rotationSpeed * Time.deltaTime;
                transform.RotateAround(m_target.position, Vector3.up, rotationAmount);
            }
        }
    }
}