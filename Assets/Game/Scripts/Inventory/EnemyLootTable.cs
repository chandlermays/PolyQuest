/*---------------------------
| File: EnemyLootTable.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Inventories
{
    // NOTE: Integrate XorShift128Plus.cs with this class

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
                if (m_item == null)
                    return 0;

                if (!m_item.IsStackable)
                    return 1;

                return RollInclusive(GetByLevel(m_minQuantity, level), GetByLevel(m_maxQuantity, level));
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

            // Guaranteed drops: no drop-chance roll, but an unassigned entry or a quantity of 0 still drops nothing
            if (m_guaranteedDrops != null)
            {
                foreach (var drop in m_guaranteedDrops)
                {
                    if (TryRoll(drop, level, out Dropped dropped))
                        yield return dropped;
                }
            }

            if (!ShouldDrop(level))
            {
                yield break;
            }

            int count = GetRandomNumberOfDrops(level);
            for (int i = 0; i < count; ++i)
            {
                // The pick can come up empty (an unassigned entry, or no entry can drop at this level): that drop is nothing
                if (TryRoll(SelectRandomItem(level), level, out Dropped dropped))
                    yield return dropped;
            }
        }

        /*-----------------------------------------------------------------------------------------------------
        | --- TryRoll: Rolls an entry's quantity; false for no entry, no item, or a quantity of 0 or less --- |
        -----------------------------------------------------------------------------------------------------*/
        private static bool TryRoll(LootItemConfig drop, int level, out Dropped dropped)
        {
            dropped = default;
            if (drop == null || drop.Item == null)
                return false;

            int quantity = drop.GetRandomNumber(level);
            if (quantity <= 0)
                return false;

            dropped = new Dropped { Item = drop.Item, Quantity = quantity };
            return true;
        }

        /*----------------------------------------------------------------------------------------------------
        | --- SelectRandomItem: Picks an entry by weight at the level; null when no entry can drop there --- |
        ----------------------------------------------------------------------------------------------------*/
        private LootItemConfig SelectRandomItem(int level)
        {
            // Weight 0 means "can't drop at this level": if nothing has weight, the pool drops nothing (no fallback)
            float totalChance = GetTotalChance(level);
            if (totalChance <= 0f)
                return null;

            // Random.Range(0, total) can return total itself: skip weight-0 entries and compare with <, so neither
            // boundary roll can pick an entry that can't drop, and fall back to the last entry that can
            float randomRoll = Random.Range(0f, totalChance);
            float chanceTotal = 0f;
            LootItemConfig lastThatCanDrop = null;
            foreach (var drop in m_possibleDrops)
            {
                float weight = GetByLevel(drop.RelativeChance, level);
                if (weight <= 0f)
                    continue;

                lastThatCanDrop = drop;
                chanceTotal += weight;
                if (randomRoll < chanceTotal)
                {
                    return drop;
                }
            }

            return lastThatCanDrop;
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
            return RollInclusive(GetByLevel(m_minDrops, level), GetByLevel(m_maxDrops, level));
        }

        /*--------------------------------------------------------------------------------------
        | --- RollInclusive: A whole number from min to max; a max below min counts as min --- |
        --------------------------------------------------------------------------------------*/
        private static int RollInclusive(int min, int max)
        {
            if (max < min)
                max = min;

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
                total += Mathf.Max(0f, GetByLevel(drop.RelativeChance, level));
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

#if UNITY_EDITOR
        /*----------------------------------------------------------------------------------------------
        | --- OnValidate: Editor warnings for tables that can't drop what they look like they drop --- |
        ----------------------------------------------------------------------------------------------*/
        private void OnValidate()
        {
            if (m_guaranteedDrops != null)
            {
                for (int i = 0; i < m_guaranteedDrops.Length; ++i)
                {
                    LootItemConfig drop = m_guaranteedDrops[i];
                    if (drop?.Item == null)
                        continue;

                    // Empty arrays read as 0 at every level, so they count too
                    if (drop.Item.IsStackable && AllLevels(drop.MinQuantity, drop.MaxQuantity, (min, max) => Mathf.Max(min, max) <= 0))
                    {
                        Warn($"guaranteed drop {Describe(drop, i)} can only roll a quantity of 0, so it never drops. Fill in its per-level quantities.");
                    }

                    CheckQuantities("guaranteed drop", drop, i);
                }
            }

            if (m_possibleDrops != null && m_possibleDrops.Length > 0)
            {
                for (int i = 0; i < m_possibleDrops.Length; ++i)
                {
                    if (m_possibleDrops[i] != null)
                        CheckQuantities("possible drop", m_possibleDrops[i], i);
                }

                int levels = 0;
                foreach (LootItemConfig drop in m_possibleDrops)
                    levels = Mathf.Max(levels, drop?.RelativeChance?.Length ?? 0);

                bool anyCanDrop = false;
                for (int level = 1; level <= levels && !anyCanDrop; ++level)
                    anyCanDrop = GetTotalChance(level) > 0f;

                if (!anyCanDrop)
                    Warn("has possible drops, but every one has a weight of 0 at every level, so none of them can ever drop.");
            }

            string inverted = InvertedLevels(m_minDrops, m_maxDrops);
            if (inverted != null)
                Warn($"max drops is below min drops at level {inverted}; the count is clamped to min there.");
        }

        /*-------------------------------------------------------------------------------------------------
        | --- CheckQuantities: Warns about a stackable entry's min/max arrays: inverted or mismatched --- |
        -------------------------------------------------------------------------------------------------*/
        private void CheckQuantities(string kind, LootItemConfig drop, int index)
        {
            if (drop.Item == null || !drop.Item.IsStackable)
                return;

            int minLength = drop.MinQuantity?.Length ?? 0;
            int maxLength = drop.MaxQuantity?.Length ?? 0;
            if (minLength != maxLength)
                Warn($"{kind} {Describe(drop, index)} has {minLength} min quantities but {maxLength} max quantities; give both one value per level.");

            string inverted = InvertedLevels(drop.MinQuantity, drop.MaxQuantity);
            if (inverted != null)
                Warn($"{kind} {Describe(drop, index)} has a max quantity below its min at level {inverted}; the quantity is clamped to min there.");
        }

        /*------------------------------------------------------------------------------------------------
        | --- InvertedLevels: The levels (as text) where max is below min, or null if there are none --- |
        ------------------------------------------------------------------------------------------------*/
        private static string InvertedLevels(int[] min, int[] max)
        {
            List<int> levels = new();
            for (int level = 1; level <= LevelCount(min, max); ++level)
            {
                if (GetByLevel(max, level) < GetByLevel(min, level))
                    levels.Add(level);
            }
            return levels.Count > 0 ? string.Join(", ", levels) : null;
        }

        /*-----------------------------------------------------------------------------------------
        | --- AllLevels: Whether every level's min and max (read as the game reads them) pass --- |
        -----------------------------------------------------------------------------------------*/
        private static bool AllLevels(int[] min, int[] max, System.Func<int, int, bool> test)
        {
            for (int level = 1; level <= LevelCount(min, max); ++level)
            {
                if (!test(GetByLevel(min, level), GetByLevel(max, level)))
                    return false;
            }
            return true;
        }

        private static int LevelCount(int[] min, int[] max) => Mathf.Max(min?.Length ?? 0, max?.Length ?? 0);

        private static string Describe(LootItemConfig drop, int index) => $"#{index} ({drop.Item.name})";

        private void Warn(string problem) => Debug.LogWarning($"EnemyLootTable '{name}': {problem}", this);
#endif
    }
}