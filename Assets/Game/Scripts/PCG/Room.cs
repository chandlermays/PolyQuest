/*---------------------------
File: Room.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public enum RoomType
    {
        kStart,
        kEnd,
        kBoss,
        kTreasure,
        kNormal
    }

    public class Room
    {
        private RoomType m_type;
        private RectInt m_area;
        private readonly List<Corridor> m_corridors;

        public RoomType Type
        {
            get => m_type;
            set => m_type = value;
        }

        public RectInt Area => m_area;
        public int NumCorridors => m_corridors.Count;

        /*--------------------------------------------------------------------
        | --- Room: Constructor to create a room with the specified area --- |
        --------------------------------------------------------------------*/
        public Room(RectInt area)
        {
            m_type = RoomType.kNormal;
            m_area = area;
            m_corridors = new List<Corridor>();
        }

        /*--------------------------------------------------------------------------------------------
        | --- GenerateCorridorCandidates: Generates possible corridor candidates around the room --- |
        --------------------------------------------------------------------------------------------*/
        public List<Corridor> GenerateCorridorCandidates(int minDistanceFromEdge)
        {
            List<Corridor> candidates = new();

            int top = m_area.height - 1;
            int minX = minDistanceFromEdge;
            int maxX = m_area.width - minDistanceFromEdge;

            for (int x = minX; x < maxX; ++x)
            {
                candidates.Add(new Corridor(Direction.kSouth, new Vector2Int(x, 0)));
                candidates.Add(new Corridor(Direction.kNorth, new Vector2Int(x, top)));
            }

            int right = m_area.width - 1;
            int minY = minDistanceFromEdge;
            int maxY = m_area.height - minDistanceFromEdge;

            for (int y = minY; y < maxY; ++y)
            {
                candidates.Add(new Corridor(Direction.kWest, new Vector2Int(0, y)));
                candidates.Add(new Corridor(Direction.kEast, new Vector2Int(right, y)));
            }

            return candidates;
        }

        /*----------------------------------------------------------------------
        | --- AddCorridor: Adds a corridor to the room's list of corridors --- |
        ----------------------------------------------------------------------*/
        public void AddCorridor(Corridor corridor)
        {
            m_corridors.Add(corridor);
        }
    }
}