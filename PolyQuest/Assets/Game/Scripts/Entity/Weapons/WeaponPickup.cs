using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    public class WeaponPickup : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] Weapon m_weapon;

        private const string PlayerTag = "Player";

        /*-----------------------------------------------------------------------------
        | --- OnTriggerEnter: Called when the Collider 'other' enters the trigger --- |
        -----------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PlayerTag))
            {
                other.GetComponent<CombatComponent>().EquipWeapon(m_weapon);
                Destroy(gameObject);
            }
        }
    }
}