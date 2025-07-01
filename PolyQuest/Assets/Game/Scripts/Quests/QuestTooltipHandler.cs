using UnityEngine;

namespace PolyQuest
{
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