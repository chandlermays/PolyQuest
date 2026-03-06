using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Pattern Matching", menuName = "PolyQuest/PCG/Decorators/Pattern Matching", order = 0)]
    public class PatternMatching : DecoratorPattern
    {
        [SerializeField] private GameObject m_prefab;
        [SerializeField] private float m_prefabRotation;
        [SerializeField] private Array2D<TileType> m_targetPattern;
        [SerializeField] private Array2D<TileType> m_resultPattern;

        /*------------------------------------------------------------------------------
        | --- CanBeApplied: Checks if the pattern can be applied to the given room --- |
        ------------------------------------------------------------------------------*/
        public override bool CanBeApplied(TileType[,] decoratedLevel, Room room)
        {
            if (FindMatchingPositions(decoratedLevel, room).Length > 0)
                return true;

            return false;
        }

        /*-----------------------------------------------------------------------------
        | --- Apply: Applies the pattern to the given room in the decorated level --- |
        -----------------------------------------------------------------------------*/
        public override void Apply(TileType[,] decoratedLevel, Room room, Transform parent, long seed)
        {
            XorShift128Plus rng = new(unchecked((ulong)seed));

            Vector2Int[] patterns = FindMatchingPositions(decoratedLevel, room);
            if (patterns.Length == 0)
                return;

            int patternIndex = rng.RandomRange(0, patterns.Length);
            Vector2Int pattern = patterns[patternIndex];

            for (int y = 0; y < m_targetPattern.Height; ++y)
            {
                for (int x = 0; x < m_targetPattern.Width; ++x)
                {
                    TileType tileType = m_resultPattern[x, y];
                    decoratedLevel[pattern.x + x, pattern.y + y] = tileType;
                }
            }

            GameObject decoration = Instantiate(m_prefab, parent.transform);
            decoration.transform.rotation = Quaternion.Euler(0, m_prefabRotation, 0);
            Vector3 center = new Vector3(pattern.x + m_targetPattern.Width / 2f, 0, pattern.y + m_targetPattern.Height / 2f);
            decoration.transform.position = (center + new Vector3(-1, 0, -1)) * 2;
            decoration.transform.localScale = Vector3.one * 2;
        }

        /*----------------------------------------------------------------------------------------------
        | --- FindMatchingPositions: Finds all positions in the room that match the target pattern --- |
        ----------------------------------------------------------------------------------------------*/
        private Vector2Int[] FindMatchingPositions(TileType[,] decoratedLevel, Room room)
        {
            List<Vector2Int> patterns = new();

            for (int y = room.Area.position.y - 1; y < room.Area.position.y + room.Area.height + 2 - m_targetPattern.Height; ++y)
            {
                for (int x = room.Area.position.x - 1; x < room.Area.position.x + room.Area.width + 2 - m_targetPattern.Width; ++x)
                {
                    if (IsPositionValid(decoratedLevel, m_targetPattern, x, y))
                    {
                        patterns.Add(new Vector2Int(x, y));
                    }
                }
            }
            return patterns.ToArray();
        }

        /*--------------------------------------------------------------------------------------------------
        | --- IsPositionValid: Checks if the pattern matches the decorated level at the given position --- |
        --------------------------------------------------------------------------------------------------*/
        private bool IsPositionValid(TileType[,] decoratedLevel, Array2D<TileType> pattern, int startX, int startY)
        {
            for (int y = 0; y < pattern.Height; ++y)
            {
                for (int x = 0; x < pattern.Width; ++x)
                {
                    TileType patternTile = pattern[x, y];
                    TileType levelTile = decoratedLevel[startX + x, startY + y];

                    if (!TilesMatch(patternTile, levelTile))
                        return false;
                }
            }
            return true;
        }

        /*-----------------------------------------------------------------------------------------
        | --- TilesMatch: Checks if a pattern tile matches a level tile considering wildcards --- |
        -----------------------------------------------------------------------------------------*/
        private bool TilesMatch(TileType patternTile, TileType levelTile)
        {
            if (patternTile == levelTile)
                return true;

            if (patternTile == TileType.kAnyWall &&
                (levelTile == TileType.kWall || levelTile == TileType.kWallProp))
                return true;

            if (patternTile == TileType.kAnyFloor &&
                (levelTile == TileType.kFloor || levelTile == TileType.kFloorProp))
                return true;

            return false;
        }
    }
}