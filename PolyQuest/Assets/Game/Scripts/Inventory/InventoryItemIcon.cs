using UnityEngine;
using UnityEngine.UI;

namespace PolyQuest
{
    [RequireComponent(typeof(Image))]
    public class InventoryItemIcon : MonoBehaviour
    {
        /*-------------------------------------------------------------------------
        | --- SetItem: Set the inventory item icon based on the provided item --- |
        -------------------------------------------------------------------------*/
        public void SetItem(InventoryItem item)
        {
            if (!TryGetComponent<Image>(out var icon))
            {
                Debug.LogWarning($"{nameof(InventoryItemIcon)}: Missing Image component.");
                return;
            }

            if (item == null)
            {
                icon.enabled = false;
                icon.sprite = null;
            }
            else
            {
                Sprite sprite = item.GetIcon();
                if (sprite == null)
                {
                    Debug.LogWarning($"{nameof(InventoryItemIcon)}: Item '{item.GetName()}' has no icon assigned.");
                    icon.enabled = false;
                    icon.sprite = null;
                }
                else
                {
                    icon.enabled = true;
                    icon.sprite = sprite;
                }
            }
        }
    }
}