using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    [CreateAssetMenu(fileName = "New Dungeon Tileset", menuName = "PolyQuest/PCG/Dungeon Tileset", order = 0)]
    public class DungeonTileset : ScriptableObject
    {
        private const int kTileCount = 16;
        [SerializeField] private GameObject[] m_tiles = new GameObject[kTileCount];

        /*-------------------------------------------------------------------------
        | --- GetTile: Retrieves the tile GameObject for the given tile index --- |
        -------------------------------------------------------------------------*/
        public GameObject GetTile(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= m_tiles.Length)
                return null;

            return m_tiles[tileIndex];
        }

#if UNITY_EDITOR
        /*-----------------------------------------------------------------------------
        | --- OnValidate: Ensures m_tiles array is properly initialized and sized --- |
        -----------------------------------------------------------------------------*/
        private void OnValidate()
        {
            if (m_tiles == null)
            {
                m_tiles = new GameObject[kTileCount];
                Debug.LogWarning($"{nameof(DungeonTileset)}: m_tiles was null and has been initialized to length {kTileCount}.", this);
                return;
            }

            if (m_tiles.Length != kTileCount)
            {
                GameObject[] old = m_tiles;
                m_tiles = new GameObject[kTileCount];

                // Preserve as many existing entries as possible
                int copy = Mathf.Min(old.Length, kTileCount);
                for (int i = 0; i < copy; i++)
                    m_tiles[i] = old[i];

                Debug.LogWarning($"{nameof(DungeonTileset)}: m_tiles length must be {kTileCount}. It was {old.Length} and has been adjusted.", this);
            }
        }
#endif
    }
}