/*---------------------------
| File: WorldFlagTrigger.cs
| Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.Quests
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Records that the player reached a place, as a WorldFlag, whether or not a quest asks   *
     *       for it yet.                                                                            *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Monitors trigger collisions for the player using Unity's physics system.              *
     *      - Sets its WorldFlag on the player's WorldFlags when the player enters.                 *
     *      - Leaves completing objectives to the quests whose conditions read the flag.            *
     * -------------------------------------------------------------------------------------------- */
    [RequireComponent(typeof(Collider))]
    public class WorldFlagTrigger : MonoBehaviour
    {
        [Tooltip("The fact this place records when the player enters, e.g. camp_entered.")]
        [SerializeField] private WorldFlag m_flag;

        private const string kPlayerTag = "Player";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_flag, nameof(m_flag));
        }

        /*-------------------------------------------------------------------------
        | --- OnTriggerEnter: Called when another collider enters the trigger --- |
        -------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(kPlayerTag))
                return;

            WorldFlags flags = other.GetComponentInParent<WorldFlags>();
            if (flags != null)
            {
                flags.Set(m_flag);
            }
        }
    }
}