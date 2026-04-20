/*---------------------------
File: EndDoor.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Core;
using PolyQuest.Player;

namespace PolyQuest.SceneManagement
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents the End Door that transitions the player to the next dungeon level.        *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Implements IAction so it integrates with ActionManager for click-to-move behavior.   *
     *      - Moves the player toward the door when clicked, mirroring Pickup movement logic.      *
     *      - Once the player is within range, movement stops and the Portal's trigger takes over, *
     *        which handles the actual scene transition.                                           *
     * ------------------------------------------------------------------------------------------- */
    public class EndDoor : MonoBehaviour, IAction
    {
        [Header("End Door Settings")]
        [SerializeField] private float m_interactRange = 2.0f;

        private Transform m_transform;
        private PlayerController m_targetPlayer;
        private MovementComponent m_playerMovement;
        private ActionManager m_actionManager;

        private const string kPlayerTag = "Player";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            var player = GameObject.FindWithTag(kPlayerTag);

            m_actionManager = player.GetComponent<ActionManager>();
            Utilities.CheckForNull(m_actionManager, nameof(m_actionManager));

            m_transform = transform;
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (m_targetPlayer == null)
                return;

            MoveToDoor();
        }

        /*------------------------------------------------------------------
        | --- MoveToDoor: Move the Player within range of the End Door --- |
        ------------------------------------------------------------------*/
        private void MoveToDoor()
        {
            float distanceToDoor = Vector3.Distance(m_playerMovement.transform.position, m_transform.position);

            if (distanceToDoor >= m_interactRange)
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

        /*---------------------------------------------------------------
        | --- SetAsTarget: Set this End Door as the player's target --- |
        ---------------------------------------------------------------*/
        public void SetAsTarget(PlayerController playerController)
        {
            m_targetPlayer = playerController;
            m_playerMovement = playerController.GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_playerMovement, nameof(m_playerMovement));

            m_actionManager.StartAction(this);
        }

        /*----------------------------------------------------------------------------------
        | --- Cancel: Clear the target, stopping any ongoing movement towards the door --- |
        ----------------------------------------------------------------------------------*/
        public void Cancel()
        {
            ClearTarget();
        }
    }
}