using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Inventories
{
    // Try implementing my custom RNG

    [CreateAssetMenu(menuName = "PolyQuest/Inventory/EnemyLootTable", fileName = "New Enemy Loot Table")]
    public class EnemyLootTable : ScriptableObject
    {
        [SerializeField] LootItemConfig[] m_guaranteedDrops;
        [SerializeField] LootItemConfig[] m_possibleDrops;
        [SerializeField] float[] m_dropChancePercentage;            // Chance to drop any loot at all
        [SerializeField] int[] m_minDrops;
        [SerializeField] int[] m_maxDrops;

        [System.Serializable]
        private class LootItemConfig
        {
            [SerializeField] private InventoryItem m_item;
            [SerializeField] private float[] m_relativeChance;      // Chance to drop this specific item relative to others
            [SerializeField] private int[] m_minQuantity;
            [SerializeField] private int[] m_maxQuantity;

            public InventoryItem Item => m_item;
            public float[] RelativeChance => m_relativeChance;
            public int[] MinQuantity => m_minQuantity;
            public int[] MaxQuantity => m_maxQuantity;

            /*---------------------------------------------------------------------------
            | --- GetRandomNumber: Get a random quantity of the item based on level --- |
            ---------------------------------------------------------------------------*/
            public int GetRandomNumber(int level)
            {
                if (!m_item.IsStackable)
                    return 1;

                int min = GetByLevel(m_minQuantity, level);
                int max = GetByLevel(m_maxQuantity, level);
                return Random.Range(min, max + 1);
            }
        }

        public struct Dropped
        {
            [SerializeField] private InventoryItem m_item;
            [SerializeField] private int m_quantity;

            public InventoryItem Item
            { get => m_item; set => m_item = value; }

            public int Quantity
            { get => m_quantity; set => m_quantity = value; }
        }

        /*--------------------------------------------------------------------------
        | --- GetRandomDrops: Get random drops based on level and drop chances --- |
        --------------------------------------------------------------------------*/
        public IEnumerable<Dropped> GetRandomDrops(int level, Equipment equipment)
        {
            // Equipped weapon always drops
            InventoryItem weapon = equipment.GetItemInSlot(EquipmentSlot.kWeapon);
            if (weapon != null)
            {
                yield return new Dropped
                {
                    Item = weapon,
                    Quantity = 1
                };
            }

            // Guaranteed drops — always yield, no roll
            if (m_guaranteedDrops != null)
            {
                foreach (var drop in m_guaranteedDrops)
                {
                    if (drop.Item == null) continue;
                    yield return new Dropped
                    {
                        Item = drop.Item,
                        Quantity = drop.GetRandomNumber(level)
                    };
                }
            }

            if (!ShouldDrop(level))
            {
                yield break;
            }
            for (int i = 0; i < GetRandomNumberOfDrops(level); ++i)
            {
                yield return GetRandomDrop(level);
            }
        }

        /*---------------------------------------------------------
        | --- GetRandomDrop: Get a random drop based on level --- |
        ---------------------------------------------------------*/
        private Dropped GetRandomDrop(int level)
        {
            LootItemConfig drop = SelectRandomItem(level);
            if (drop == null || drop.Item == null)
            {
                Debug.LogWarning($"EnemyLootTable '{name}': no drop selected for level {level}.");
                return default;
            }

            Dropped result = new()
            {
                Item = drop.Item,
                Quantity = drop.GetRandomNumber(level)
            };
            return result;
        }

        /*--------------------------------------------------------------------------
        | --- SelectRandomItem: Select a random item based on relative chances --- |
        --------------------------------------------------------------------------*/
        private LootItemConfig SelectRandomItem(int level)
        {
            if (m_possibleDrops == null || m_possibleDrops.Length == 0)
                return null;

            float totalChance = GetTotalChance(level);
            if (totalChance <= 0f)
            {
                // No defined chance values -> fall back to first configured drop to avoid returning null
                return m_possibleDrops[0];
            }

            float randomRoll = Random.Range(0f, totalChance);
            float chanceTotal = 0f;
            foreach (var drop in m_possibleDrops)
            {
                chanceTotal += GetByLevel(drop.RelativeChance, level);
                // return when the cumulative chance reaches/exceeds the roll
                if (randomRoll <= chanceTotal)
                {
                    return drop;
                }
            }

            // Fallback: if floating point edge cases prevent a return, return last entry
            return m_possibleDrops[m_possibleDrops.Length - 1];
        }

        /*------------------------------------------------------------------------------
        | --- ShouldDrop: Determine if any drops should occur based on drop chance --- |
        ------------------------------------------------------------------------------*/
        private bool ShouldDrop(int level)
        {
            return Random.Range(0f, 100f) < GetByLevel(m_dropChancePercentage, level);
        }

        /*-----------------------------------------------------------------------------
        | --- GetRandomNumberOfDrops: Get a random number of drops based on level --- |
        -----------------------------------------------------------------------------*/
        private int GetRandomNumberOfDrops(int level)
        {
            int min = GetByLevel(m_minDrops, level);
            int max = GetByLevel(m_maxDrops, level);
            return Random.Range(min, max + 1);
        }

        /*----------------------------------------------------------------------------------------
        | --- GetTotalChance: Calculate total relative chance for all drops at a given level --- |
        ----------------------------------------------------------------------------------------*/
        private float GetTotalChance(int level)
        {
            float total = 0;
            if (m_possibleDrops == null)
                return total;

            foreach (var drop in m_possibleDrops)
            {
                total += GetByLevel(drop.RelativeChance, level);
            }
            return total;
        }

        /*------------------------------------------------------------------------------
        | --- GetByLevel: Get value from array based on level with bounds checking --- |
        ------------------------------------------------------------------------------*/
        private static T GetByLevel<T>(T[] values, int level)
        {
            if (values == null || values.Length == 0)
                return default;

            if (level > values.Length)
                return values[values.Length - 1];

            if (level <= 0)
                return default;

            return values[level - 1];
        }
    }
}