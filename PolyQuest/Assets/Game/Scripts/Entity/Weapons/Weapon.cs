using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "PolyQuest/Weapon")]
    public class Weapon : ScriptableObject
    {
        [Header("Weapon Settings")]
        [SerializeField] private GameObject m_equippedWeapon;
        [SerializeField] private Projectile m_projectile;
        [SerializeField] private AnimatorOverrideController m_weaponAOC;
        [SerializeField] private float m_weaponDamage = 5f;
        [SerializeField] private float m_weaponRange = 2f;
        [SerializeField] private bool m_isRightHanded = true;

        /* --- References --- */
        private GameObject m_weaponInstance;

        /* --- Getter Methods --- */
        public bool HasProjectile() => m_projectile != null;
        public float GetDamage() => m_weaponDamage;
        public float GetRange() => m_weaponRange;

        /*---------------------------------------------------------------------------------
        | --- Spawn: Instantiate a Weapon Prefab at the Position of the Entity's Hand --- |
        ---------------------------------------------------------------------------------*/
        public void Spawn(Transform left, Transform right, Animator animator)
        {
            if (m_equippedWeapon != null)
            {
                var chosenHand = GetHandPlacement(left, right);
                m_weaponInstance = Instantiate(m_equippedWeapon, chosenHand);
            }

            if (m_weaponAOC != null)
            {
                animator.runtimeAnimatorController = m_weaponAOC;
            }
            else if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
            {
                animator.runtimeAnimatorController = overrideController.runtimeAnimatorController;
            }
        }

        /*-----------------------------------------------------------------------
        | --- LaunchProjectile: Instantiate a Projectile and Set its Target --- |
        -----------------------------------------------------------------------*/
        public void LaunchProjectile(Transform left, Transform right, Transform target, GameObject instigator)
        {
            Projectile newProjectile = Instantiate(m_projectile, GetHandPlacement(right, left).position, Quaternion.identity);
            newProjectile.SetTarget(target, instigator, m_weaponDamage);
        }

        /*---------------------------------------------------------------------
        | --- Remove: Destroy the Currently Equipped Instance of a Weapon --- |
        ---------------------------------------------------------------------*/
        public void Remove()
        {
            Destroy(m_weaponInstance);
        }

        /*------------------------------------------------------------------------
        | --- GetHandPlacement: Returns which Hand the Weapon is Equipped in --- |
        ------------------------------------------------------------------------*/
        private Transform GetHandPlacement(Transform left, Transform right)
        {
            return m_isRightHanded ? right : left;
        }
    }
}