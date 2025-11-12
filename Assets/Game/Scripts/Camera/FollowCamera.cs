using UnityEngine;
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

        private const KeyCode kRotateLeft = KeyCode.A;
        private const KeyCode kRotateRight = KeyCode.D;

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
            // Rotating the Camera to the Left
            if (Input.GetKey(kRotateLeft))
            {
                transform.RotateAround(m_target.position, Vector3.up, -m_rotationSpeed * Time.deltaTime);
            }
            // Rotating the Camera to the Right
            if (Input.GetKey(kRotateRight))
            {
                transform.RotateAround(m_target.position, Vector3.up, m_rotationSpeed * Time.deltaTime);
            }
        }
    }
}