using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    /*-------------------------------------------- 
    | --- The Combat Component for an Entity --- |
    --------------------------------------------*/
    public class CombatComponent : EntityComponent, IRaycastable
    {
        [Header("Weapon Settings")]
        [SerializeField] private Weapon m_defaultWeapon;
        [SerializeField] private Transform m_leftHand;
        [SerializeField] private Transform m_rightHand;
        [SerializeField] private float m_attackCooldown = 1f;

        private float m_lastAttackTime = Mathf.Infinity;

        /* --- References --- */
        private GameObject m_target;
        private Transform m_targetTransform;
        private MovementComponent m_movementComponent;
        private HealthComponent m_healthComponent;
        private Weapon m_currentWeapon;

        /* --- Animation Parameters --- */
        private const string kAttack = "Attack";
        private const string kStopAttacking = "StopAttacking";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();

            m_movementComponent = GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_movementComponent, nameof(m_movementComponent));

            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));
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

            if (m_healthComponent.IsDead())
                return;

            if (m_target == null || m_target.GetComponent<HealthComponent>().IsDead())
                return;

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
        | --- SetTarget: Set the Target to kAttack --- |
        ---------------------------------------------*/
        public void SetTarget(GameObject target)
        {
            m_target = target;
            m_targetTransform = target.transform;
        }

        /*-----------------------------------------------
        | --- StopAttack: Stop the kAttack Animation --- |
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
            return targetHealth != null && !targetHealth.IsDead();
        }

        /*------------------------------------------------------------
        | --- kAttack: Perform the Behavior of Attacking a Target --- |
        ------------------------------------------------------------*/
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

        /*----------------------------------------------------------------------------
        | --- EquipWeapon: Equip a Weapon based on the Prefab (ScriptableObject) --- |
        ----------------------------------------------------------------------------*/
        public void EquipWeapon(Weapon weapon)
        {
            if (m_currentWeapon != null)
            {
                m_currentWeapon.Remove();
            }
            m_currentWeapon = weapon;
            weapon.Spawn(m_leftHand, m_rightHand, Animator);
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

        /*--------------------------------------------------------------------
        | --- PerformAttack: Execute the kAttack with the Equipped Weapon --- |
        --------------------------------------------------------------------*/
        private void PerformAttack()
        {
            if (m_target == null)
                return;

            if (m_currentWeapon.HasProjectile())
            {
                m_currentWeapon.LaunchProjectile(m_rightHand, m_leftHand, m_targetTransform, gameObject);
            }
            else
            {
                var health = m_target.GetComponent<HealthComponent>();
                health.TakeDamage(gameObject, m_currentWeapon.GetDamage());
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