using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public class TextureBasedLevel : ILevel
    {
        private readonly Texture2D m_levelTexture;

        public TextureBasedLevel(Texture2D levelTexture)
        {
            m_levelTexture = levelTexture;
        }

        public int Width => m_levelTexture.width;
        public int Length => m_levelTexture.height;

        /*--------------------------------------------------------------------------
        | --- IsBlocked: Checks if the tile at (x, y) is blocked (black pixel) --- |
        --------------------------------------------------------------------------*/
        public bool IsBlocked(int x, int y)
        {
            if (x < 0 || y < 0 || x >= m_levelTexture.width || y >= m_levelTexture.height)
                return true;

            Color pixel = m_levelTexture.GetPixel(x, y);
            return Color.black.Equals(pixel);
        }
    }
}