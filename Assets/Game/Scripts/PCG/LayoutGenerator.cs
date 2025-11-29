using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public class LayoutGenerator : MonoBehaviour
    {
        private enum TileType
        {
            kWall,
            kTunnel,
            kRoom,
        }

        private readonly Vector2Int[] m_directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        [SerializeField] private GameObject m_levelDisplay;

        [SerializeField] private int m_width = 64;
        [SerializeField] private int m_length = 64;
        [SerializeField] private int m_desiredRoomCount = 10;
        [SerializeField] private int m_minRoomSize = 3;
        [SerializeField] private int m_maxRoomSize = 7;

        [SerializeField] private int m_minTunnelBetweenRooms = 8;
        [SerializeField] private int m_roomSeparationMargin = 2;

        private XorShift128Plus m_rng;
        private TileType[,] m_tiles;

        /*--------------------------------------------------------------------------------------------
        | --- GenerateLevelLayout: Generates a new level layout using the blind digger algorithm --- |
        --------------------------------------------------------------------------------------------*/
        [ContextMenu("Generate Level Layout")]
        public void GenerateLevelLayout()
        {
            m_rng = new XorShift128Plus((ulong)System.DateTime.Now.Ticks);
            m_tiles = new TileType[m_width, m_length];

            for (int x = 0; x < m_width; ++x)
            {
                for (int y = 0; y < m_length; ++y)
                {
                    m_tiles[x, y] = TileType.kWall;
                }
            }

            BlindDigger();

            DisplayLayout();
        }

        /*----------------------------------------------------------------------------------------
        | --- BlindDigger: Implements the blind digger algorithm to create tunnels and rooms --- |
        ----------------------------------------------------------------------------------------*/
        private void BlindDigger()
        {
            Vector2Int diggerPos = new Vector2Int(m_width / 2, m_length / 2);
            Vector2Int currentDir = m_directions[m_rng.RandomRange(0, m_directions.Length)];

            int turnChance = 5;
            int roomChance = 5;
            int roomCount = 0;

            int stepsSinceLastRoom = 0;

            m_tiles[diggerPos.x, diggerPos.y] = TileType.kTunnel;

            while (!IsDone(roomCount))
            {
                diggerPos += currentDir;

                diggerPos.x = Mathf.Clamp(diggerPos.x, 1, m_width - 2);
                diggerPos.y = Mathf.Clamp(diggerPos.y, 1, m_length - 2);

                SetTile(diggerPos, TileType.kTunnel);
                ++stepsSinceLastRoom;

                int choice = m_rng.RandomRange(0, 100);
                if (choice < turnChance)
                {
                    Vector2Int newDir;
                    do
                    {
                        newDir = m_directions[m_rng.RandomRange(0, m_directions.Length)];
                    } while (newDir + currentDir == Vector2Int.zero);

                    currentDir = newDir;

                    turnChance = 0;
                }
                else
                {
                    turnChance += 5;
                }

                if (stepsSinceLastRoom >= m_minTunnelBetweenRooms)
                {
                    choice = m_rng.RandomRange(0, 100);
                    if (choice < roomChance)
                    {
                        int width = m_rng.RandomRange(m_minRoomSize, m_maxRoomSize + 1);
                        int height = m_rng.RandomRange(m_minRoomSize, m_maxRoomSize + 1);

                        if (PlaceRoomWithMargin(diggerPos, width, height, m_roomSeparationMargin))
                        {
                            ++roomCount;
                            stepsSinceLastRoom = 0;
                        }

                        roomChance = 0;
                    }
                    else
                    {
                        roomChance += 5;
                    }
                }
            }
        }

        /*------------------------------------------------------------------------
        | --- IsDone: Checks if the desired number of rooms has been created --- |
        ------------------------------------------------------------------------*/
        private bool IsDone(int roomCount)
        {
            return roomCount >= m_desiredRoomCount;
        }

        /*--------------------------------------------------------------------------------
        | --- SetTile: Sets the tile type at the specified position if within bounds --- |
        --------------------------------------------------------------------------------*/
        private void SetTile(Vector2Int pos, TileType type)
        {
            if (IsInBounds(pos))
            {
                m_tiles[pos.x, pos.y] = type;
            }
        }

        /*----------------------------------------------------------------------------------------------------------------
        | --- PlaceRoomWithMargin: Attempts to place a room at the specified center with given dimensions and margin --- |
        ----------------------------------------------------------------------------------------------------------------*/
        private bool PlaceRoomWithMargin(Vector2Int center, int width, int height, int margin)
        {
            int startX = center.x - width / 2;
            int startY = center.y - height / 2;
            int endX = startX + width - 1;
            int endY = startY + height - 1;

            if (startX < 1 || startY < 1 || endX >= m_width - 1 || endY >= m_length - 1)
            {
                return false;
            }

            int checkStartX = Mathf.Max(1, startX - margin);
            int checkStartY = Mathf.Max(1, startY - margin);
            int checkEndX = Mathf.Min(m_width - 2, endX + margin);
            int checkEndY = Mathf.Min(m_length - 2, endY + margin);

            for (int x = checkStartX; x <= checkEndX; ++x)
            {
                for (int y = checkStartY; y <= checkEndY; ++y)
                {
                    TileType t = m_tiles[x, y];
                    if (t == TileType.kRoom)
                    {
                        return false;
                    }
                }
            }

            for (int x = startX; x <= endX; ++x)
            {
                for (int y = startY; y <= endY; ++y)
                {
                    m_tiles[x, y] = TileType.kRoom;
                }
            }

            return true;
        }

        /*------------------------------------------------------------------------------
        | --- IsInBounds: Checks if the given position is within the layout bounds --- |
        ------------------------------------------------------------------------------*/
        private bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < m_width && pos.y >= 0 && pos.y < m_length;
        }

        /*----------------------------------------------------------------------------------
        | --- DisplayLayout: Renders the current layout onto the level display texture --- |
        ----------------------------------------------------------------------------------*/
        private void DisplayLayout()
        {
            if (!m_levelDisplay.TryGetComponent<Renderer>(out var renderer))
                return;

            var layoutTexture = (Texture2D)renderer.sharedMaterial.mainTexture;
            if (layoutTexture == null)
                return;

            layoutTexture.Reinitialize(m_width, m_length);
            m_levelDisplay.transform.localScale = new Vector3(m_width, m_length, 1);

            for (int x = 0; x < m_width; ++x)
            {
                for (int y = 0; y < m_length; ++y)
                {
                    Color color = m_tiles[x, y] switch
                    {
                        TileType.kWall => Color.black,
                        TileType.kTunnel => Color.white,
                        TileType.kRoom => Color.white,
                        _ => Color.black
                    };

                    layoutTexture.SetPixel(x, y, color);
                }
            }

            layoutTexture.Apply();
            layoutTexture.SaveAsset();
        }
    }
}