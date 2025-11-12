using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Quests;

namespace PolyQuest.Core
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Tracks and manages the health state of registered enemies for objective completion.    *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Registers and unregisters enemies to monitor their alive/dead state.                  *
     *      - Listens for health changes on tracked enemies.                                        *
     *      - Updates the count of alive enemies in real time.                                      *
     *      - Notifies the ObjectiveCompletion component when all enemies are defeated.             *
     * -------------------------------------------------------------------------------------------- */
    public class EnemyTracker : MonoBehaviour
    {
        private ObjectiveCompletion m_objectiveCompletion;

        private readonly HashSet<HealthComponent> m_enemies = new();        // TODO: do I really need a hashset? what can a hashset do?
        private int m_aliveEnemyCount = 0;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_objectiveCompletion = GetComponent<ObjectiveCompletion>();
            Utilities.CheckForNull(m_objectiveCompletion, nameof(m_objectiveCompletion));
        }

        /*-----------------------------------------------------------------------
        | --- RegisterEnemy: Register an enemy to track their health status --- |
        -----------------------------------------------------------------------*/
        public void RegisterEnemy(HealthComponent enemy)
        {
            if (m_enemies.Add(enemy))
            {
                if (!enemy.IsDead)
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
                if (!enemy.IsDead)
                    --m_aliveEnemyCount;

                enemy.OnHealthChanged -= OnEnemyHealthChanged;
            }
        }

        /*---------------------------------------------------------------------
        | --- OnEnemyHealthChanged: Called when an enemy's health changes --- |         // TODO: This is a hefty method, we don't need to be checking
        ---------------------------------------------------------------------*/         // every time the health changes. We just need to know when they die.
        private void OnEnemyHealthChanged()
        {
            int alive = 0;
            foreach (var enemy in m_enemies)
            {
                if (!enemy.IsDead)
                    ++alive;
            }
            m_aliveEnemyCount = alive;

            if (m_aliveEnemyCount == 0)
            {
                m_objectiveCompletion.CompleteObjective();
            }
        }
    }
}