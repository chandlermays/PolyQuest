using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public class MarchingSquares : MonoBehaviour
    {
        [SerializeField] private Texture2D m_levelTexture;
        [SerializeField] private GameObject m_levelGeometry;
        [SerializeField] private DungeonTileset m_tileset;
        [SerializeField] private float m_scale = 2f;

        public float Scale => m_scale;

        /*-----------------------------------------------------------------------------------
        | --- CreateLevelGeometry: Creates level geometry based on the provided texture --- |
        -----------------------------------------------------------------------------------*/
        public void CreateLevelGeometry()
        {
            DestroyAllChildren(m_levelGeometry.transform);

            Vector3 scaleVector = new(m_scale, m_scale, m_scale);
            TextureBasedLevel tbLevel = new(m_levelTexture);

            for (int y = 0; y < tbLevel.Length - 1; ++y)
            {
                for (int x = 0; x < tbLevel.Width - 1; ++x)
                {
                    int tileIndex = CalculateTileIndex(tbLevel, x, y);
                    GameObject tilePrefab = m_tileset.GetTile(tileIndex);
                    if (tilePrefab == null)
                        continue;

                    GameObject tile = Instantiate(tilePrefab, m_levelGeometry.transform);
                    tile.transform.localScale = scaleVector;
                    tile.transform.position = new Vector3(x * m_scale, 0, y * m_scale);
                    string name = $"Tile_{x}_{y}_Index_{tileIndex}";
                    tile.name = name;
                }
            }
        }

        /*------------------------------------------------------------------------------------------------------------------
        | --- CalculateTileIndex: Calculates the tile index for the given cell based on the marching squares algorithm --- |
        ------------------------------------------------------------------------------------------------------------------*/
        private int CalculateTileIndex(ILevel level, int x, int y)
        {
            int topLeft = level.IsBlocked(x, y + 1) ? 1 : 0;
            int topRight = level.IsBlocked(x + 1, y + 1) ? 1 : 0;
            int bottomLeft = level.IsBlocked(x, y) ? 1 : 0;
            int bottomRight = level.IsBlocked(x + 1, y) ? 1 : 0;
            int tileIndex = topLeft + (topRight * 2) + (bottomLeft * 4) + (bottomRight * 8);
            return tileIndex;
        }

        /*------------------------------------------------------------------------------------------
        | --- DestroyAllChildren: Destroys all child GameObjects of the given parent Transform --- |
        ------------------------------------------------------------------------------------------*/
        private void DestroyAllChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; --i)
            {
#if UNITY_EDITOR
                DestroyImmediate(parent.GetChild(i).gameObject);
#else
                Destroy(parent.GetChild(i).gameObject);
#endif
            }
        }
    }
}