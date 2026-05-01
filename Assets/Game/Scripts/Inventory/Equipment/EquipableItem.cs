/*---------------------------
File: EquipableItem.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Tools;

namespace PolyQuest.Inventories
{
    [CreateAssetMenu(menuName = "PolyQuest/Items/Equipable Item", fileName = "New Equipable Item")]
    public class EquipableItem : InventoryItem, IStatModifier
    {
        [System.Serializable]
        private struct Modifier
        {
            [SerializeField] private Stat m_stat;
            [SerializeField] private float m_value;

            public readonly Stat Stat => m_stat;
            public readonly float Value => m_value;
        }

        [Tooltip("The equipment slot this item can be equipped to.")]
        [SerializeField] private EquipmentSlot m_targetEquipmentSlot = EquipmentSlot.kNone;
        [SerializeField] private Conjunction m_equipCondition;
        [SerializeField] private Modifier[] m_additiveModifiers;
        [SerializeField] private Modifier[] m_percentageModifiers;

        public EquipmentSlot TargetEquipmentSlot => m_targetEquipmentSlot;

        /*-----------------------------------------------------------------------
        | --- OnUse: Attempt to equip the item to the target equipment slot --- |
        -----------------------------------------------------------------------*/
        public override void OnUse(Inventory inventory, int slotIndex)
        {
            Equipment equipment = inventory.GetComponent<Equipment>();

            if (m_targetEquipmentSlot == EquipmentSlot.kNone)
                return;

            if (!CanEquip(m_targetEquipmentSlot, equipment))
                return;

            EquipableItem currentlyEquipped = equipment.GetItemInSlot(m_targetEquipmentSlot);
            inventory.RemoveItemsFromSlot(slotIndex, 1);

            if (currentlyEquipped != null)
            {
                inventory.SuppressItemAddedNotif = true;
                equipment.AddItem(m_targetEquipmentSlot, this);
                inventory.TryAddItemToSlot(slotIndex, currentlyEquipped, 1);
                inventory.SuppressItemAddedNotif = false;
            }
            else
            {
                equipment.AddItem(m_targetEquipmentSlot, this);
            }
        }

        /*---------------------------------------------------------------------------
        | --- CanEquip: Check if the item can be equipped to the specified slot --- |
        ---------------------------------------------------------------------------*/
        public bool CanEquip(EquipmentSlot equipmentSlot, Equipment equipment)
        {
            if (m_targetEquipmentSlot != equipmentSlot)
                return false;

            return m_equipCondition.Check(equipment.GetComponents<IConditionChecker>());
        }

        /*-----------------------------------------------------------------------------------
        | --- GetAdditiveModifiers: Get all additive modifiers from this equipable item --- |
        -----------------------------------------------------------------------------------*/
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            foreach (var modifier in m_additiveModifiers)
            {
                if (modifier.Stat == stat)
                    yield return modifier.Value;
            }
        }

        /*---------------------------------------------------------------------------------------
        | --- GetPercentageModifiers: Get all percentage modifiers from this equipable item --- |
        ---------------------------------------------------------------------------------------*/
        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            foreach (var modifier in m_percentageModifiers)
            {
                if (modifier.Stat == stat)
                    yield return modifier.Value;
            }
        }
    }
}