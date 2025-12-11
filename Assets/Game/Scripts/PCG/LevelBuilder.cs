using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

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
        private void Start()
        {
            GenerateNewSeedAndLevel();
        }

        /*------------------------------------------------------------------------
        | --- GenerateNewSeedAndLevel: Generates a new seed and level layout --- |
        ------------------------------------------------------------------------*/
        [ContextMenu("Generate New Seed and Level")]
        public void GenerateNewSeedAndLevel()
        {
            m_layoutGenerator.GenerateNewSeed();
            GenerateNewLevel();
        }

        /*------------------------------------------------------------------------------
        | --- GenerateNewLevel: Generates a new level layout with the current seed --- |
        ------------------------------------------------------------------------------*/
        [ContextMenu("Generate New Level")]
        public void GenerateNewLevel()
        {
            Level level = m_layoutGenerator.GenerateLayout();
            m_levelGeometry.CreateLevelGeometry();
            m_roomDecorator.Initialize(level);
            m_navMeshSurface.BuildNavMesh();

            Room startRoom = level.StartRoom;
            Vector2 center = startRoom.Area.center;
            Vector3 playerPosition = ToWorldPosition(center);
            if (m_player.TryGetComponent<NavMeshAgent>(out var playerAgent))
            {
                playerAgent.Warp(playerPosition);
            }
        }

        /*-----------------------------------------------------------------------------
        | --- ToWorldPosition: Converts a 2D grid position to a 3D world position --- |
        -----------------------------------------------------------------------------*/
        private Vector3 ToWorldPosition(Vector2 position)
        {
            float scale = m_levelGeometry.Scale;
            return new Vector3((position.x - 1) * scale, 1, (position.y - 1) * scale);
        }
    }
}