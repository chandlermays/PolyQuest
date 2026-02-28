using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Room Pattern", menuName = "PolyQuest/PCG/Decorators/Room Pattern", order = 0)]
    public class RoomPattern : CompositePattern
    {
        [SerializeField] private RoomType m_roomType;

        public RoomType RoomType => m_roomType;

        /*-----------------------------------------------------------------------------
        | --- MatchesRoomType: Checks if this room pattern matches the room type --- |
        -----------------------------------------------------------------------------*/
        public bool MatchesRoomType(Room room)
        {
            return m_roomType == room.Type;
        }

        /*--------------------------------------------------------------------------------------
        | --- CanBeApplied: Checks if all of the patterns can be applied to the given room --- |
        --------------------------------------------------------------------------------------*/
        public override bool CanBeApplied(TileType[,] decoratedLevel, Room room)
        {
            foreach (DecoratorPattern pattern in Patterns)
            {
                if (!pattern.CanBeApplied(decoratedLevel, room))
                {
                    return false;
                }
            }

            return true;
        }

        /*-------------------------------------------------------------------------------------
        | --- Apply: Applies all of the patterns to the given room in the decorated level --- |
        -------------------------------------------------------------------------------------*/
        public override void Apply(TileType[,] decoratedLevel, Room room, Transform parent)
        {
            foreach (DecoratorPattern pattern in Patterns)
            {
                pattern.Apply(decoratedLevel, room, parent);
            }
        }
    }
}