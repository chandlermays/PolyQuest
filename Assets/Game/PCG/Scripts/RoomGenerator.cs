using UnityEngine;

namespace PolyQuest.PCG
{
    public class RoomGenerator : MonoBehaviour
    {
        [SerializeField] private int m_roomLength = 64;
        [SerializeField] private int m_roomWidth = 64;

        [SerializeField] private int m_roomWidthMin = 6;
        [SerializeField] private int m_roomWidthMax = 8;
        [SerializeField] private int m_roomLengthMin = 6;
        [SerializeField] private int m_roomLengthMax = 8;

        [SerializeField] private GameObject m_display;

        private XorShift128Plus m_rng;

        [ContextMenu("Generate Level")]
        public void GenerateLevel()
        {
            m_rng = new XorShift128Plus();

            RectInt startRoom = GetStartRoomRect();
            DrawLayout(startRoom);
        }

        private RectInt GetStartRoomRect()
        {
            int roomWidth = m_rng.RandomRange(m_roomWidthMin, m_roomWidthMax);
            int availableWidthX = (m_roomWidth / 2) - roomWidth;
            int randomX = m_rng.RandomRange(0, availableWidthX);
            int roomX = randomX + (m_roomWidth / 4);

            int roomLength = m_rng.RandomRange(m_roomLengthMin, m_roomLengthMax);
            int availableLengthY = (m_roomLength / 2) - roomLength;
            int randomY = m_rng.RandomRange(0, availableLengthY);
            int roomY = randomY + (m_roomLength / 4);

            return new RectInt(roomX, roomY, roomWidth, roomLength);
        }

        private void DrawLayout(RectInt roomCandidate)
        {
            Renderer renderer = m_display.GetComponent<Renderer>();

            Texture2D layoutTexture = (Texture2D)renderer.sharedMaterial.mainTexture;
            layoutTexture.Reinitialize(m_roomWidth, m_roomLength);
            layoutTexture.FillWithColor(Color.black);
            layoutTexture.DrawRectangle(roomCandidate, Color.green);
            layoutTexture.SaveAsset();
        }
    }
}