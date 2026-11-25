/*---------------------------
| File: EnemyTracker.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Quests;
using PolyQuest.Saving;

namespace PolyQuest.Core
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Tracks an encounter's enemies and records it as cleared once every one of them is dead. *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Registers and unregisters enemies to monitor their alive/dead state.                  *
     *      - Counts enemies by whether they're dead, so ones a save restores dead count too.       *
     *      - Sets its WorldFlag (e.g. camp_cleared) on the player when the last enemy is dead;     *
     *        quest objectives read the flag, and the flag is saved.                                *
     * -------------------------------------------------------------------------------------------- */
    public class EnemyTracker : MonoBehaviour
    {
        [Tooltip("The fact this encounter records once every tracked enemy is dead, e.g. camp_cleared.")]
        [SerializeField] private WorldFlag m_clearedFlag;

        private readonly HashSet<HealthComponent> m_enemies = new();
        private bool m_isCleared = false;

        private const string kPlayerTag = "Player";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_clearedFlag, nameof(m_clearedFlag));

            // A load can leave every enemy dead without any of them dying now
            SavingSystem.RestoreFinished += HandleRestoreFinished;
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
                enemy.OnDeath += CheckCleared;
                enemy.OnHealthChanged += HandleHealthChanged;
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
                enemy.OnDeath -= CheckCleared;
                enemy.OnHealthChanged -= HandleHealthChanged;
            }
        }

        /*------------------------------------------------------------------------------------------------
        | --- HandleHealthChanged: A restore outside a full load can leave enemies dead: check again --- |
        ------------------------------------------------------------------------------------------------*/
        private void HandleHealthChanged()
        {
            // Mid-load, wait for RestoreFinished: the rest of the world (the Player's flags) isn't loaded yet
            if (!SavingSystem.IsRestoring)
            {
                CheckCleared();
            }
        }

        /*-----------------------------------------------------------------------------------
        | --- HandleRestoreFinished: The whole load is done: enemies it left dead count --- |
        -----------------------------------------------------------------------------------*/
        private void HandleRestoreFinished()
        {
            // A static event can outlive a tracker that was destroyed without OnDestroy running
            if (this == null)
            {
                SavingSystem.RestoreFinished -= HandleRestoreFinished;
                return;
            }

            CheckCleared();
        }

        /*-------------------------------------------------------------------------------------------
        | --- CheckCleared: Once every tracked enemy is dead, sets the cleared flag (only once) --- |
        -------------------------------------------------------------------------------------------*/
        private void CheckCleared()
        {
            if (m_isCleared || m_enemies.Count == 0)
                return;

            foreach (HealthComponent enemy in m_enemies)
            {
                if (enemy != null && !enemy.IsDead)
                    return;
            }

            m_isCleared = true;

            GameObject player = GameObject.FindWithTag(kPlayerTag);
            WorldFlags flags = player != null ? player.GetComponent<WorldFlags>() : null;
            if (flags == null)
            {
                Debug.LogWarning($"EnemyTracker on '{name}': every enemy is dead, but the Player has no WorldFlags to record '{(m_clearedFlag != null ? m_clearedFlag.ID : "no flag")}'.", this);
                return;
            }

            flags.Set(m_clearedFlag);
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            SavingSystem.RestoreFinished -= HandleRestoreFinished;

            foreach (var enemy in m_enemies)
            {
                if (enemy != null)
                {
                    enemy.OnDeath -= CheckCleared;
                    enemy.OnHealthChanged -= HandleHealthChanged;
                }
            }
            m_enemies.Clear();
        }
    }
}