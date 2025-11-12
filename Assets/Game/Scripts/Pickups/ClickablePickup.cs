using UnityEngine;
//---------------------------------
using PolyQuest.Player;
using PolyQuest.Core;
using PolyQuest.UI.Core;

namespace PolyQuest.Pickups
{
    /* --------------------------------------------------------------------------------------------------
     * Role: Enables pickups in the world to be collected via mouse click using raycast interaction.     *
     *                                                                                                   *
     * Responsibilities:                                                                                 *
     *      - Implements IRaycastable to allow pickups to be detected and interacted with by the player. *
     *      - Sets the mouse cursor to the pickup icon when hovered.                                     *
     *      - Handles mouse click input to collect the pickup and add it to the inventory.               *
     * ------------------------------------------------------------------------------------------------- */
    [RequireComponent(typeof(Pickup))]
    public class ClickablePickup : MonoBehaviour, IRaycastable
    {
        private Pickup m_pickup;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_pickup = GetComponent<Pickup>();
            Utilities.CheckForNull(m_pickup, nameof(m_pickup));
        }

        /*-------------------------------------------------------------
        | --- GetCursorType: Returns the cursor type for a pickup --- |
        -------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kPickup;
        }

        /*------------------------------------------------------------------------
        | --- HandleRaycast: Handles the raycast interaction with the pickup --- |
        ------------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (Input.GetMouseButtonDown(0))
            {
                m_pickup.PickupItem();
            }
            return true;
        }
    }
}