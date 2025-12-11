using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public enum TileType
    {
        kWall,
        kFloor,
        kFloorProp,
        kWallProp,
        kAnyWall,
        kAnyFloor,
        kEmpty,
        //...
    }

    public static class TileTypeExt
    {
        private static readonly Color[] s_colors =
        {
            Color.black,        // kWall
            Color.white,        // kFloor
            Color.blue,         // kFloorProp
            Color.green,        // kWallProp
            Color.yellow,       // kAnyWall
            Color.cyan,         // kAnyFloor
            Color.magenta,      // kEmpty
        };

        public static Color GetColor(this TileType tileType)
        {
            int idx = (int)tileType;
            if (idx < 0 || idx >= s_colors.Length)
            {
                return Color.magenta;
            }
            return s_colors[idx];
        }
    }
}