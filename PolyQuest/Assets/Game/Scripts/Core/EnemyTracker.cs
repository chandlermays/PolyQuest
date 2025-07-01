using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    public class EnemyTracker : MonoBehaviour
    {
        private ObjectiveCompletion m_objectiveCompletion;

        private readonly HashSet<HealthComponent> m_enemies = new();
        private int m_aliveEnemyCount = 0;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_objectiveCompletion = GetComponent<ObjectiveCompletion>();
        }

        /*-----------------------------------------------------------------------
        | --- RegisterEnemy: Register an enemy to track their health status --- |
        -----------------------------------------------------------------------*/
        public void RegisterEnemy(HealthComponent enemy)
        {
            if (m_enemies.Add(enemy))
            {
                if (!enemy.IsDead())
                    ++m_aliveEnemyCount;

                enemy.OnHealthChanged += OnEnemyHealthChanged;
            }
        }

        /*--------------------------------------------------------------------------------
        | --- UnregisterEnemy: Unregister an enemy from tracking their health status --- |
        --------------------------------------------------------------------------------*/
        public void UnregisterEnemy(HealthComponent enemy)
        {
            if (m_enemies.Remove(enemy))
            {
                if (!enemy.IsDead())
                    --m_aliveEnemyCount;

                enemy.OnHealthChanged -= OnEnemyHealthChanged;
            }
        }

        /*---------------------------------------------------------------------
        | --- OnEnemyHealthChanged: Called when an enemy's health changes --- |
        ---------------------------------------------------------------------*/
        private void OnEnemyHealthChanged()
        {
            int alive = 0;
            foreach (var enemy in m_enemies)
            {
                if (!enemy.IsDead())
                    ++alive;
            }
            m_aliveEnemyCount = alive;

            if (m_aliveEnemyCount == 0)
            {
                m_objectiveCompletion?.CompleteObjective();
            }
        }
    }
}