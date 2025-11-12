using UnityEngine;
using System.Collections.Generic;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Combat;
using PolyQuest.Player;
using PolyQuest.UI.Core;
using PolyQuest.Inventories;
using PolyQuest.Attributes;

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
    public class CombatComponent : EntityComponent, IRaycastable, IAttributeModifier
    {
        [Header("Weapon Settings")]
        [SerializeField] private Weapon m_defaultWeapon;
        [SerializeField] private Transform m_leftHand;
        [SerializeField] private Transform m_rightHand;
        [SerializeField] private float m_attackCooldown = 1.0f;
        [SerializeField] private float m_autoAttackRange = 5.0f;
        [SerializeField] private LayerMask m_targetLayers;

        private float m_lastAttackTime = Mathf.Infinity;

        /* --- References --- */
        private GameObject m_target;
        private Transform m_targetTransform;
        private MovementComponent m_movementComponent;
        private HealthComponent m_healthComponent;
        private Weapon m_currentWeapon;
        private BaseStats m_baseStats;
        private Equipment m_equipment;

        /* --- Animation Parameters --- */
        private const string kAttack = "Attack";
        private const string kStopAttacking = "StopAttacking";

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

            m_baseStats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_baseStats, nameof(m_baseStats));

            m_equipment = GetComponent<Equipment>();
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
            m_lastAttackTime += Time.deltaTime;

            if (m_healthComponent.IsDead)
                return;

            if (m_target == null)
                return;

            // If the target is dead, check your surroundings for a new one
            if (m_target.GetComponent<HealthComponent>().IsDead)
            {
                m_target = FindNewTargetInRange();
                if (m_target == null)
                    return;

                SetTarget(m_target);
            }

            // If the Player is not in Range, Move to the Target
            if (Vector3.Distance(Transform.position, m_targetTransform.position) >= m_currentWeapon.GetRange())
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

        /*---------------------------------------------
        | --- SetTarget: Set the Target to Attack --- |
        ---------------------------------------------*/
        public void SetTarget(GameObject target)
        {
            m_target = target;
            m_targetTransform = target.transform;
        }

        /*-----------------------------------------------
        | --- StopAttack: Stop the Attack Animation --- |
        -----------------------------------------------*/
        public void StopAttack()
        {
            Animator.ResetTrigger(kAttack);
            Animator.SetTrigger(kStopAttacking);
        }

        /*-------------------------------------------
        | --- Cancel: Clear the Target and Stop --- |
        -------------------------------------------*/
        public void Cancel()
        {
            StopAttack();
            m_target = null;
            m_targetTransform = null;
        }

        /*---------------------------------------------------------
        | --- CanAttack: Checks if the Target can be Attacked --- |
        ---------------------------------------------------------*/
        public bool CanAttack(GameObject target)
        {
            // Check for a valid target
            if (target == null)
                return false;

            // Check if the target is within range/navigatable
            if (m_movementComponent.CanMoveTo(target.transform.position) == false)
                return false;

            var targetHealth = target.GetComponent<HealthComponent>();
            return targetHealth != null && !targetHealth.IsDead;
        }

        /*--------------------------------------------------------------------
        | --- AttackBehavior: Perform the Behavior of Attacking a Target --- |
        --------------------------------------------------------------------*/
        public void AttackBehavior()
        {
            Transform.LookAt(m_targetTransform);

            if (m_lastAttackTime > m_attackCooldown)
            {
                m_movementComponent.Cancel();
                Animator.ResetTrigger(kStopAttacking);
                Animator.SetTrigger(kAttack);
                m_lastAttackTime = 0;
            }
        }

        /*--------------------------------------------------------------------------
        | --- EquipWeapon: Equip a Weapon and Spawn it in the Character's Hand --- |
        --------------------------------------------------------------------------*/
        public void EquipWeapon(Weapon weapon)
        {
            if (m_currentWeapon != null)
            {
                m_currentWeapon.Remove();
            }
            m_currentWeapon = weapon;
            weapon.Spawn(m_leftHand, m_rightHand, Animator);
        }

        /*------------------------------------------------------------------------------
        | --- UpdateWeapon: Update the Equipped Weapon based on the Equipment Slot --- |
        ------------------------------------------------------------------------------*/
        private void UpdateWeapon()
        {
            Weapon weapon = m_equipment.GetItemInSlot(EquipmentSlot.kWeapon) as Weapon;
            if (weapon != null)
            {
                EquipWeapon(weapon);
            }
            else
            {
                EquipWeapon(m_defaultWeapon);
            }
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

            if (Input.GetMouseButton(0))
            {
                playerController.GetComponent<CombatComponent>().SetTarget(gameObject);
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
                var health = m_target.GetComponent<HealthComponent>();
                health.TakeDamage(gameObject, damage);
            }
        }

        /*----------------------------------------------------------------------------
        | --- FindNewTargetInRange: Locate a New Target within Auto-Attack Range --- |
        ----------------------------------------------------------------------------*/
        private GameObject FindNewTargetInRange()
        {
            GameObject best = null;
            float bestDistance = Mathf.Infinity;
            foreach (var candidate in FindAllTargetsInRange())
            {
                float candidateDistance = Vector3.Distance(transform.position, candidate.transform.position);

                if (candidateDistance < bestDistance)
                {
                    best = candidate;
                    bestDistance = candidateDistance;
                }
            }

            return best;
        }

        /*-------------------------------------------------------------------------------
        | --- FindAllTargetsInRange: Get All Valid Targets within Auto-Attack Range --- |
        -------------------------------------------------------------------------------*/
        private IEnumerable<GameObject> FindAllTargetsInRange()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_autoAttackRange, m_targetLayers);

            foreach (var collider in colliders)
            {
                if (!collider.TryGetComponent<HealthComponent>(out var health))
                    continue;

                if (health.IsDead)
                    continue;

                if (collider.gameObject == gameObject)
                    continue;

                yield return health.gameObject;
            }
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
    }
}