using UnityEngine;

namespace PolyQuest.UI
{
    public class BillboardUI : MonoBehaviour
    {
        private Camera m_camera;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_camera = Camera.main;
        }

        /*---------------------------------------------------------------------------------
        | --- LateUpdate: Called upon every frame after the internal animation update --- |
        ---------------------------------------------------------------------------------*/
        private void LateUpdate()
        {
            if (m_camera == null)
            {
                m_camera = Camera.main;
            }

            transform.rotation = m_camera.transform.rotation * Quaternion.Euler(0, 180f, 0);
        }
    }
}