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
        kNormal,
        kUndefined
    }

    public class Room
    {
        private RectInt m_area;
        private List<Corridor> m_corridors;

        public RectInt Area => m_area;
        public RoomType Type { get; set; } = RoomType.kUndefined;

        public Room(RectInt area)
        {
            m_area = area;
            m_corridors = new();
        }
    }
}