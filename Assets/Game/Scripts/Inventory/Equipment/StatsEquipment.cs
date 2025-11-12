using System.Collections.Generic;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Inventories
{
    public class StatsEquipment : Equipment, IAttributeModifier
    {
        /*------------------------------------------------------------------------------
        | --- GetAdditiveModifiers: Get all additive modifiers from equipped items --- |
        ------------------------------------------------------------------------------*/
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            foreach (var slot in OccupiedSlots)
            {
                if (GetItemInSlot(slot) is not IAttributeModifier item)
                    continue;

                foreach (float modifier in item.GetAdditiveModifiers(stat))
                {
                    yield return modifier;
                }
            }
        }

        /*----------------------------------------------------------------------------------
        | --- GetPercentageModifiers: Get all percentage modifiers from equipped items --- |
        ----------------------------------------------------------------------------------*/
        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            foreach (var slot in OccupiedSlots)
            {
                if (GetItemInSlot(slot) is not IAttributeModifier item)
                    continue;

                foreach (float modifier in item.GetPercentageModifiers(stat))
                {
                    yield return modifier;
                }
            }
        }
    }
}