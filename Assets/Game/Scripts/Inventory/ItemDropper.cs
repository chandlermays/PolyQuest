/*---------------------------
| File: ItemDropper.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
//---------------------------------
using PolyQuest.Pickups;
using PolyQuest.Saving;

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
            if (pickup == null)
                return;

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
                // Picked up or destroyed: it's no longer in the world, even if Destroy hasn't run yet this frame
                if (droppedItem == null || droppedItem.IsCollected)
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
            m_droppedItems.RemoveAll(item => item == null || item.IsCollected);
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

            // Forget them now: Destroy only runs at the end of the frame, and a save in this frame must not report them
            m_droppedItems.Clear();
            m_otherSceneDroppedItems.Clear();
        }

        /*---------------------------------------------------------------------
        | --- CaptureState: Capture the state of dropped items for saving --- |
        ---------------------------------------------------------------------*/
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
                    [kWorldPositionKey] = drop.WorldPosition.ToToken(),
                    [kSceneBuildIndexKey] = drop.SceneBuildIndex
                };
                state.Add(dropState);
            }

            return state;
        }

        /*--------------------------------------------------------------------------------------------
        | --- RestoreState: Restores the saved drops; an entry that can't be restored is skipped --- |
        --------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            // The whole save is the wrong shape: ignore it and keep the drops in place
            if (state is not JArray stateArray)
                return;

            int currentScene = SceneManager.GetActiveScene().buildIndex;
            ClearExistingDrops();

            foreach (JToken entry in stateArray)
            {
                // One bad entry is skipped; the rest load
                if (!TryReadDrop(entry, out DroppedItemData data))
                {
                    WarnSkippedDrop(entry.ToString(Formatting.None), "the entry can't be read");
                    continue;
                }

                // Drops for other scenes stay as saved; they're spawned when their own scene loads
                if (data.SceneBuildIndex != currentScene)
                {
                    m_otherSceneDroppedItems.Add(data);
                    continue;
                }

                InventoryItem item = InventoryItem.FindByID(data.ItemID);
                if (item == null)
                {
                    WarnSkippedDrop(data.ItemID, "no item has that ID");
                    continue;
                }

                CreatePickupAtLocation(item, data.WorldPosition, data.Quantity);
            }
        }

        /*---------------------------------------------------------------------------------------------------
        | --- TryReadDrop: Reads a saved drop; false unless every field is there with the expected type --- |
        ---------------------------------------------------------------------------------------------------*/
        private static bool TryReadDrop(JToken entry, out DroppedItemData data)
        {
            data = default;
            if (entry is not JObject dropState)
                return false;

            JToken itemID = dropState[kItemIDKey];
            JToken quantity = dropState[kQuantityKey];
            JToken sceneBuildIndex = dropState[kSceneBuildIndexKey];
            if (itemID == null || itemID.Type != JTokenType.String ||
                quantity == null || quantity.Type != JTokenType.Integer ||
                sceneBuildIndex == null || sceneBuildIndex.Type != JTokenType.Integer)
                return false;

            if (!TryReadPosition(dropState[kWorldPositionKey], out Vector3 worldPosition))
                return false;

            data = new DroppedItemData
            {
                ItemID = itemID.Value<string>(),
                Quantity = quantity.Value<int>(),
                WorldPosition = worldPosition,
                SceneBuildIndex = sceneBuildIndex.Value<int>()
            };
            return !string.IsNullOrWhiteSpace(data.ItemID) && data.Quantity > 0;
        }

        /*-----------------------------------------------------------------------------
        | --- TryReadPosition: Reads a saved position with a number for each axis --- |
        -----------------------------------------------------------------------------*/
        private static bool TryReadPosition(JToken token, out Vector3 position)
        {
            position = default;
            if (token is not JObject axes)
                return false;

            foreach (string axis in new[] { "x", "y", "z" })
            {
                JToken value = axes[axis];
                if (value == null || (value.Type != JTokenType.Float && value.Type != JTokenType.Integer))
                    return false;
            }

            position = new Vector3(axes["x"].Value<float>(), axes["y"].Value<float>(), axes["z"].Value<float>());
            return true;
        }

        /*----------------------------------------------------------------------------------
        | --- WarnSkippedDrop: Editor warning naming a saved drop that wasn't restored --- |
        ----------------------------------------------------------------------------------*/
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void WarnSkippedDrop(string itemID, string reason)
        {
            Debug.LogWarning($"ItemDropper on '{name}': skipped a saved drop with item ID '{itemID}' because {reason}. The other drops loaded.", this);
        }
    }
}