using UnityEngine;

namespace PolyQuest.AI
{
    /// <summary>
    /// Suspicion state - AI is alert after losing sight of target.
    /// Waits for suspicion timer to expire before returning to patrol/idle.
    /// </summary>
    public class SuspicionState : IAIState
    {
        private readonly AIControllerNew m_controller;
        private float m_suspicionTimer;

        /// <summary>
        /// Constructor for SuspicionState.
        /// </summary>
        /// <param name="controller">Reference to the AI controller</param>
        public SuspicionState(AIControllerNew controller)
        {
            m_controller = controller;
        }

        /// <summary>
        /// Called when entering the suspicion state.
        /// </summary>
        public void Enter()
        {
            m_suspicionTimer = 0f;

            // Stop movement when entering suspicion
            if (m_controller.MovementComponent != null)
            {
                m_controller.MovementComponent.Stop();
            }

            // Clear combat target but remain alert
            if (m_controller.CombatComponent != null)
            {
                m_controller.CombatComponent.Cancel();
            }
        }

        /// <summary>
        /// Called when exiting the suspicion state.
        /// </summary>
        public void Exit()
        {
            // Clear aggro when leaving suspicion
            m_controller.ClearAggro();
        }

        /// <summary>
        /// Called every frame while in suspicion state.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        public void Tick(float deltaTime)
        {
            m_suspicionTimer += deltaTime;

            // Check if target is re-detected
            if (m_controller.CurrentTarget != null)
            {
                if (m_controller.CombatComponent != null && 
                    m_controller.CombatComponent.CanAttack(m_controller.CurrentTarget))
                {
                    m_controller.StateMachine.ChangeState("Attack");
                    return;
                }
            }

            // Check if suspicion timer expired
            float suspicionTime = m_controller.Data != null ? m_controller.Data.SuspicionTime : 3f;
            
            if (m_suspicionTimer >= suspicionTime)
            {
                // Return to patrol or idle
                if (m_controller.PatrolComponent != null && m_controller.PatrolComponent.HasWaypoints)
                {
                    m_controller.StateMachine.ChangeState("Patrol");
                }
                else
                {
                    m_controller.StateMachine.ChangeState("Idle");
                }
            }
        }

        /// <summary>
        /// Handle events while in suspicion state.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="data">Optional event data</param>
        public void OnEvent(string eventName, object data)
        {
            switch (eventName)
            {
                case "TargetDetected":
                    if (data is GameObject target)
                    {
                        m_controller.SetTarget(target);
                        m_controller.StateMachine.ChangeState("Attack");
                    }
                    break;

                case "Aggravate":
                    m_controller.Aggravate();
                    // Re-enter attack if we have a target
                    if (m_controller.CurrentTarget != null)
                    {
                        m_controller.StateMachine.ChangeState("Attack");
                    }
                    break;

                case "TargetLost":
                    // Already in suspicion, reset timer
                    m_suspicionTimer = 0f;
                    break;
            }
        }
    }
}
