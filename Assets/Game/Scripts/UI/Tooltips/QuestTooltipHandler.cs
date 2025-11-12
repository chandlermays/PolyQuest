using UnityEngine;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.UI.Tooltip
{
    /* --------------------------------------------------------------------------------------------
     * Role: Handles displaying quest tooltips in the UI when hovering over quest UI elements.     *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Determines if a quest tooltip should be shown for the current UI element.            *
     *      - Updates the tooltip content with quest status and details.                           *
     *      - Integrates with the TooltipHandler base for pointer event handling.                  *
     * ------------------------------------------------------------------------------------------- */
    public class QuestTooltipHandler : TooltipHandler
    {
        /*------------------------------------------------------------------
        | --- CanDisplayTooltip: Check if the tooltip can be displayed --- |
        ------------------------------------------------------------------*/
        public override bool CanDisplayTooltip()
        {
            return true;
        }

        /*----------------------------------------------------------------------
        | --- UpdateTooltip: Update the tooltip with the quest information --- |
        ----------------------------------------------------------------------*/
        public override void UpdateTooltip(GameObject tooltip)
        {
            QuestStatus status = GetComponent<QuestUI>().GetQuestStatus();
            tooltip.GetComponent<QuestTooltipUI>().Setup(status);
        }
    }
}