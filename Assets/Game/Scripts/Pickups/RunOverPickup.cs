using UnityEngine;
//---------------------------------

namespace PolyQuest.Pickups
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Enables automatic collection of pickups when the player collides with them.           *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Detects when the player enters the pickup's trigger collider.                        *
     *      - Calls the Pickup component to add the item to the player's inventory.                *
     *      - Provides a simple, physics-based pickup interaction (run-over to collect).           *
     * ------------------------------------------------------------------------------------------- */
    [RequireComponent(typeof(Pickup))]
    public class RunOverPickup : MonoBehaviour
    {
        private Pickup m_pickup;
        private const string kPlayerTag = "Player";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_pickup = GetComponent<Pickup>();
            Utilities.CheckForNull(m_pickup, nameof(m_pickup));
        }

        /*-------------------------------------------------------------------------
        | --- OnTriggerEnter: Called when another collider enters the trigger --- |
        -------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(kPlayerTag))
            {
                m_pickup.PickupItem();
            }
        }
    }
}