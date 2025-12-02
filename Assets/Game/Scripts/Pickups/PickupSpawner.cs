using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Saving;

namespace PolyQuest.Pickups
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Responsible for managing the existence and state of a pickup in the world.            *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Spawns a Pickup object at a specific location with a given item and quantity.        *
     *      - Handles saving and restoring whether the pickup is present (ISaveable).              *
     *      - Destroys or respawns the pickup based on the game state (i.e. when loading a save).  *
     * ------------------------------------------------------------------------------------------- */
    public class PickupSpawner : MonoBehaviour, ISaveable
    {
        [SerializeField] private InventoryItem m_item;
        [SerializeField] private int m_quantity;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_item, nameof(m_item));
            SpawnPickup();
        }

        /*--------------------------------------------------------------------
        | --- GetPickup: Retrieve the Pickup component from the children --- |
        --------------------------------------------------------------------*/
        public Pickup GetPickup()
        {
            return GetComponentInChildren<Pickup>();
        }

        /*----------------------------------------------------------------
        | --- IsPickedUp: Check if the Pickup is currently picked up --- |
        ----------------------------------------------------------------*/
        public bool IsPickedUp()
        {
            return GetPickup() == null;
        }

        public JToken CaptureState()
        {
            return JToken.FromObject(IsPickedUp());
        }

        public void RestoreState(JToken state)
        {
            bool shouldBePickedUp = state.ToObject<bool>();

            if (shouldBePickedUp && !IsPickedUp())
            {
                DestroyPickup();
            }

            if (!shouldBePickedUp && IsPickedUp())
            {
                SpawnPickup();
            }
        }

        /*-----------------------------------------------------------------------------
        | --- SpawnPickup: Create a new Pickup instance at the spawner's position --- |
        -----------------------------------------------------------------------------*/
        private void SpawnPickup()
        {
            var spawnedPickup = m_item.SpawnPickup(transform.position, m_quantity);
            spawnedPickup.transform.SetParent(transform);
        }

        /*---------------------------------------------------------
        | --- DestroyPickup: Remove the Pickup from the world --- |
        ---------------------------------------------------------*/
        private void DestroyPickup()
        {
            if (GetPickup())
            {
                Destroy(GetPickup().gameObject);
            }
        }
    }
}