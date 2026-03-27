using UnityEngine;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Core;

namespace PolyQuest.Healing
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Manages the player's interaction with a Healer NPC, including approach and healing.     *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Moves the player toward the target Healer NPC until within proximity.                  *
     *      - Triggers a full health replenish once the player is close enough.                      *
     *      - Integrates with ActionManager to cancel any competing actions on approach.             *
     * --------------------------------------------------------------------------------------------- */
    public class HealerInteractor : MonoBehaviour, IAction
    {
        private Healer m_targetHealer;

        private const float kProximityThreshold = 2.0f;

        private MovementComponent m_movementComponent;
        private ActionManager m_actionManager;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_movementComponent = GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_movementComponent, nameof(m_movementComponent));

            m_actionManager = GetComponent<ActionManager>();
            Utilities.CheckForNull(m_actionManager, nameof(m_actionManager));
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (m_targetHealer == null)
                return;

            if (Vector3.Distance(m_targetHealer.transform.position, transform.position) > kProximityThreshold)
            {
                m_movementComponent.MoveTo(m_targetHealer.transform.position);
            }
            else
            {
                m_movementComponent.Stop();
                m_targetHealer.Heal(this);
                m_targetHealer = null;
            }
        }

        /*---------------------------------------------------------------------------
        | --- SetTargetHealer: Sets the Healer NPC for the player to move toward --- |
        ---------------------------------------------------------------------------*/
        public void SetTargetHealer(Healer healer)
        {
            m_targetHealer = healer;
            m_actionManager.StartAction(this);
        }

        /*------------------------------------------------------------------
        | --- Cancel: Cancels the current movement toward the Healer NPC --- |
        ------------------------------------------------------------------*/
        public void Cancel()
        {
            m_targetHealer = null;
        }
    }
}