using UnityEngine;

namespace PolyQuest.PCG
{
    public class RoomGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject m_levelDisplay;

        [SerializeField] private int m_width = 64;
        [SerializeField] private int m_length = 64;

        [SerializeField] private int m_minRoomWidth = 3;
        [SerializeField] private int m_maxRoomWidth = 5;
        [SerializeField] private int m_minRoomHeight = 3;
        [SerializeField] private int m_maxRoomHeight = 5;

        private XorShift128Plus m_rng;

        [ContextMenu("Generate Level")]
        public void GenerateLevel()
        {
            m_rng = new XorShift128Plus((ulong)System.DateTime.Now.Ticks);
            RectInt roomRect = GetStartRoomRect();
            DrawLayout(roomRect);
        }

        private RectInt GetStartRoomRect()
        {
            int roomWidth = m_rng.RandomRange(m_minRoomWidth, m_maxRoomWidth);
            int availableWidth = (m_width / 2) - roomWidth;
            int randomX = m_rng.RandomRange(0, availableWidth);
            int roomX = randomX + (m_width / 4);

            int roomLength = m_rng.RandomRange(m_minRoomHeight, m_maxRoomHeight);
            int availableLength = (m_length / 2) - roomLength;
            int randomY = m_rng.RandomRange(0, availableLength);
            int roomY = randomY + (m_length / 4);

            return new RectInt(roomX, roomY, roomWidth, roomLength);
        }

        private void DrawLayout(RectInt roomRect)
        {
            var renderer = m_levelDisplay.GetComponent<Renderer>();

            var layoutTexture = (Texture2D)renderer.sharedMaterial.mainTexture;

            layoutTexture.Reinitialize(m_width, m_length);
            m_levelDisplay.transform.localScale = new Vector3(m_width, m_length, 1);
            layoutTexture.FillWithColor(Color.black);
            layoutTexture.DrawRectangle(roomRect, Color.white);
            layoutTexture.SaveAsset();
        }
    }
}