using UnityEngine;
using PolyQuest.Components;

namespace PolyQuest.AI
{
    /// <summary>
    /// Adapter/documentation for using the existing CombatComponent with the new AI system.
    /// The existing CombatComponent (Assets/Game/Scripts/Entity/Components/CombatComponent.cs) is fully functional
    /// and should be used directly by AI states.
    /// 
    /// Key API methods for AI usage:
    /// - bool CanAttack(GameObject target) - Check if target can be attacked
    /// - void SetTarget(GameObject target) - Set the current attack target
    /// - void AttackBehavior() - Perform attack on current target
    /// - void Cancel() - Clear target and stop attacking
    /// - void StopAttack() - Stop attack animation
    /// 
    /// The CombatComponent handles:
    /// - Attack cooldowns (m_attackCooldown field)
    /// - Attack range checking via weapon range
    /// - Movement to target when out of range
    /// - Attack animations and damage dealing
    /// - Auto-target acquisition within m_autoAttackRange
    /// 
    /// TODO for maintainers:
    /// - If AIData attack settings conflict with CombatComponent settings, decide which takes precedence
    /// - Consider exposing CombatComponent.m_attackCooldown as a public property for AIData synchronization
    /// - Consider adding a TryAttack(GameObject target) method that combines CanAttack + SetTarget + AttackBehavior
    /// - The CombatComponent currently manages its own movement - AI states may need to coordinate with this
    /// 
    /// This file exists only for documentation. Remove this file once the integration is verified
    /// and the above TODOs are addressed or documented in AI_Migration.md.
    /// </summary>
    public static class CombatComponentAdapter
    {
        /// <summary>
        /// Example usage pattern for AI states calling CombatComponent.
        /// This is a reference implementation - actual calls should be made directly to CombatComponent.
        /// </summary>
        public static void ExampleUsage(CombatComponent combat, GameObject target)
        {
            if (combat == null)
            {
                Debug.LogWarning("[CombatComponentAdapter] CombatComponent is null. Cannot attack.");
                return;
            }

            // Check if we can attack the target
            if (combat.CanAttack(target))
            {
                // Set the target
                combat.SetTarget(target);

                // The CombatComponent will handle movement and attacking in its Update loop
                // Alternatively, if the AI wants manual control, it can call:
                // combat.AttackBehavior();
            }
            else
            {
                // Target is not attackable - cancel combat
                combat.Cancel();
            }
        }

        /// <summary>
        /// Check if combat component is ready to attack (cooldown elapsed).
        /// This is an approximation - the CombatComponent doesn't expose this directly.
        /// TODO: Request CombatComponent expose a CanAttackNow() or IsOnCooldown property.
        /// </summary>
        public static bool IsReadyToAttack(CombatComponent combat)
        {
            // The CombatComponent manages this internally via m_timeSinceLastAttack
            // For now, AI states should just call AttackBehavior() and let CombatComponent handle timing
            return combat != null;
        }
    }
}
