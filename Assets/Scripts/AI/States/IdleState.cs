using UnityEngine;

namespace PolyQuest.AI
{
    /// <summary>
    /// Idle state - AI does nothing and waits for events or transitions.
    /// This is the fallback state when patrol or other behaviors are not available.
    /// </summary>
    public class IdleState : IAIState
    {
        private readonly AIController m_controller;

        /// <summary>
        /// Constructor for IdleState.
        /// </summary>
        /// <param name="controller">Reference to the AI controller</param>
        public IdleState(AIController controller)
        {
            m_controller = controller;
        }

        /// <summary>
        /// Called when entering the idle state.
        /// </summary>
        public void Enter()
        {
            // Stop movement when entering idle
            if (m_controller.MovementComponent != null)
            {
                m_controller.MovementComponent.Stop();
            }
        }

        /// <summary>
        /// Called when exiting the idle state.
        /// </summary>
        public void Exit()
        {
            // Nothing to clean up
        }

        /// <summary>
        /// Called every frame while in idle state.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        public void Tick(float deltaTime)
        {
            // Check if we should transition to patrol if patrol component becomes available
            if (m_controller.PatrolComponent != null && m_controller.PatrolComponent.HasWaypoints)
            {
                m_controller.StateMachine.ChangeState("Patrol");
                return;
            }

            // For enemies, check if we have a target and should attack
            if (m_controller.Type == AIController.AIType.Enemy)
            {
                if (m_controller.CurrentTarget != null && 
                    m_controller.CombatComponent != null &&
                    m_controller.CombatComponent.CanAttack(m_controller.CurrentTarget))
                {
                    m_controller.StateMachine.ChangeState("Attack");
                    return;
                }
            }

            // Otherwise remain idle
        }

        /// <summary>
        /// Handle events while in idle state.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="data">Optional event data</param>
        public void OnEvent(string eventName, object data)
        {
            switch (eventName)
            {
                case "TargetDetected":
                    if (m_controller.Type == AIController.AIType.Enemy && data is GameObject target)
                    {
                        m_controller.SetTarget(target);
                        m_controller.StateMachine.ChangeState("Attack");
                    }
                    break;

                case "Aggravate":
                    if (m_controller.Type == AIController.AIType.Enemy)
                    {
                        m_controller.Aggravate();
                    }
                    break;
            }
        }
    }
}
