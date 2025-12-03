using UnityEngine;

namespace PolyQuest.PCG
{
    public class Corridor
    {
        private Vector2Int m_startPosition;
        private Vector2Int m_endPosition;

        private Room m_startRoom;
        private Room m_endRoom;

        public Room StartRoom
        {
            get => m_startRoom;
            set => m_startRoom = value;
        }

        public Room EndRoom
        {
            get => m_endRoom;
            set => m_endRoom = value;
        }

        public Vector2Int StartPositionAbs => m_startPosition + m_startRoom.Area.position;
        public Vector2Int EndPositionAbs => m_endPosition + m_endRoom.Area.position;

        public Corridor(Vector2Int startPos, Room startRoom)
        {
            m_startPosition = startPos;
            m_startRoom = startRoom;
        }
    }
}