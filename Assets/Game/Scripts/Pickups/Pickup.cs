using UnityEngine;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Core;
using PolyQuest.Inventories;
using PolyQuest.Player;
using PolyQuest.Quests;

namespace PolyQuest.Pickups
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents the actual collectible item in the game world.                             *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Holds data about the item and quantity.                                              *
     *      - Handles player interaction (i.e. adding the item to the inventory when picked up).   *
     *      - Contains logic for whether it can be picked up and what happens on pickup.           *
     * ------------------------------------------------------------------------------------------- */
    public class Pickup : MonoBehaviour, IAction
    {
        [SerializeField] private float m_pickupRange = 1.5f;

        private Inventory m_inventory;
        private InventoryItem m_item;
        private int m_quantity = 1;
        private Transform m_transform;

        private PlayerController m_targetPlayer;
        private MovementComponent m_playerMovement;
        private ActionManager m_actionManager;

        public InventoryItem Item => m_item;
        public int Quantity => m_quantity;

        private const string kPlayerTag = "Player";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            var player = GameObject.FindWithTag(kPlayerTag);
            m_inventory = player.GetComponent<Inventory>();
            Utilities.CheckForNull(m_inventory, nameof(m_inventory));

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

            MoveToPickup();
        }

        /*------------------------------------------------------------------
        | --- MoveToPickup: Move the Player within range of the Pickup --- |
        ------------------------------------------------------------------*/
        private void MoveToPickup()
        {
            float distanceToPickup = Vector3.Distance(m_playerMovement.transform.position, m_transform.position);

            if (distanceToPickup >= m_pickupRange)
            {
                m_playerMovement.MoveTo(m_transform.position);
            }
            else
            {
                m_playerMovement.Stop();
                PickupItem();
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

        /*----------------------------------------------------------------
        | --- Setup: Initialize the Pickup with an item and quantity --- |
        ----------------------------------------------------------------*/
        public void Setup(InventoryItem item, int quantity)
        {
            m_item = item;
            m_quantity = item.IsStackable ? quantity : 1;
        }

        /*-------------------------------------------------------------
        | --- SetAsTarget: Set this pickup as the player's target --- |
        -------------------------------------------------------------*/
        public void SetAsTarget(PlayerController playerController)
        {
            m_targetPlayer = playerController;
            m_playerMovement = playerController.GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_playerMovement, nameof(m_playerMovement));

            m_actionManager.StartAction(this);
        }

        /*--------------------------------------------------------------------------
        | --- PickupItem: Add the item to the inventory and destroy the Pickup --- |
        --------------------------------------------------------------------------*/
        public void PickupItem()
        {
            bool slotAvailable = m_inventory.TryAddToAvailableSlot(m_item, m_quantity);
            if (slotAvailable)
            {
                if (m_item is QuestItem questItem)
                {
                    QuestManager questManager = m_inventory.GetComponent<QuestManager>();
                    questManager.CompleteObjective(questItem.Quest, questItem.Objective);
                }
                Destroy(gameObject);
            }
        }

        /*---------------------------------------------------------------------------------------------
        | --- CanBePickedUp: Check if there's space in the inventory for the item to be picked up --- |
        ---------------------------------------------------------------------------------------------*/
        public bool CanBePickedUp()
        {
            if (m_inventory == null)
            {
                Debug.Log("Pickup: Inventory is not assigned on the player.");
                return false;
            }
            else
            {
                return m_inventory.HasSpaceFor(m_item);
            }
        }

        /*------------------------------------------------------------------------------------
        | --- Cancel: Clear the target, stopping any ongoing movement towards the pickup --- |
        ------------------------------------------------------------------------------------*/
        public void Cancel()
        {
            ClearTarget();
        }
    }
}