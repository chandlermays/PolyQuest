using UnityEngine;
//---------------------------------
using PolyQuest.Tools;

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(menuName = "PolyQuest/Items/Equipable Item", fileName = "New Equipable Item")]
    public class EquipableItem : InventoryItem
    {
        [Tooltip("The equipment slot this m_item can be equipped to.")]
        [SerializeField] private EquipmentSlot m_targetEquipmentSlot = EquipmentSlot.kNone;
        [SerializeField] private Condition m_equipCondition;

        public EquipmentSlot TargetEquipmentSlot => m_targetEquipmentSlot;

        /*---------------------------------------------------------------------------
        | --- CanEquip: Check if the item can be equipped to the specified slot --- |
        ---------------------------------------------------------------------------*/
        public bool CanEquip(EquipmentSlot equipmentSlot, Equipment equipment)
        {
            if (m_targetEquipmentSlot != equipmentSlot)
                return false;

            return m_equipCondition.Check(equipment.GetComponents<IConditionChecker>());
        }
    }
}