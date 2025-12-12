using Unity.AI.Navigation;
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public class LevelBuilder : MonoBehaviour
    {
        [SerializeField] private LayoutGenerator m_layoutGenerator;
        [SerializeField] private MarchingSquares m_levelGeometry;
        [SerializeField] private RoomDecorator m_roomDecorator;
        [SerializeField] private NavMeshSurface m_navMeshSurface;
        [SerializeField] private GameObject m_player;

        /*-------------------------------------------------------------------------
        | --- Start: Generates a new seed and level at the start of the scene --- |
        -------------------------------------------------------------------------*/
        private void Awake()
        {
            GenerateNewSeedAndLevel();
        }

        /*------------------------------------------------------------------------
        | --- GenerateNewSeedAndLevel: Generates a new seed and level layout --- |
        ------------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Generate New Seed and Level")]
        public void GenerateNewSeedAndLevel()
        {
            m_layoutGenerator.GenerateNewSeed();
            GenerateNewLevel();
        }

        /*------------------------------------------------------------------------------
        | --- GenerateNewLevel: Generates a new level layout with the current seed --- |
        ------------------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Generate New Level")]
        public void GenerateNewLevel()
        {
            Level level = m_layoutGenerator.GenerateLayout();
            m_levelGeometry.CreateLevelGeometry();
            m_roomDecorator.Initialize(level);
            m_navMeshSurface.BuildNavMesh();
        }
    }
}