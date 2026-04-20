/*---------------------------
File: RoomDecorator.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public class RoomDecorator : MonoBehaviour
    {
        [SerializeField] private Texture2D m_levelTexture;
        [SerializeField] private Texture2D m_decorationTexture;
        [SerializeField] private GameObject m_levelGeometry;
        [SerializeField] private LayoutGenerator m_layoutGenerator;

        [Tooltip("Room patterns define which decorations apply to which room types. " +
                 "Each pattern should be a 'Room Pattern' asset that specifies its target room types.")]
        [SerializeField] private RoomPattern[] m_roomPatterns;

        private const string kDecorationsParentName = "Decorations";

        /*-------------------------------------------------------------------------
        | --- GenerateDecorations: Generates decorations for the level layout --- |
        -------------------------------------------------------------------------*/
        public void GenerateDecorations()
        {
            Level level = m_layoutGenerator.GenerateLayout();
            Initialize(level, m_layoutGenerator.Seed);
        }

        /*-------------------------------------------------------------------------
        | --- Initialize: Initializes the room decorator with the given level --- |
        -------------------------------------------------------------------------*/
        public void Initialize(Level level, long seed)
        {
            Transform decorationsTransform = InitializeDecorations();

            TileType[,] decoratedLevel = GenerateDecoratedLevel(level);
            foreach (Room room in level.Rooms)
            {
                DecorateRoom(decoratedLevel, room, decorationsTransform, seed);
            }

            UpdateDecorationTexture(decoratedLevel);
        }

        /*-----------------------------------------------------------------------------
        | --- InitializeDecorations: Initializes the decorations parent transform --- |
        -----------------------------------------------------------------------------*/
        private Transform InitializeDecorations()
        {
            Transform decorationsTransform = m_levelGeometry.transform.Find(kDecorationsParentName);
            if (decorationsTransform == null)
            {
                GameObject decorations = new(kDecorationsParentName);
                decorationsTransform = decorations.transform;
                decorationsTransform.SetParent(m_levelGeometry.transform);
            }
            else
            {
                DestroyAllChildren(decorationsTransform);
            }

            return decorationsTransform;
        }

        /*---------------------------------------------------------------------
        | --- DecorateRoom: Decorates a room based on the decorated level --- |
        ---------------------------------------------------------------------*/
        private void DecorateRoom(TileType[,] decoratedLevel, Room room, Transform decorationsTransform, long seed)
        {
            RoomPattern roomPattern = GetMatchingRoomPattern(room);
            if (roomPattern == null)
                return;

            if (roomPattern.CanBeApplied(decoratedLevel, room))
            {
                roomPattern.Apply(decoratedLevel, room, decorationsTransform, seed);
            }
        }

        /*---------------------------------------------------------------------------------------
        | --- GetMatchingRoomPattern: Returns the first room pattern matching the room type --- |
        ---------------------------------------------------------------------------------------*/
        private RoomPattern GetMatchingRoomPattern(Room room)
        {
            foreach (RoomPattern pattern in m_roomPatterns)
            {
                if (pattern.MatchesRoomType(room))
                {
                    return pattern;
                }
            }
            return null;
        }

        /*---------------------------------------------------------------------------------------------
        | --- GenerateDecoratedLevel: Generates a decorated level based on the decoration texture --- |
        ---------------------------------------------------------------------------------------------*/
        private TileType[,] GenerateDecoratedLevel(Level level)
        {
            TextureBasedLevel tbLevel = new(m_levelTexture);
            TileType[,] decoratedLevel = new TileType[level.Width, level.Length];

            int wallCount = 0;
            int floorCount = 0;

            for (int y = 0; y < level.Length; ++y)
            {
                for (int x = 0; x < level.Width; ++x)
                {
                    TileType tileType = tbLevel.IsBlocked(x, y) ? TileType.kWall : TileType.kFloor;
                    decoratedLevel[x, y] = tileType;

                    if (tileType == TileType.kWall)
                    {
                        ++wallCount;
                    }
                    else if (tileType == TileType.kFloor)
                    {
                        ++floorCount;
                    }
                }
            }

            return decoratedLevel;
        }

        /*----------------------------------------------------------------------------------------------
        | --- UpdateDecorationTexture: Updates the decoration texture based on the decorated level --- |
        ----------------------------------------------------------------------------------------------*/
        private void UpdateDecorationTexture(TileType[,] tileTypes)
        {
            int width = tileTypes.GetLength(0);
            int length = tileTypes.GetLength(1);

            Color32[] pixels = new Color32[width * length];

            for (int y = 0; y < length; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    pixels[x + y * width] = tileTypes[x, y].GetColor();
                }
            }

            m_decorationTexture.Reinitialize(width, length);
            m_decorationTexture.SetPixels32(pixels);
            m_decorationTexture.Apply();
            m_decorationTexture.SaveAsset();
        }

        /*------------------------------------------------------------------------------------------
        | --- DestroyAllChildren: Destroys all child GameObjects of the given parent Transform --- |
        ------------------------------------------------------------------------------------------*/
        private void DestroyAllChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; --i)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
    }
}