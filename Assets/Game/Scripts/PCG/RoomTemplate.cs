using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    [CreateAssetMenu(fileName = "New Room Template", menuName = "PolyQuest/PCG/Room Template", order = 0)]
    public class RoomTemplate : ScriptableObject
    {
        [SerializeField] private int m_minRoomWidth = 3;
        [SerializeField] private int m_maxRoomWidth = 5;
        [SerializeField] private int m_minRoomLength = 3;
        [SerializeField] private int m_maxRoomLength = 5;

        public int MinRoomWidth => m_minRoomWidth;
        public int MaxRoomWidth => m_maxRoomWidth;
        public int MinRoomLength => m_minRoomLength;
        public int MaxRoomLength => m_maxRoomLength;

        /*--------------------------------------------------------------------------------------
        | --- GenerateRoomCandidate: Generates a random room candidate within the template --- |
        --------------------------------------------------------------------------------------*/
        public RectInt GenerateRoomCandidate(XorShift128Plus rng)
        {
            RectInt roomCandidate = new()
            {
                width = rng.RandomRange(m_minRoomWidth, m_maxRoomWidth),
                height = rng.RandomRange(m_minRoomLength, m_maxRoomLength)
            };
            return roomCandidate;
        }
    }
}