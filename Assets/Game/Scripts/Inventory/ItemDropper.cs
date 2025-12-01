using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//---------------------------------
using PolyQuest.Saving;
using PolyQuest.Pickups;

namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Handles dropping items from the inventory into the game world as pickups.             *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Spawns Pickup objects at the appropriate world location when items are dropped.      *
     *      - Tracks all dropped items in the current scene.                                       *
     *      - Supports saving and restoring the state of dropped items across scenes.              *
     *      - Cleans up references to destroyed or collected pickups.                              *
     * ------------------------------------------------------------------------------------------- */
    public class ItemDropper : MonoBehaviour, ISaveable, IJsonSaveable
    {
        [System.Serializable]
        private struct DroppedItemData
        {
            [SerializeField] private string m_itemID;
            [SerializeField] private MySerializableVector3 m_worldPosition;
            [SerializeField] private int m_quantity;
            [SerializeField] private int m_sceneBuildIndex;

            public string ItemID
            { get => m_itemID; set => m_itemID = value; }

            public MySerializableVector3 WorldPosition
            { get => m_worldPosition; set => m_worldPosition = value; }

            public int Quantity
            { get => m_quantity; set => m_quantity = value; }

            public int SceneBuildIndex
            { get => m_sceneBuildIndex; set => m_sceneBuildIndex = value; }
        }

        private readonly List<Pickup> m_droppedItems = new();
        private readonly List<DroppedItemData> m_otherSceneDroppedItems = new();

        /*-------------------------------------------------------------------------------
        | --- DropItem: Drop an item at the drop location with a specified quantity --- |
        -------------------------------------------------------------------------------*/
        public void DropItem(InventoryItem item, int quantity)
        {
            CreatePickupAtLocation(item, GetDropLocation(), quantity);
        }

        /*-----------------------------------------------------
        | --- DropItem: Drop an item at the drop location --- |
        -----------------------------------------------------*/
        public void DropItem(InventoryItem item)
        {
            CreatePickupAtLocation(item, GetDropLocation(), 1);
        }

        /*-----------------------------------------------------------------------
        | --- GetDropLocation: Get the location where items will be dropped --- |
        -----------------------------------------------------------------------*/
        protected virtual Vector3 GetDropLocation()
        {
            return transform.position;
        }

        /*-----------------------------------------------------------------------------------
        | --- CreatePickupAtLocation: Spawn a Pickup instance at the specified location --- |
        -----------------------------------------------------------------------------------*/
        public void CreatePickupAtLocation(InventoryItem item, Vector3 location, int quantity)
        {
            Pickup pickup = item.SpawnPickup(location, quantity);
            m_droppedItems.Add(pickup);
        }

        /*---------------------------------------------------------------------
        | --- CaptureState: Capture the state of dropped items for saving --- |
        ---------------------------------------------------------------------*/
        public object CaptureState()
        {
            CleanUpDroppedItems();

            var droppedItemsList = new List<DroppedItemData>();
            int buildIndex = SceneManager.GetActiveScene().buildIndex;

            foreach (Pickup pickup in m_droppedItems)
            {
                if (pickup == null) continue;
                DroppedItemData droppedItem = new()
                {
                    ItemID = pickup.Item.ID,
                    WorldPosition = new MySerializableVector3(pickup.transform.position),
                    Quantity = pickup.Quantity,
                    SceneBuildIndex = buildIndex
                };
                droppedItemsList.Add(droppedItem);
            }

            droppedItemsList.AddRange(m_otherSceneDroppedItems);
            return droppedItemsList;
        }

        /*--------------------------------------------------------------------------
        | --- RestoreState: Restore the state of dropped items from saved data --- |
        --------------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            var droppedItemsList = (List<DroppedItemData>)state;
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            m_otherSceneDroppedItems.Clear();

            foreach(var item in droppedItemsList)
            {
                if (item.SceneBuildIndex != buildIndex)
                {
                    m_otherSceneDroppedItems.Add(item);
                    continue;
                }
                var pickupItem = InventoryItem.FindByID(item.ItemID);
                Vector3 position = item.WorldPosition.ToVector3();
                int quantity = item.Quantity;
                CreatePickupAtLocation(pickupItem, position, quantity);
            }
        }

        /*---------------------------------------------------------------------------------
        | --- CleanUpDroppedItems: Remove null references from the dropped items list --- |
        ---------------------------------------------------------------------------------*/
        private void CleanUpDroppedItems()
        {
            m_droppedItems.RemoveAll(item => item == null);
        }

        public JToken CaptureJToken()
        {
            throw new System.NotImplementedException();
        }

        public void RestoreJToken(JToken state)
        {
            throw new System.NotImplementedException();
        }
    }
}