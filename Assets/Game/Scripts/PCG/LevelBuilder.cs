using Newtonsoft.Json.Linq;
using Unity.AI.Navigation;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.PCG
{
    public class LevelBuilder : MonoBehaviour, ISaveable
    {
        [SerializeField] private LayoutGenerator m_layoutGenerator;
        [SerializeField] private MarchingSquares m_levelGeometry;
        [SerializeField] private RoomDecorator m_roomDecorator;
        [SerializeField] private NavMeshSurface m_navMeshSurface;

        private bool m_restoredThisLoad = false;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            if (!m_restoredThisLoad)
            {
                GenerateNewSeedAndLevel();
            }
        }

        /*------------------------------------------------------------------------
        | --- GenerateNewSeedAndLevel: Generates a new seed and level layout --- |
        ------------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Generate New Seed and Level")]
        public void GenerateNewSeedAndLevel()
        {
            m_layoutGenerator.GenerateNewSeed();
            BuildLevel();
        }

        /*---------------------------------------------------------------------
        | --- GenerateNewLevel: Rebuilds the level using the current seed --- |
        ---------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Generate New Level")]
        public void GenerateNewLevel()
        {
            BuildLevel();
        }

        /*------------------------------------------------------------------------
        | --- BuildLevel: Constructs geometry, navmesh, and room decorations --- |
        ------------------------------------------------------------------------*/
        private void BuildLevel()
        {
            Level level = m_layoutGenerator.GenerateLayout();
            m_levelGeometry.CreateLevelGeometry();
            m_navMeshSurface.BuildNavMesh();
            m_roomDecorator.Initialize(level);
        }

        /*--------------------------------------------------------------------
        | --- CaptureState: Saves the current seed so it can be restored --- |
        --------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            return new JValue(m_layoutGenerator.Seed);
        }

        /*---------------------------------------------------------------------------
        | --- RestoreState: Restores the saved seed and rebuilds level geometry --- |
        ---------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            m_layoutGenerator.Seed = state.ToObject<long>();
            BuildLevel();
            m_restoredThisLoad = true;
        }
    }
}