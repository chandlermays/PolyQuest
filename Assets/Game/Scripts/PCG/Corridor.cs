/*---------------------------
File: Corridor.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public enum Direction
    {
        kNorth,
        kSouth,
        kEast,
        kWest,
        kUndefined
    }

    public class Corridor
    {
        private readonly Direction m_startDirection;
        private Direction m_endDirection;

        private Vector2Int m_startPosition;
        private Vector2Int m_endPosition;

        private Room m_startRoom;
        private Room m_endRoom;

        public Direction StartDirection => m_startDirection;
        public Direction EndDirection
        {
            get => m_endDirection;
            set => m_endDirection = value;
        }

        public Vector2Int StartPosition
        {
            get => m_startPosition;
            set => m_startPosition = value;
        }
        public Vector2Int EndPosition
        {
            get => m_endPosition;
            set => m_endPosition = value;
        }

        public Vector2Int StartPositionAbs => m_startPosition + (m_startRoom != null ? m_startRoom.Area.position : Vector2Int.zero);
        public Vector2Int EndPositionAbs => m_endPosition + (m_endRoom != null ? m_endRoom.Area.position : Vector2Int.zero);

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

        /*------------------------------------------------------------------------------------------------------------------
        | --- Area: Computes the bounding rectangle that encompasses the corridor based on its start and end positions --- |
        ------------------------------------------------------------------------------------------------------------------*/
        public RectInt Area
        {
            get
            {
                var a = StartPositionAbs;
                var b = EndPositionAbs;

                int minX = Mathf.Min(a.x, b.x);
                int maxX = Mathf.Max(a.x, b.x);
                int minY = Mathf.Min(a.y, b.y);
                int maxY = Mathf.Max(a.y, b.y);

                int x = minX;
                int y = minY;
                int width = Mathf.Max(0, maxX - minX);
                int height = Mathf.Max(0, maxY - minY);

                return new RectInt(x, y, width, height);
            }
        }

        /*-------------------------------------------------------------------------------------------------------
        | --- Corridor: Constructor to create a corridor with the specified starting direction and position --- |
        -------------------------------------------------------------------------------------------------------*/
        public Corridor(Direction startDirection, Vector2Int startPosition, Room startRoom = null)
        {
            m_startDirection = startDirection;
            m_startPosition = startPosition;
            m_startRoom = startRoom;
        }

        /*----------------------------------------------------------------------------------------
        | --- GetColor: Utility method to get a color based on the direction of the corridor --- |
        ----------------------------------------------------------------------------------------*/
        public static Color GetColor(Direction direction)
        {
            return direction switch
            {
                Direction.kNorth => Color.cyan,
                Direction.kSouth => Color.green,
                Direction.kEast => Color.yellow,
                Direction.kWest => Color.magenta,
                Direction.kUndefined => Color.white,
                _ => throw new System.NotImplementedException()
            };
        }

        /*-------------------------------------------------------------------------------------------------
        | --- GetOppositeDirection: Utility method to get the opposite direction of a given direction --- |
        -------------------------------------------------------------------------------------------------*/
        public static Direction GetOppositeDirection(Direction direction)
        {
            return direction switch
            {
                Direction.kNorth => Direction.kSouth,
                Direction.kSouth => Direction.kNorth,
                Direction.kEast => Direction.kWest,
                Direction.kWest => Direction.kEast,
                Direction.kUndefined => Direction.kUndefined,
                _ => throw new System.NotImplementedException()
            };
        }
    }
}