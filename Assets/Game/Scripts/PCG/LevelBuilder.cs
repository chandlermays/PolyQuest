using System;
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

        private Level m_level;
        private const string kSeedKey = "Seed";

        private bool m_hasBeenGenerated = false;
        public event Action OnLevelGenerated;

        private void Start()
        {
            if (!m_hasBeenGenerated)
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
            GenerateLevel();
        }

        /*----------------------------------------------------------------------------------------
        | --- RegenerateDecorations: Generates a new arrangement of decorations in the level --- |
        ----------------------------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Regenerate Decorations")]
        public void RegenerateDecorations()
        {
            if (m_level == null)
            {
                Debug.LogWarning("Regenerate Decorations: Must generate a level before placing decorations.");
                return;
            }

            m_roomDecorator.Initialize(m_level, DateTime.Now.Ticks);
        }

        /*---------------------------------------------------------------------------------------------
        | --- GenerateLevel: Generates the level geometry and navmesh based on the current layout --- |
        ---------------------------------------------------------------------------------------------*/
        private void GenerateLevel()
        {
            m_level = m_layoutGenerator.GenerateLayout();
            if (m_level == null)
            {
                Debug.LogError("Generate Level: Failed to generate level layout.");
                return;
            }
            m_levelGeometry.CreateLevelGeometry();
            m_roomDecorator.Initialize(m_level, m_layoutGenerator.Seed);
            m_navMeshSurface.BuildNavMesh();

            m_hasBeenGenerated = true;
            OnLevelGenerated?.Invoke();
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