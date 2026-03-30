using UnityEngine;
using UnityEngine.AI;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Inventories
{
    public class RandomDropper : ItemDropper
    {
        [SerializeField] private float m_distanceThreshold;
        [SerializeField] private EnemyLootTable m_enemyLootTable;

        private BaseStats m_baseStats;
        private Equipment m_equipment;

        private const float m_navMeshSampleRadius = 1f;
        private const int kMaxAttempts = 20;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_baseStats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_baseStats, nameof(m_baseStats));

            m_equipment = GetComponent<Equipment>();
            Utilities.CheckForNull(m_equipment, nameof(m_equipment));
        }

        /*------------------------------------------------------
        | --- RandomDrop: Drop a random selection of items --- |
        ------------------------------------------------------*/
        public void RandomDrop()
        {
            var drops = m_enemyLootTable.GetRandomDrops(m_baseStats.Level, m_equipment);
            foreach (var drop in drops)
            {
                DropItem(drop.Item, drop.Quantity);
            }
        }

        /*-----------------------------------------------------------------------
        | --- GetDropLocation: Get the location where items will be dropped --- |
        -----------------------------------------------------------------------*/
        protected override Vector3 GetDropLocation()
        {
            for (int i = 0; i < kMaxAttempts; ++i)
            {
                Vector3 randomPosition = transform.position + Random.insideUnitSphere * m_distanceThreshold;
                if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, m_navMeshSampleRadius, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

#if UNITY_EDITOR
            Debug.LogWarning($"Failed to find a valid drop location after {kMaxAttempts} attempts. Dropping at the dropper's position.");
#endif
            return transform.position;      // Failsafe if no valid position is found after max attempts
        }
    }
}