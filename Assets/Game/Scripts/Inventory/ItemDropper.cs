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
    public class ItemDropper : MonoBehaviour, ISaveable
    {
        [System.Serializable]
        private struct DroppedItemData
        {
            public string ItemID { get; set; }
            public int Quantity { get; set; }
            public Vector3 WorldPosition { get; set; }
            public int SceneBuildIndex { get; set; }
        }

        private const string kItemIDKey = "ItemID";
        private const string kQuantityKey = "Quantity";
        private const string kWorldPositionKey = "WorldPosition";
        private const string kSceneBuildIndexKey = "SceneBuildIndex";

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

        /*-----------------------------------------------------------------------------
        | --- MergeAllDroppedItems: Combine current and other scene dropped items --- |
        -----------------------------------------------------------------------------*/
        private List<DroppedItemData> MergeAllDroppedItems()
        {
            List<DroppedItemData> result = new();
            result.AddRange(m_otherSceneDroppedItems);

            foreach (var droppedItem in m_droppedItems)
            {
                if (droppedItem == null)
                    continue;

                DroppedItemData data = new()
                {
                    ItemID = droppedItem.Item.ID,
                    Quantity = droppedItem.Quantity,
                    WorldPosition = droppedItem.transform.position,
                    SceneBuildIndex = SceneManager.GetActiveScene().buildIndex
                };
                result.Add(data);
            }
            return result;
        }

        /*---------------------------------------------------------------------------------
        | --- CleanUpDroppedItems: Remove null references from the dropped items list --- |
        ---------------------------------------------------------------------------------*/
        private void CleanUpDroppedItems()
        {
            m_droppedItems.RemoveAll(item => item == null);
        }

        /*------------------------------------------------------------------
        | --- ClearExistingDrops: Destroy all existing dropped pickups --- |
        ------------------------------------------------------------------*/
        private void ClearExistingDrops()
        {
            foreach (var oldDrop in m_droppedItems)
            {
                if (oldDrop != null)
                {
                    Destroy(oldDrop.gameObject);
                }
            }

            m_otherSceneDroppedItems.Clear();
        }

        /*----------------------------------------------------------------------
        | --- CaptureState: Capture the state of dropped items for saving --- |
        ----------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            CleanUpDroppedItems();
            var drops = MergeAllDroppedItems();
            JArray state = new();

            foreach (var drop in drops)
            {
                JObject dropState = new()
                {
                    [kItemIDKey] = drop.ItemID,
                    [kQuantityKey] = drop.Quantity,
                    [kWorldPositionKey] = JToken.FromObject(drop.WorldPosition),
                    [kSceneBuildIndexKey] = drop.SceneBuildIndex
                };
                state.Add(dropState);
            }

            return state;
        }

        /*---------------------------------------------------------------------------
        | --- RestoreState: Restore the state of dropped items from saved data --- |
        ---------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state is JArray stateArray)
            {
                int currentScene = SceneManager.GetActiveScene().buildIndex;
                ClearExistingDrops();

                foreach (var entry in stateArray)
                {
                    if (entry is JObject dropState)
                    {
                        string itemID = dropState[kItemIDKey].ToObject<string>();
                        int quantity = dropState[kQuantityKey].ToObject<int>();
                        Vector3 worldPosition = dropState[kWorldPositionKey].ToObject<Vector3>();
                        int sceneBuildIndex = dropState[kSceneBuildIndexKey].ToObject<int>();

                        InventoryItem item = InventoryItem.FindByID(itemID);

                        if (sceneBuildIndex == currentScene)
                        {
                            CreatePickupAtLocation(item, worldPosition, quantity);
                        }
                        else
                        {
                            DroppedItemData data = new()
                            {
                                ItemID = itemID,
                                Quantity = quantity,
                                WorldPosition = worldPosition,
                                SceneBuildIndex = sceneBuildIndex
                            };
                            m_otherSceneDroppedItems.Add(data);
                        }
                    }
                }
            }
        }
    }
}