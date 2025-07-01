using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    /*------------------------------------------------------------------------------ 
    | --- Responsible for the Movement of the Camera in Relation to the Player --- |
    ------------------------------------------------------------------------------*/
    public class FollowCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Transform m_target;
        [SerializeField] private float m_rotationSpeed = 100f;

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