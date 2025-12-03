using System.Collections.Generic;

namespace PolyQuest.PCG
{
    public class Level
    {
        private int m_width;
        private int m_length;
        List<Room> m_rooms;
        List<Corridor> m_corridors;

        public int Width => m_width;
        public int Length => m_length;

        public Level(int width, int length)
        {
            m_width = width;
            m_length = length;
            m_rooms = new List<Room>();
            m_corridors = new List<Corridor>();
        }
    }
}