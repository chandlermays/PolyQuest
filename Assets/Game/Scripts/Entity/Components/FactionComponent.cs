using UnityEngine;

namespace PolyQuest.Components
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Defines the faction/allegiance of an entity for determining combat relationships.       *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Stores the faction type for the entity.                                                *
     *      - Provides methods to determine hostility between factions.                              *
     * --------------------------------------------------------------------------------------------- */
    public enum Faction
    {
        kPlayer,
        kFriendly,
        kEnemy,
        kNeutral
    }

    public class FactionComponent : MonoBehaviour
    {
        [SerializeField] private Faction m_faction = Faction.kNeutral;

        public Faction Faction => m_faction;

        /*-----------------------------------------------------------------------
        | --- IsHostileTo: Check if this faction is hostile to another faction |
        -----------------------------------------------------------------------*/
        public bool IsHostileTo(FactionComponent other)
        {
            if (other == null)
                return false;

            return IsHostileTo(other.Faction);
        }

        /*-----------------------------------------------------------------------
        | --- IsHostileTo: Check if this faction is hostile to another faction |
        -----------------------------------------------------------------------*/
        public bool IsHostileTo(Faction otherFaction)
        {
            // Same faction is never hostile
            if (m_faction == otherFaction)
                return false;

            // kFriendly NPCs are never hostile to anyone
            if (m_faction == Faction.kFriendly || otherFaction == Faction.kFriendly)
                return false;

            // kPlayer vs kEnemy
            if (m_faction == Faction.kPlayer && otherFaction == Faction.kEnemy)
                return true;

            if (m_faction == Faction.kEnemy && otherFaction == Faction.kPlayer)
                return true;

            // kNeutral is not hostile to anyone by default
            return false;
        }
    }
}