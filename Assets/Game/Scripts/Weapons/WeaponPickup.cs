using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.Combat
{
    /* --------------------------------------------------------------------------------------------
     * Role: Allows the player to collect and equip a weapon by colliding with the pickup object.  *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Detects player collision with the pickup trigger.                                    *
     *      - Equips the weapon to the player's CombatComponent upon pickup.                       *
     *      - Destroys the pickup object after the weapon is collected.                            *
     * ------------------------------------------------------------------------------------------- */
    public class WeaponPickup : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] Weapon m_weapon;

        private const string kPlayerTag = "Player";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_weapon, nameof(m_weapon));
        }

        /*-----------------------------------------------------------------------------
        | --- OnTriggerEnter: Called when the Collider 'other' enters the trigger --- |
        -----------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(kPlayerTag))
            {
                other.GetComponent<CombatComponent>().EquipWeapon(m_weapon);
                Destroy(gameObject);
            }
        }
    }
}