using System;
using UnityEngine;
//---------------------------------
namespace PolyQuest.Core
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Detects when the player enters or exits a defined area using a trigger collider.       *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Monitors player entry and exit events within a specified area.                        *
     *      - Logs area entry events for debugging or gameplay feedback.                            *
     *      - Ensures the trigger only responds once per entry until the player exits.              *
     * -------------------------------------------------------------------------------------------- */
    [RequireComponent(typeof(BoxCollider))]
    public class AreaTrigger : MonoBehaviour
    {
        [SerializeField] private string m_areaName;
        private const string kPlayerTag = "Player";

        public static event Action<string> OnAreaEntered;
        public static event Action<string> OnAreaExited;
        private bool m_hasEntered = false;

        /*------------------------------------------------------------------------
        | --- OnTriggerEnter: Called when the player enters the trigger area --- |
        ------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            if (!m_hasEntered && other.CompareTag(kPlayerTag))
            {
                OnAreaEntered?.Invoke(m_areaName);
                m_hasEntered = true;
            }
        }

        /*----------------------------------------------------------------------
        | --- OnTriggerExit: Called when the player exits the trigger area --- |
        ----------------------------------------------------------------------*/
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(kPlayerTag))
            {
                OnAreaExited?.Invoke(m_areaName);
                m_hasEntered = false;
            }
        }
    }
}