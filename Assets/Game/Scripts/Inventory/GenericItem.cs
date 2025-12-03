using UnityEngine;
//---------------------------------

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(fileName = "New Generic Item", menuName = "PolyQuest/Items/Generic Item", order = 0)]
    public class GenericItem : InventoryItem
    {
        // Inherits all base functionality from InventoryItem
        // to be used for quest items, crafting materials, junk items, etc.
    }
}