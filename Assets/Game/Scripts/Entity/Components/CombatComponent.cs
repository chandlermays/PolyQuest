//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Combat;
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Inventories;
using PolyQuest.Player;
using PolyQuest.UI.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PolyQuest.Components
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Handles combat logic and interactions for an entity, including attacking and targeting. *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Manages weapon equipping, attack behavior, and targeting logic.                        *
     *      - Integrates with movement and health components for combat flow.                        *
     *      - Implements IRaycastable for player interaction and targeting via raycast.              *
     *      - Controls attack animations and cooldowns.                                              *
     * --------------------------------------------------------------------------------------------- */
    public class CombatComponent : EntityComponent, IRaycastable, IStatModifier, IAction
    {
        [Header("Weapon Settings")]
        [SerializeField] private Weapon m_defaultWeapon;
        [SerializeField] private Transform m_leftHand;
        [SerializeField] private Transform m_rightHand;
        [SerializeField] private float m_attackCooldown = 1.0f;
        [SerializeField] private float m_autoAttackRange = 5.0f;
        [SerializeField] private LayerMask m_targetLayers;

        private float m_timeSinceLastAttack = Mathf.Infinity;

        /* --- References --- */
        private MovementComponent m_movementComponent;
        private HealthComponent m_healthComponent;
        private ActionManager m_actionManager;
        private Weapon m_currentWeapon;
        private BaseStats m_baseStats;
        private Equipment m_equipment;

        private GameObject m_target;
        private HealthComponent m_targetHealth;
        private Transform m_targetTransform;

        /* --- Animation Parameters --- */
        private static readonly int kAttack = Animator.StringToHash("Attack");
        private static readonly int kStopAttacking = Animator.StringToHash("StopAttacking");

        public GameObject Target => m_target;
        public LayerMask TargetLayers => m_targetLayers;

        private Outline m_outline;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();

            Utilities.CheckForNull(m_defaultWeapon, nameof(m_defaultWeapon));
            Utilities.CheckForNull(m_leftHand, nameof(m_leftHand));
            Utilities.CheckForNull(m_rightHand, nameof(m_rightHand));

            m_movementComponent = GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_movementComponent, nameof(m_movementComponent));

            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));

            m_actionManager = GetComponent<ActionManager>();
            Utilities.CheckForNull(m_actionManager, nameof(m_actionManager));

            m_baseStats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_baseStats, nameof(m_baseStats));

            m_equipment = GetComponent<Equipment>();

            m_outline = GetComponent<Outline>();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            if (m_equipment != null)
            {
                m_equipment.OnEquipmentChanged += UpdateWeapon;
            }
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            if (m_equipment != null)
            {
                m_equipment.OnEquipmentChanged -= UpdateWeapon;
            }
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            EquipWeapon(m_defaultWeapon);
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            m_timeSinceLastAttack += Time.deltaTime;

            if (m_healthComponent.IsDead || m_target == null)
                return;

            if (!IsTargetValid())
            {
                AcquireNewTarget();
                if (m_target == null)
                    return;
            }

            HandleCombatMovement();
        }

        /*----------------------------------------------------------------
        | --- HandleCombatMovement: Manage Movement and Attack Logic --- |
        ----------------------------------------------------------------*/
        private void HandleCombatMovement()
        {
            float distanceToTarget = Vector3.Distance(Transform.position, m_targetTransform.position);

            if (distanceToTarget >= m_currentWeapon.GetRange())
            {
                StopAttack();
                m_movementComponent.MoveTo(m_targetTransform.position);
            }
            else
            {
                m_movementComponent.Stop();
                AttackBehavior();
            }
        }

        /*---------------------------------------------------------------
        | --- IsTargetValid: Check if Current Target is Still Valid --- |
        ---------------------------------------------------------------*/
        private bool IsTargetValid()
        {
            return m_targetHealth != null && !m_targetHealth.IsDead;
        }

        /*--------------------------------------------------------------
        | --- AcquireNewTarget: Find and Set a New Target in Range --- |
        --------------------------------------------------------------*/
        private void AcquireNewTarget()
        {
            m_target = FindNearestTargetInRange();
            if (m_target != null)
            {
                SetTarget(m_target);
            }
        }

        /*---------------------------------------------------------
        | --- CanAttack: Checks if the Target can be Attacked --- |
        ---------------------------------------------------------*/
        public bool CanAttack(GameObject target)
        {
            // Check for a valid target
            if (target == null)
                return false;

            // Check faction compatibility
            FactionComponent thisFaction = GetComponent<FactionComponent>();
            FactionComponent targetFaction = target.GetComponent<FactionComponent>();
            if (thisFaction != null && targetFaction != null)
            {
                if (!thisFaction.IsHostileTo(targetFaction))
                    return false;
            }

            // Check if the target is within range/navigatable
            if (m_movementComponent.CanMoveTo(target.transform.position) == false)
                return false;

            m_targetHealth = target.GetComponent<HealthComponent>();
            return IsTargetValid();
        }

        /*--------------------------------------------------------------------
        | --- AttackBehavior: Perform the Behavior of Attacking a Target --- |
        --------------------------------------------------------------------*/
        public void AttackBehavior()
        {
            Transform.LookAt(m_targetTransform);

            if (m_timeSinceLastAttack > m_attackCooldown)
            {
                Animator.ResetTrigger(kStopAttacking);
                Animator.SetTrigger(kAttack);
                m_timeSinceLastAttack = 0;
            }
        }

        /*---------------------------------------------
        | --- SetTarget: Set the Target to Attack --- |
        ---------------------------------------------*/
        public void SetTarget(GameObject target)
        {
            m_actionManager.StartAction(this);
            m_target = target;
            m_targetTransform = target.transform;
            m_targetHealth = target.GetComponent<HealthComponent>();
        }

        /*-------------------------------------------
        | --- Cancel: Clear the Target and Stop --- |
        -------------------------------------------*/
        public void Cancel()
        {
            StopAttack();
            m_target = null;
            m_targetTransform = null;
            m_targetHealth = null;
        }

        /*-----------------------------------------------
        | --- StopAttack: Stop the Attack Animation --- |
        -----------------------------------------------*/
        public void StopAttack()
        {
            Animator.ResetTrigger(kAttack);
            Animator.SetTrigger(kStopAttacking);
        }

        /*--------------------------------------------------------------------------
        | --- EquipWeapon: Equip a Weapon and Spawn it in the Character's Hand --- |
        --------------------------------------------------------------------------*/
        public void EquipWeapon(Weapon weapon)
        {
            m_currentWeapon?.Remove();
            m_currentWeapon = weapon;
            weapon.Spawn(m_leftHand, m_rightHand, Animator);
        }

        /*---------------------------------------------------------------------------------------
        | --- GetHandTransform: Get the Transform of the Left or Right Hand based on Weapon --- |
        ---------------------------------------------------------------------------------------*/
        public Transform GetHandTransform(bool isRightHand)
        {
            return isRightHand ? m_rightHand : m_leftHand;
        }

        /*-----------------------------------------------------------
        | --- GetCursorType: Returns the Cursor Type for Combat --- |
        -----------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kTargeting;
        }

        /*--------------------------------------------------------------------------
        | --- HandleRaycast: The Behavior of the Raycast for Initiating Combat --- |
        --------------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (!playerController.GetComponent<CombatComponent>().CanAttack(gameObject))
                return false;
            
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                if (!playerController.IsTargeting)
                {
                    playerController.GetComponent<CombatComponent>().SetTarget(gameObject);
                }
            }

            return true;
        }

        /*-------------------------------------------------------------------------
        | --- GetAdditiveModifiers: Get Additive Modifiers for the Given Stat --- |
        -------------------------------------------------------------------------*/
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (stat == Stat.kDamage)
            {
                yield return m_currentWeapon.GetDamage();
            }
        }

        /*-----------------------------------------------------------------------------
        | --- GetPercentageModifiers: Get Percentage Modifiers for the Given Stat --- |
        -----------------------------------------------------------------------------*/
        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (stat == Stat.kDamage)
            {
                yield return m_currentWeapon.GetPercentageBonus();
            }
        }

        /*------------------------------------------------------------------------------
        | --- UpdateWeapon: Update the Equipped Weapon based on the Equipment Slot --- |
        ------------------------------------------------------------------------------*/
        private void UpdateWeapon()
        {
            Weapon weapon = m_equipment.GetItemInSlot(EquipmentSlot.kWeapon) as Weapon;
            EquipWeapon(weapon ?? m_defaultWeapon);
        }

        /*--------------------------------------------------------------------
        | --- PerformAttack: Execute the Attack with the Equipped Weapon --- |
        --------------------------------------------------------------------*/
        private void PerformAttack()
        {
            if (m_target == null)
                return;

            float damage = m_baseStats.GetDamage();

            if (m_currentWeapon.HasProjectile())
            {
                m_currentWeapon.LaunchProjectile(m_rightHand, m_leftHand, m_targetTransform, gameObject, damage);
            }
            else
            {
                m_targetHealth.TakeDamage(gameObject, damage);
            }
        }

        /*----------------------------------------------------------------------------
        | --- FindNearestTargetInRange: Locate Nearest Target within Auto-Attack --- |
        ----------------------------------------------------------------------------*/
        private GameObject FindNearestTargetInRange()
        {
            GameObject nearest = null;
            float nearestDistance = Mathf.Infinity;
            Vector3 position = transform.position;

            Collider[] colliders = Physics.OverlapSphere(position, m_autoAttackRange, m_targetLayers);
            FactionComponent myFaction = GetComponent<FactionComponent>();

            foreach (var collider in colliders)
            {
                if (collider.gameObject == gameObject)
                    continue;

                if (!collider.TryGetComponent<HealthComponent>(out var health) || health.IsDead)
                    continue;

                // Check faction compatibility
                if (myFaction != null && collider.TryGetComponent<FactionComponent>(out var targetFaction))
                {
                    if (!myFaction.IsHostileTo(targetFaction))
                        continue;
                }

                float distance = Vector3.Distance(position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = collider.gameObject;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /*--------------------------------------------------------------
        | --- Hit: Animation Event upon Attacking an Enemy (Sword) --- |
        --------------------------------------------------------------*/
        private void Hit()
        {
            PerformAttack();
        }

        /*--------------------------------------------------------------
        | --- Shoot: Animation Event upon Attacking an Enemy (Bow) --- |
        --------------------------------------------------------------*/
        private void Shoot()
        {
            PerformAttack();
        }

        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
        }
    }
}