using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.UI.Tooltip
{
    /* --------------------------------------------------------------------------------------------
     * Role: Handles displaying item tooltips in the UI when hovering over item holders.           *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Determines if an item tooltip should be shown for the current UI element.            *
     *      - Updates the tooltip content with item details (name, description, etc.).             *
     *      - Integrates with the TooltipHandler base for pointer event handling.                  *
     * ------------------------------------------------------------------------------------------- */
    public class ItemTooltipHandler : TooltipHandler
    {
        /*--------------------------------------------------------------------------------
        | --- CanDisplayTooltip: Check if the tooltip can be displayed for this item --- |
        --------------------------------------------------------------------------------*/
        public override bool CanDisplayTooltip()
        {
            var item = GetComponent<IItemHolder>().GetItem();
            if (!item)
                return false;

            return true;
        }

        /*---------------------------------------------------------------------
        | --- UpdateTooltip: Update the tooltip content with item details --- |
        ---------------------------------------------------------------------*/
        public override void UpdateTooltip(GameObject tooltip)
        {
            ItemTooltipUI itemTooltip = tooltip.GetComponent<ItemTooltipUI>();
            if (!itemTooltip)
                return;

            var item = GetComponent<IItemHolder>().GetItem();
            itemTooltip.Setup(item);
        }
    }
}