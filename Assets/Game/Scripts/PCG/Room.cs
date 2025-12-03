using UnityEngine;

namespace PolyQuest.PCG
{
    public class Room
    {
        private RectInt m_area;
        public RectInt Area => m_area;

        public Room(RectInt area)
        {
            m_area = area;
        }
    }
}