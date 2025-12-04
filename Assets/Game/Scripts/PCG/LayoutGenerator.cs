using System.Collections.Generic;
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
            kDoor
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
        private RoomGraph m_roomGraph;

        /*--------------------------------------------------------------------------------------------
        | --- GenerateLevelLayout: Generates a new level layout using the blind digger algorithm --- |
        --------------------------------------------------------------------------------------------*/
        [ContextMenu("Generate Level Layout")]
        public void GenerateLevelLayout()
        {
            m_rng = new XorShift128Plus((ulong)System.DateTime.Now.Ticks);
            m_tiles = new TileType[m_width, m_length];
            m_roomGraph = new RoomGraph();

            for (int x = 0; x < m_width; ++x)
            {
                for (int y = 0; y < m_length; ++y)
                {
                    m_tiles[x, y] = TileType.kWall;
                }
            }

            BlindDigger();
            PlaceDoors();
            GenerateRoomGraph();
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

        /*--------------------------------------------------------------------------
        | --- PlaceDoors: Places doors between the rooms of the dungeon layout --- |
        --------------------------------------------------------------------------*/
        private void PlaceDoors()
        {
            for (int x = 1; x < m_width - 1; ++x)
            {
                for (int y = 1; y < m_length - 1; ++y)
                {
                    Vector2Int position = new Vector2Int(x, y);

                    // Skip if current tile is not a tunnel
                    if (m_tiles[x, y] != TileType.kTunnel)
                        continue;

                    if (ShouldPlaceDoor(position))
                    {
                        SetTile(position, TileType.kDoor);
                    }
                }
            }
        }

        /*--------------------------------------------------------------------------------------------
        | --- GenerateRoomGraph: Generates a graph representation of rooms and their connections --- |
        --------------------------------------------------------------------------------------------*/
        private void GenerateRoomGraph()
        {
            Vector2Int startTile = new Vector2Int(m_width / 2, m_length / 2);

            HashSet<Vector2Int> discovered = new HashSet<Vector2Int>();
            Queue<Vector2Int> roomOpenSet = new Queue<Vector2Int>();
            Queue<Vector2Int> tileOpenSet = new Queue<Vector2Int>();

            int prevRoomIndex = -1;
            roomOpenSet.Enqueue(startTile);

            while (roomOpenSet.Count > 0)
            {
                Vector2Int startTilePos = roomOpenSet.Dequeue();

                // If the tile has already been discovered, link the rooms
                if (discovered.Contains(startTilePos))
                {
                    int roomIndex = m_roomGraph.FindRoomIndexByTile(startTilePos);

                    if (roomIndex != -1 && roomIndex != prevRoomIndex)
                    {
                        m_roomGraph.LinkNode(prevRoomIndex, roomIndex);
                    }
                    continue;
                }

                // Generate an empty graph node
                int currentRoomIndex = m_roomGraph.AddEmptyNode();
                m_roomGraph.AddTileToRoom(startTilePos, currentRoomIndex);

                if (prevRoomIndex != -1 && currentRoomIndex != -1)
                {
                    m_roomGraph.LinkNode(prevRoomIndex, currentRoomIndex);
                }

                // Update BFS data with current tile
                tileOpenSet.Enqueue(startTilePos);
                discovered.Add(startTilePos);

                // Tile BFS - floodfill
                while (tileOpenSet.Count > 0)
                {
                    Vector2Int tilePos = tileOpenSet.Dequeue();

                    // If this is a door, find the undiscovered adjacent tile
                    if (GetTile(tilePos) == TileType.kDoor)
                    {
                        foreach (var direction in m_directions)
                        {
                            Vector2Int adjacentPos = tilePos + direction;

                            if (IsInBounds(adjacentPos))
                            {
                                TileType adjacentTile = GetTile(adjacentPos);
                                if (!discovered.Contains(adjacentPos) &&
                                    (adjacentTile == TileType.kTunnel || adjacentTile == TileType.kRoom || adjacentTile == TileType.kDoor))
                                {
                                    roomOpenSet.Enqueue(adjacentPos);
                                    break;
                                }
                            }
                        }
                        continue; // Done processing this door tile
                    }

                    // Not a door, so add this tile to the room
                    m_roomGraph.AddTileToRoom(tilePos, currentRoomIndex);

                    // Check neighbors
                    foreach (var direction in m_directions)
                    {
                        Vector2Int neighborPos = tilePos + direction;

                        if (IsInBounds(neighborPos) && !discovered.Contains(neighborPos))
                        {
                            TileType neighborTile = GetTile(neighborPos);

                            // Include doors in the tileOpenSet so we can access its adjacency for roomOpenSet
                            if (neighborTile == TileType.kTunnel || neighborTile == TileType.kRoom || neighborTile == TileType.kDoor)
                            {
                                discovered.Add(neighborPos);
                                tileOpenSet.Enqueue(neighborPos);
                            }
                        }
                    }
                }

                // Set the previous room index so we can link rooms
                prevRoomIndex = currentRoomIndex;
            }
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
                        TileType.kDoor => Color.brown,
                        _ => Color.black
                    };

                    layoutTexture.SetPixel(x, y, color);
                }
            }

            layoutTexture.Apply();
            layoutTexture.SaveAsset();
        }

        /*---------------------------------------------------------------------------------------
        | --- RenderRoomVisualization: Renders color-coded room visualization for debugging --- |
        ---------------------------------------------------------------------------------------*/
        [ContextMenu("Visualize Room Graph")]
        public void RenderRoomVisualization()
        {
            if (m_roomGraph == null || m_roomGraph.GetRoomCount() == 0)
            {
                Debug.LogWarning("No room graph available. Generate a layout first.");
                return;
            }

            if (!m_levelDisplay.TryGetComponent<Renderer>(out var renderer))
                return;

            var layoutTexture = (Texture2D)renderer.sharedMaterial.mainTexture;
            if (layoutTexture == null)
                return;

            // Predefined colors for room visualization (up to 16 rooms)
            Color[] roomColors = new Color[]
            {
                new Color(1f, 0f, 0f),          // Red
                new Color(0f, 1f, 0f),          // Green
                new Color(0f, 0f, 1f),          // Blue
                new Color(1f, 1f, 0f),          // Yellow
                new Color(1f, 0f, 1f),          // Magenta
                new Color(0f, 1f, 1f),          // Cyan
                new Color(1f, 0.5f, 0f),        // Orange
                new Color(0.5f, 0f, 1f),        // Purple
                new Color(1f, 0.5f, 0.5f),      // Light Red
                new Color(0.5f, 1f, 0.5f),      // Light Green
                new Color(0.5f, 0.5f, 1f),      // Light Blue
                new Color(1f, 1f, 0.5f),        // Light Yellow
                new Color(1f, 0.5f, 1f),        // Light Magenta
                new Color(0.5f, 1f, 1f),        // Light Cyan
                new Color(0.75f, 0.75f, 0.75f), // Silver
                new Color(0.5f, 0.5f, 0.5f)     // Gray
            };

            // Start with the base layout
            for (int x = 0; x < m_width; ++x)
            {
                for (int y = 0; y < m_length; ++y)
                {
                    Color color = m_tiles[x, y] switch
                    {
                        TileType.kWall => Color.black,
                        TileType.kTunnel => Color.gray,
                        TileType.kRoom => Color.white,
                        TileType.kDoor => Color.brown,
                        _ => Color.black
                    };

                    layoutTexture.SetPixel(x, y, color);
                }
            }

            // Overlay room colors
            int roomCount = m_roomGraph.GetRoomCount();
            for (int roomIndex = 0; roomIndex < roomCount; ++roomIndex)
            {
                var roomTiles = m_roomGraph.GetRoomTiles(roomIndex);
                Color roomColor = roomColors[roomIndex % roomColors.Length];

                foreach (var tilePos in roomTiles)
                {
                    // Only colorize if it's a walkable tile
                    if (m_tiles[tilePos.x, tilePos.y] == TileType.kTunnel || m_tiles[tilePos.x, tilePos.y] == TileType.kRoom)
                    {
                        layoutTexture.SetPixel(tilePos.x, tilePos.y, roomColor);
                    }
                }
            }

            layoutTexture.Apply();
            layoutTexture.SaveAsset();

            Debug.Log($"Room visualization complete. Found {roomCount} rooms in the graph.");
        }

        /*--------------------------------------------------------------------------------------
        | --- ShouldPlaceDoor: Determines if a door should be placed at the given position --- |
        --------------------------------------------------------------------------------------*/
        private bool ShouldPlaceDoor(Vector2Int position)
        {
            TileType north = GetTile(position + Vector2Int.up);
            TileType south = GetTile(position + Vector2Int.down);
            TileType east = GetTile(position + Vector2Int.right);
            TileType west = GetTile(position + Vector2Int.left);
            TileType northeast = GetTile(position + new Vector2Int(1, 1));
            TileType northwest = GetTile(position + new Vector2Int(-1, 1));
            TileType southeast = GetTile(position + new Vector2Int(1, -1));
            TileType southwest = GetTile(position + new Vector2Int(-1, -1));

            bool IsTunnel(TileType tile) => tile == TileType.kTunnel || tile == TileType.kRoom;
            bool IsWall(TileType tile) => tile == TileType.kWall;

            // Pattern 1: Horizontal door (north side)
            if (IsTunnel(north) && IsWall(east) && IsWall(west) && IsTunnel(northwest) && IsTunnel(northeast))
                return true;

            // Pattern 2: Horizontal door (south side)
            if (IsTunnel(south) && IsWall(east) && IsWall(west) && IsTunnel(southwest) && IsTunnel(southeast))
                return true;

            // Pattern 3: Vertical door (east side)
            if (IsTunnel(east) && IsWall(north) && IsWall(south) && IsTunnel(northeast) && IsTunnel(southeast))
                return true;

            // Pattern 4: Vertical door (west side)
            if (IsTunnel(west) && IsWall(north) && IsWall(south) && IsTunnel(northwest) && IsTunnel(southwest))
                return true;

            return false;
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

        /*-------------------------------------------------------------
        | --- GetTile: Returns the TileType at the given position --- |
        -------------------------------------------------------------*/
        private TileType GetTile(Vector2Int position)
        {
            if (IsInBounds(position))
            {
                return m_tiles[position.x, position.y];
            }
            return TileType.kWall; // Out of bounds, treat as wall
        }

        
    }
}