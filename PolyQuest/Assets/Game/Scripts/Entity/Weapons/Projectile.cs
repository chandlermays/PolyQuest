using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float m_speed = 5f;
        [SerializeField] private float m_lifespan = 5f;

        /* --- References --- */
        private Transform m_transform;
        private Transform m_targetTransform;
        private GameObject m_instigator;

        private float m_damage = 0;

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
            if (m_targetTransform != null)
            {
                m_transform.Translate(m_speed * Time.deltaTime * Vector3.forward);
            }
        }

        /*----------------------------------------------------------------------------------
        | --- SetTarget: Assign the Target of this Projectile and the amount of Damage --- |
        ----------------------------------------------------------------------------------*/
        public void SetTarget(Transform target, GameObject instigator, float damage)
        {
            m_targetTransform = target;
            m_instigator = instigator;
            m_damage = damage;
            m_transform.LookAt(GetTargetAimPosition());

            // Destroy the Projectile after a certain amount of time
            Destroy(gameObject, m_lifespan);
        }

        /*--------------------------------------------------------------------------------------------------
        | --- GetTargetAimPosition: Calculate the Position of where the Projectile will hit the Target --- |
        --------------------------------------------------------------------------------------------------*/
        private Vector3 GetTargetAimPosition()
        {
            if (m_targetTransform.TryGetComponent<CapsuleCollider>(out var targetCollider))
            {
                // Aim at the center of the target's collider
                return m_targetTransform.position + Vector3.up * targetCollider.height / 2;
            }
            else
            {
                // Aim at the target's position
                return m_targetTransform.position;
            }
        }

        /*------------------------------------------------------------------------------ 
        | --- OnTriggerEnter: Called when the Collider 'other' enters this trigger --- |
        ------------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform == m_targetTransform)
            {
                var healthComponent = m_targetTransform.GetComponent<HealthComponent>();
                if (healthComponent != null && !healthComponent.IsDead())
                {
                    healthComponent.TakeDamage(m_instigator, m_damage);
                    Destroy(gameObject);
                }
            }
        }
    }
}