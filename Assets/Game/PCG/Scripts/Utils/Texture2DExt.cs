using UnityEditor;
using UnityEngine;
using System.IO;

namespace PolyQuest
{
    public static class Texture2DExt
    {
        public static void FillWithColor(this Texture2D texture, Color color)
        {
            texture.DrawRectangle(new RectInt(0, 0, texture.width, texture.height), color);
        }

        public static void DrawRectangle(this Texture2D texture, RectInt rect, Color color)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
        }

        public static void SaveAsset(this Texture2D texture)
        {
            byte[] bytes = texture.EncodeToPNG();
            string assetPath = AssetDatabase.GetAssetPath(texture);
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.Refresh();
        }
    }
}