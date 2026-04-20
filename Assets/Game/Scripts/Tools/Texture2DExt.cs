/*---------------------------
File: Texture2DExt.cs
Author: Chandler Mays
----------------------------*/
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
//---------------------------------

namespace PolyQuest
{
    public static class Texture2DExt
    {
        /*--------------------------------------------------------------------------
        | --- FillWithColor: Fills the entire texture with the specified color --- |
        --------------------------------------------------------------------------*/
        public static void FillWithColor(this Texture2D texture, Color color)
        {
            texture.DrawRectangle(new RectInt(0, 0, texture.width, texture.height), color);
        }

        /*-------------------------------------------------------------------------
        | --- ConvertToBlackAndWhite: Converts the texture to black and white --- |
        -------------------------------------------------------------------------*/
        public static void ConvertToBlackAndWhite(this Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; ++i)
            {
                if (pixels[i] != Color.black)
                {
                    pixels[i] = Color.white;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        /*----------------------------------------------------------------
        | --- DrawRectangle: Draws a filled rectangle on the texture --- |
        ----------------------------------------------------------------*/
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

        /*----------------------------------------------------------------------------------------------
        | --- DrawLine: Draws a line between two points on the texture using Bresenham's algorithm --- |
        ----------------------------------------------------------------------------------------------*/
        public static void DrawLine(this Texture2D texture, Vector2Int p0, Vector2Int p1, Color color)
        {
            int dx = Mathf.Abs(p1.x - p0.x);
            int dy = Mathf.Abs(p1.y - p0.y);
            int sx = (p0.x < p1.x) ? 1 : -1;
            int sy = (p0.y < p1.y) ? 1 : -1;
            int err = dx - dy;

            while (p0 != p1)
            {
                texture.SetPixel(p0.x, p0.y, color);

                int e2 = 2 * err;

                if (e2 > -dy && e2 < dx)
                {
                    texture.SetPixel(p0.x + sx, p0.y, color);
                }

                if (e2 > -dy)
                {
                    err -= dy;
                    p0.x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    p0.y += sy;
                }
            }

            texture.SetPixel(p0.x, p0.y, color);
            texture.Apply();
        }

        /*-----------------------------------------------------------------------------------------------------
        | --- DrawTexture: Draws a source texture onto the destination texture at the specified rectangle --- |
        -----------------------------------------------------------------------------------------------------*/
        public static void DrawTexture(this Texture2D texture, Texture2D sourceTexture, RectInt destinationRect)
        {
            int startX = Mathf.Max(0, destinationRect.x);
            int startY = Mathf.Max(0, destinationRect.y);
            int endX = Mathf.Min(destinationRect.x + destinationRect.width, texture.width);
            int endY = Mathf.Min(destinationRect.y + destinationRect.height, texture.height);

            for (int y = startY; y < endY; ++y)
            {
                for (int x = startX; x < endX; ++x)
                {
                    Color sourceColor = sourceTexture.GetPixel(x - destinationRect.x, y - destinationRect.y);
                    texture.SetPixel(x, y, sourceColor);
                }
            }

            texture.Apply();
        }

        /*------------------------------------------------------------------
        | --- SaveAsset: Saves the texture asset to disk as a PNG file --- |
        ------------------------------------------------------------------*/
        public static void SaveAsset(this Texture2D texture)
        {
#if UNITY_EDITOR
            byte[] bytes = texture.EncodeToPNG();
            string assetPath = AssetDatabase.GetAssetPath(texture);
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.Refresh();
#endif
        }
    }
}