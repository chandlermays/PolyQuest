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
     *      - Listens for enemy death events.                                                       *
     *      - Updates the count of alive enemies in real time.                                      *
     *      - Notifies the ObjectiveCompletion component when all enemies are defeated.             *
     * -------------------------------------------------------------------------------------------- */
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
            Utilities.CheckForNull(m_objectiveCompletion, nameof(m_objectiveCompletion));
        }

        /*-----------------------------------------------------------------------
        | --- RegisterEnemy: Register an enemy to track their health status --- |
        -----------------------------------------------------------------------*/
        public void RegisterEnemy(HealthComponent enemy)
        {
            if (enemy == null)
                return;

            if (m_enemies.Add(enemy))
            {
                if (!enemy.IsDead)
                {
                    ++m_aliveEnemyCount;
                }

                enemy.OnDeath += OnEnemyDeath;
            }
        }

        /*--------------------------------------------------------------------------------
        | --- UnregisterEnemy: Unregister an enemy from tracking their health status --- |
        --------------------------------------------------------------------------------*/
        public void UnregisterEnemy(HealthComponent enemy)
        {
            if (enemy == null)
                return;

            if (m_enemies.Remove(enemy))
            {
                if (!enemy.IsDead)
                {
                    --m_aliveEnemyCount;
                }

                enemy.OnDeath -= OnEnemyDeath;
            }
        }

        /*-----------------------------------------------------------
        | --- OnEnemyDeath: Called when a registered enemy dies --- |
        -----------------------------------------------------------*/
        private void OnEnemyDeath()
        {
            --m_aliveEnemyCount;

            if (m_aliveEnemyCount <= 0)
            {
                m_objectiveCompletion.CompleteObjective();
            }
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            foreach (var enemy in m_enemies)
            {
                if (enemy != null)
                {
                    enemy.OnDeath -= OnEnemyDeath;
                }
            }
            m_enemies.Clear();
        }
    }
}