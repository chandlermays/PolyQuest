using UnityEngine;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Core;
using PolyQuest.Player;

namespace PolyQuest.SceneManagement
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents the Stairs that transition the player to the previous dungeon level        *
     *       or back to the overworld.                                                             *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Implements IAction so it integrates with ActionManager for click-to-move behavior.   *
     *      - Moves the player toward the stairs when clicked.                                     *
     *      - Once the player is within range, movement stops and the Portal's trigger takes over, *
     *        which handles the actual scene transition.                                           *
     * ------------------------------------------------------------------------------------------- */
    public class Stairs : MonoBehaviour, IAction
    {
        [Header("Stairs Settings")]
        [SerializeField] private float m_interactRange = 2.0f;

        private Transform m_transform;
        private PlayerController m_targetPlayer;
        private MovementComponent m_playerMovement;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_transform = transform;
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (m_targetPlayer == null)
                return;

            MoveToStairs();
        }

        /*------------------------------------------------------------------
        | --- MoveToStairs: Move the Player within range of the Stairs --- |
        ------------------------------------------------------------------*/
        private void MoveToStairs()
        {
            float distanceToStairs = Vector3.Distance(m_playerMovement.transform.position, m_transform.position);

            if (distanceToStairs >= m_interactRange)
            {
                m_playerMovement.MoveTo(m_transform.position);
            }
            else
            {
                m_playerMovement.Stop();
                ClearTarget();
            }
        }

        /*----------------------------------------------
        | --- ClearTarget: Clear the player target --- |
        ----------------------------------------------*/
        private void ClearTarget()
        {
            m_targetPlayer = null;
            m_playerMovement = null;
        }

        /*--------------------------------------------------------------
        | --- SetAsTarget: Set these Stairs as the player's target --- |
        --------------------------------------------------------------*/
        public void SetAsTarget(PlayerController playerController)
        {
            m_targetPlayer = playerController;
            m_playerMovement = playerController.GetComponent<MovementComponent>();

            ActionManager actionManager = playerController.GetComponent<ActionManager>();
            actionManager.StartAction(this);
        }

        /*------------------------------------------------------------------------------------
        | --- Cancel: Clear the target, stopping any ongoing movement towards the stairs --- |
        ------------------------------------------------------------------------------------*/
        public void Cancel()
        {
            ClearTarget();
        }
    }
}