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

        private const string kSeedKey = "Seed";

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
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
            GenerateLevel();
        }

        /*---------------------------------------------------------------------
        | --- GenerateNewLevel: Rebuilds the level using the current seed --- |
        ---------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Generate New Level")]
        public void GenerateNewLevel()
        {
            GenerateLevel();
        }

        /*---------------------------------------------------------------------------------------------
        | --- GenerateLevel: Generates the level geometry and navmesh based on the current layout --- |
        ---------------------------------------------------------------------------------------------*/
        private void GenerateLevel()
        {
            Level level = m_layoutGenerator.GenerateLayout();
            m_levelGeometry.CreateLevelGeometry();
            m_navMeshSurface.BuildNavMesh();
            m_roomDecorator.Initialize(level, m_layoutGenerator.Seed);
        }

        /*-----------------------------------------------------------------------------------
        | --- CptureState: Captures the current seed of the layout generator for saving --- |
        -----------------------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            JObject state = new()
            {
                [kSeedKey] = m_layoutGenerator.Seed
            };
            return state;
        }

        /*--------------------------------------------------------------------------------------------
        | --- RestoreState: Restores the seed and regenerates the level based on the saved state --- |
        --------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state is not JObject jObject)
                return;

            if (!jObject.TryGetValue(kSeedKey, out JToken seedToken))
                return;

            m_layoutGenerator.Seed = seedToken.ToObject<long>();
            GenerateLevel();
        }
    }
}