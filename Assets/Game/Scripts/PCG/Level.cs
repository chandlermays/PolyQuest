using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public class Level
    {
        private readonly List<Room> m_rooms;
        private readonly List<Corridor> m_corridors;

        private readonly TileType[,] m_grid;
        private readonly int m_width = 64;
        private readonly int m_length = 64;

        public IReadOnlyList<Room> Rooms => m_rooms;
        public IReadOnlyList<Corridor> Corridors => m_corridors;

        public Room StartRoom { get; set; }

        public int Width => m_width;
        public int Length => m_length;

        /*--------------------------------------------------------------------------------------
        | --- Level: Constructor that initializes the level with empty rooms and corridors --- |
        --------------------------------------------------------------------------------------*/
        public Level()
        {
            m_rooms = new List<Room>();
            m_corridors = new List<Corridor>();
            m_grid = new TileType[m_width, m_length];
            Fill(TileType.kEmpty);
        }

        /*-------------------------------------------
        | --- AddRoom: Adds a room to the level --- |
        -------------------------------------------*/
        public void AddRoom(Room room)
        {
            if (room != null)
            {
                m_rooms.Add(room);
            }
        }

        /*---------------------------------------------------
        | --- AddCorridor: Adds a corridor to the level --- |
        ---------------------------------------------------*/
        public void AddCorridor(Corridor corridor)
        {
            if (corridor != null)
            {
                m_corridors.Add(corridor);
            }
        }

        public bool InBounds(int x, int y) => x >= 0 && x < m_width && y >= 0 && y < m_length;

        public bool RemoveRoom(Room room) => room != null && m_rooms.Remove(room);
        public bool RemoveCorridor(Corridor corridor) => corridor != null && m_corridors.Remove(corridor);
        public void ClearRooms() => m_rooms.Clear();
        public void ClearCorridors() => m_corridors.Clear();

        public bool ContainsRoom(Room room) => room != null && m_rooms.Contains(room);
        public bool ContainsCorridor(Corridor corridor) => corridor != null && m_corridors.Contains(corridor);

        /*---------------------------------------------------------------------------------------------------
        | --- TryGetRoomAtPosition: Attempts to retrieve the room at the specified position for testing --- |
        ---------------------------------------------------------------------------------------------------*/
        public bool TryGetRoomAtPosition(Vector2Int position, out Room foundRoom)
        {
            for (int i = 0; i < m_rooms.Count; i++)
            {
                Room room = m_rooms[i];
                if (room.Area.Contains(position))
                {
                    foundRoom = room;
                    return true;
                }
            }
            foundRoom = null;
            return false;
        }

        /*-----------------------------------------------------------------------------------
        | --- GetIntersectingRooms: Retrieves all rooms intersecting the specified area --- |
        -----------------------------------------------------------------------------------*/
        public List<Room> GetIntersectingRooms(RectInt area)
        {
            List<Room> result = new();
            for (int i = 0; i < m_rooms.Count; i++)
            {
                Room room = m_rooms[i];
                if (Intersects(room.Area, area))
                {
                    result.Add(room);
                }
            }
            return result;
        }

        /*---------------------------------------------------------------------------------
        | --- FindConnectingCorridors: Finds all corridors connecting roomA and roomB --- |
        ---------------------------------------------------------------------------------*/
        public List<Corridor> FindConnectingCorridors(Room roomA, Room roomB)
        {
            List<Corridor> result = new();
            if (roomA == null || roomB == null)
                return result;

            for (int i = 0; i < m_corridors.Count; i++)
            {
                Corridor corridor = m_corridors[i];
                if ((corridor.StartRoom == roomA && corridor.EndRoom == roomB) ||
                    (corridor.StartRoom == roomB && corridor.EndRoom == roomA))
                {
                    result.Add(corridor);
                }
            }
            return result;
        }

        /*-----------------------------------------------------------
        | --- Intersects: Checks if two RectInt areas intersect --- |
        -----------------------------------------------------------*/
        private static bool Intersects(RectInt a, RectInt b)
        {
            return a.xMin < b.xMax && a.xMax > b.xMin &&
                   a.yMin < b.yMax && a.yMax > b.yMin;
        }

        /*----------------------------------------------------------------------------------
        | --- GetTileType: Retrieves the tile type at the specified (x, y) coordinates --- |
        ----------------------------------------------------------------------------------*/
        public TileType GetTileType(int x, int y)
        {
            if (!InBounds(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds for grid of size ({m_width}, {m_length}).");

            return m_grid[x, y];
        }

        /*-----------------------------------------------------------------------------
        | --- TryGetTileType: Attempts to get the tile type at (x, y) for testing --- |
        -----------------------------------------------------------------------------*/
        public bool TryGetTileType(int x, int y, out TileType type)
        {
            if (InBounds(x, y))
            {
                type = m_grid[x, y];
                return true;
            }

            type = default;
            return false;
        }

        /*-----------------------------------------------------------------------------
        | --- SetTileType: Sets the tile type at the specified (x, y) coordinates --- |
        -----------------------------------------------------------------------------*/
        public void SetTileType(int x, int y, TileType type)
        {
            if (!InBounds(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds for grid of size ({m_width}, {m_length}).");

            m_grid[x, y] = type;
        }

        /*------------------------------------------------------------------
        | --- Fill: Fills the entire grid with the specified tile type --- |
        ------------------------------------------------------------------*/
        public void Fill(TileType type)
        {
            for (int x = 0; x < m_width; x++)
            {
                for (int y = 0; y < m_length; y++)
                {
                    m_grid[x, y] = type;
                }
            }
        }

        /*-----------------------------------------------------------------------
        | --- DrawRect: Draws a rectangle area with the specified tile type --- |
        -----------------------------------------------------------------------*/
        public void DrawRect(RectInt area, TileType type)
        {
            int xMin = Mathf.Max(0, area.xMin);
            int xMax = Mathf.Min(m_width, area.xMax);
            int yMin = Mathf.Max(0, area.yMin);
            int yMax = Mathf.Min(m_length, area.yMax);

            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    m_grid[x, y] = type;
                }
            }
        }

        /*-------------------------------------------------------------------------------
        | --- DrawLine: Draws a line between two points using Bresenham's algorithm --- |
        -------------------------------------------------------------------------------*/
        public void DrawLine(Vector2Int a, Vector2Int b, TileType type)
        {
            int dx = Mathf.Abs(b.x - a.x);
            int dy = Mathf.Abs(b.y - a.y);
            int sx = (a.x < b.x) ? 1 : -1;
            int sy = (a.y < b.y) ? 1 : -1;
            int err = dx - dy;

            Vector2Int current = a;

            while (true)
            {
                if (InBounds(current.x, current.y))
                {
                    m_grid[current.x, current.y] = type;
                }

                if (current.x == b.x && current.y == b.y)
                    break;

                int e2 = 2 * err;

                if (e2 > -dy)
                {
                    err -= dy;
                    current.x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    current.y += sy;
                }
            }
        }

        /*--------------------------------------------------------------------------------
        | --- ToArray: Creates and returns a copy of the internal grid as a 2D array --- |
        --------------------------------------------------------------------------------*/
        public TileType[,] ToArray()
        {
            TileType[,] copy = new TileType[m_width, m_length];
            Array.Copy(m_grid, copy, m_grid.Length);
            return copy;
        }

        /*----------------------------------------------------------------------------------------------
        | --- ToTexture: Converts the grid to a Texture2D using the provided tile-to-color mapping --- |
        ----------------------------------------------------------------------------------------------*/
        public Texture2D ToTexture(Func<TileType, Color> tileToColor)
        {
            if (tileToColor == null)
                throw new ArgumentNullException(nameof(tileToColor));

            Texture2D texture = new(m_width, m_length)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int x = 0; x < m_width; x++)
            {
                for (int y = 0; y < m_length; y++)
                {
                    Color color = tileToColor(m_grid[x, y]);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}