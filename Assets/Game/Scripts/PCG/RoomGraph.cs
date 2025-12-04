using System.Collections.Generic;
using UnityEngine;

namespace PolyQuest.PCG
{
    public enum RoomType
    {
        kStart,
        kEnd,
        kBoss,
        kTreasure,
        kNormal,
        kUndefined
    }

    public class Room
    {
        public HashSet<Vector2Int> Tiles { get; private set; }
        public HashSet<int> ConnectedRooms { get; private set; }

        public Room()
        {
            Tiles = new HashSet<Vector2Int>();
            ConnectedRooms = new HashSet<int>();
        }
    }

    public class RoomGraph
    {
        private List<Room> m_rooms;
        private Dictionary<Vector2Int, int> m_tileRoomMapping;

        public RoomGraph()
        {
            m_rooms = new List<Room>();
            m_tileRoomMapping = new Dictionary<Vector2Int, int>();
        }

        public void AddTileToRoom(Vector2Int tilePosition, int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= m_rooms.Count)
                return;

            m_rooms[roomIndex].Tiles.Add(tilePosition);
            m_tileRoomMapping[tilePosition] = roomIndex;
        }

        public void LinkNode(int roomAIndex, int roomBIndex)
        {
            // Validate both room indices
            if (roomAIndex < 0 || roomAIndex >= m_rooms.Count ||
                roomBIndex < 0 || roomBIndex >= m_rooms.Count)
            {
                return;
            }

            // Prevent linking a room to itself
            if (roomAIndex == roomBIndex)
                return;

            // Create bidirectional connection
            m_rooms[roomAIndex].ConnectedRooms.Add(roomBIndex);
            m_rooms[roomBIndex].ConnectedRooms.Add(roomAIndex);
        }

        public int AddEmptyNode()
        {
            m_rooms.Add(new Room());
            return m_rooms.Count - 1;
        }

        public int FindRoomIndexByTile(Vector2Int tilePosition)
        {
            if (m_tileRoomMapping.TryGetValue(tilePosition, out int roomIndex))
            {
                return roomIndex;
            }

            return -1; // Tile not found in any room
        }

        public void Clear()
        {
            m_rooms.Clear();
            m_tileRoomMapping.Clear();
        }

        public int GetRoomCount()
        {
            return m_rooms.Count;
        }

        public Room GetRoom(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= m_rooms.Count)
            {
                Debug.LogError($"Invalid room index: {roomIndex}");
                return null;
            }

            return m_rooms[roomIndex];
        }

        public HashSet<Vector2Int> GetRoomTiles(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= m_rooms.Count)
            {
                Debug.LogError($"Invalid room index: {roomIndex}");
                return null;
            }

            return m_rooms[roomIndex].Tiles;
        }

        public HashSet<int> GetRoomConnections(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= m_rooms.Count)
            {
                Debug.LogError($"Invalid room index: {roomIndex}");
                return null;
            }

            return m_rooms[roomIndex].ConnectedRooms;
        }
    }
}