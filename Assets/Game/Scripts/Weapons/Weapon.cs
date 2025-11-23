using UnityEngine;
using System.Collections.Generic;
//---------------------------------
using PolyQuest.Inventories;
using PolyQuest.Attributes;

namespace PolyQuest.Combat
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Represents a weapon data asset, defining weapon properties and behavior.              *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores weapon stats, equipped prefab, projectile, and animation overrides.           *
     *      - Spawns and removes weapon instances on the character.                                *
     *      - Launches projectiles and sets their targets and damage.                              *
     * ------------------------------------------------------------------------------------------- */
    [CreateAssetMenu(menuName = "PolyQuest/Items/Weapon", fileName = "New Weapon", order = 0)]
    public class Weapon : EquipableItem, IStatModifier
    {
        [Header("Weapon Settings")]
        [SerializeField] private GameObject m_equippedPrefab;
        [SerializeField] private Projectile m_projectile;
        [SerializeField] private AnimatorOverrideController m_weaponAOC;
        [SerializeField] private float m_weaponDamage = 5f;
        [SerializeField] private float m_weaponRange = 2f;
        [SerializeField] private float m_percentageBonus = 0f;
        [SerializeField] private bool m_isRightHanded = true;

        /* --- References --- */
        private GameObject m_weaponInstance;

        private const string kWeaponName = "Weapon";

        /* --- Getter Methods --- */
        public bool HasProjectile() => m_projectile != null;
        public float GetDamage() => m_weaponDamage;
        public float GetRange() => m_weaponRange;
        public float GetPercentageBonus() => m_percentageBonus;

        /*---------------------------------------------------------------------------------
        | --- Spawn: Instantiate a Weapon Prefab at the Position of the Entity's Hand --- |
        ---------------------------------------------------------------------------------*/
        public void Spawn(Transform leftHand, Transform rightHand, Animator animator)
        {
            DestroyPreviousWeapon(leftHand, rightHand);

            if (m_equippedPrefab != null)
            {
                var chosenHand = GetHandPlacement(leftHand, rightHand);
                m_weaponInstance = Instantiate(m_equippedPrefab, chosenHand);
                m_weaponInstance.name = kWeaponName;
            }

            var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
            if (m_weaponAOC != null)
            {
                animator.runtimeAnimatorController = m_weaponAOC;
            }
            else if (overrideController != null)
            {
                animator.runtimeAnimatorController = overrideController.runtimeAnimatorController;
            }
        }

        /*---------------------------------------------------------------------------------------
        | --- DestroyPreviousWeapon: Remove any previously Equipped Weapon from either Hand --- |
        ---------------------------------------------------------------------------------------*/
        private void DestroyPreviousWeapon(Transform leftHand, Transform rightHand)
        {
            // Remove the use of strings to find the weapon

            Transform previousWeapon = rightHand.Find(kWeaponName);
            if (previousWeapon == null)
            {
                previousWeapon = leftHand.Find(kWeaponName);
            }

            if (previousWeapon == null)
                return;

            previousWeapon.name = "DESTROYING";
            Destroy(previousWeapon.gameObject);
        }

        /*-----------------------------------------------------------------------
        | --- LaunchProjectile: Instantiate a Projectile and Set its Target --- |
        -----------------------------------------------------------------------*/
        public void LaunchProjectile(Transform leftHand, Transform rightHand, Transform target, GameObject instigator, float damage)
        {
            Projectile newProjectile = Instantiate(m_projectile, GetHandPlacement(leftHand, rightHand).position, Quaternion.identity);
            newProjectile.SetTarget(target, instigator, damage);
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
        private Transform GetHandPlacement(Transform leftHand, Transform rightHand)
        {
            return m_isRightHanded ? rightHand : leftHand;
        }

        /*---------------------------------------------------------------------------
        | --- GetAdditiveModifiers: Get all additive modifiers from this weapon --- |
        ---------------------------------------------------------------------------*/
        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (stat == Stat.kDamage)
            {
                yield return m_weaponDamage;
            }
        }

        /*-------------------------------------------------------------------------------
        | --- GetPercentageModifiers: Get all percentage modifiers from this weapon --- |
        -------------------------------------------------------------------------------*/
        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (stat == Stat.kDamage)
            {
                yield return m_percentageBonus;
            }
        }
    }
}