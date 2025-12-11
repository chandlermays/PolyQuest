using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    [System.Serializable]
    public class RoomLimit
    {
        [SerializeField] private RoomTemplate m_roomTemplate;
        [SerializeField] private int m_maxCountPerTemplate = 1;

        public RoomTemplate RoomTemplate => m_roomTemplate;
        public int MaxCountPerTemplate => m_maxCountPerTemplate;
    }

    [System.Serializable]
    public class BossRoomScoringWeights
    {
        [Header("Scoring Weights")]
        [Tooltip("Emphasizes rooms near the geometric midpoint between Start and End. Higher values make the boss gravitate toward the center of the layout.")]
        [SerializeField][Range(0f, 5f)] private float m_midpointWeight = 3f;
        [Tooltip("Prefers rooms farther from Start. Higher value makes boss selection favor rooms distant from the start.")]
        [SerializeField][Range(0f, 5f)] private float m_startDistanceWeight = 1.5f;
        [Tooltip("Prefers rooms farther from End. Higher value increases the chance of rooms being chosen that are distant from the end.")]
        [SerializeField][Range(0f, 5f)] private float m_endDistanceWeight = 1f;
        [Tooltip("Favors rooms with more corridors (up to 4). Higher values make well-connected, central rooms more likely to be the boss.")]
        [SerializeField][Range(0f, 5f)] private float m_connectivityWeight = 0.5f;
        [Tooltip("Adds randomness to introduce variability. Larger values make boss selection less deterministic.")]
        [SerializeField][Range(0f, 2f)] private float m_randomVariance = 0.5f;

        [Header("Distance Thresholds (as fraction of total distance)")]
        [Tooltip("Controls how quickly the midpoint score decays with distance. Smaller tolerance sharply penalizes rooms away from the midpoint.")]
        [SerializeField][Range(0.1f, 1f)] private float m_midpointTolerance = 0.5f;
        [Tooltip("Sets how far from Start the room must be. Higher values require the boss to be farther from Start.")]
        [SerializeField][Range(0.1f, 0.5f)] private float m_minStartDistance = 0.3f;
        [Tooltip("Sets how far from End the room must be. Higher values require the boss to be farther from End.")]
        [SerializeField][Range(0.1f, 0.5f)] private float m_minEndDistance = 0.3f;

        public float MidpointWeight => m_midpointWeight;
        public float StartDistanceWeight => m_startDistanceWeight;
        public float EndDistanceWeight => m_endDistanceWeight;
        public float ConnectivityWeight => m_connectivityWeight;
        public float RandomVariance => m_randomVariance;
        public float MidpointTolerance => m_midpointTolerance;
        public float MinStartDistance => m_minStartDistance;
        public float MinEndDistance => m_minEndDistance;
    }

    [System.Serializable]
    public class TreasureRoomScoringWeights
    {
        [Header("Scoring Weights")]
        [Tooltip("Prefers rooms nearthe Boss. Higher values strongly bias treasure to be close to the boss.")]
        [SerializeField][Range(0f, 5f)] private float m_bossProximityWeight = 2f;
        [Tooltip("Prefers rooms near the End. Higher values strongly bias treasure to be close to the end.")]
        [SerializeField][Range(0f, 5f)] private float m_endProximityWeight = 1.5f;
        [Tooltip("Prefers 'side rooms' with fewer connections using discrete scores. Increase this value to consistently choose dead-end rooms.")]
        [SerializeField][Range(0f, 5f)] private float m_connectivityWeight = 1f;
        [Tooltip("Adds randomness to introduce variability. Larger values make treasure selection less deterministic.")]
        [SerializeField][Range(0f, 2f)] private float m_randomVariance = 0.5f;

        [Header("Proximity Settings")]
        [Tooltip("Controls how quickly the proximity score drops with distance. Larger falloff makes the score decay faster.")]
        [SerializeField][Range(0.01f, 0.5f)] private float m_bossProximityFalloff = 0.1f;
        [Tooltip("Controls how quickly the proximity score drops with distance. Larger falloff makes the score decay faster.")]
        [SerializeField][Range(0.01f, 0.5f)] private float m_endProximityFalloff = 0.05f;

        [Header("Connectivity Scores")]
        [Tooltip("Increase favorability to rooms with one corridor connection.")]
        [SerializeField][Range(0f, 3f)] private float m_deadEndBonus = 1.5f;
        [Tooltip("Increase favorability to rooms with two corridor connections.")]
        [SerializeField][Range(0f, 2f)] private float m_twoCorridorScore = 1f;
        [Tooltip("Increase favorability to rooms with three or more corridor connections.")]
        [SerializeField][Range(0f, 1f)] private float m_multiCorridorScore = 0.5f;

        public float BossProximityWeight => m_bossProximityWeight;
        public float EndProximityWeight => m_endProximityWeight;
        public float ConnectivityWeight => m_connectivityWeight;
        public float RandomVariance => m_randomVariance;
        public float BossProximityFalloff => m_bossProximityFalloff;
        public float EndProximityFalloff => m_endProximityFalloff;
        public float DeadEndBonus => m_deadEndBonus;
        public float TwoCorridorScore => m_twoCorridorScore;
        public float MultiCorridorScore => m_multiCorridorScore;
    }

    [CreateAssetMenu(fileName = "New Level Layout Config", menuName = "PolyQuest/PCG/Level Layout Config", order = 0)]
    public class LevelLayoutConfig : ScriptableObject
    {
        [Header("Room Generation")]
        [SerializeField] private int m_maxRoomCount = 10;
        [SerializeField] private List<RoomLimit> m_roomLimits = new();

        [Header("Corridor Settings")]
        [SerializeField] private int m_minCorridorLength = 3;
        [SerializeField] private int m_maxCorridorLength = 5;

        [Header("Spacing Settings")]
        [SerializeField] private int m_doorDistanceFromEdge = 1;
        [SerializeField] private int m_minDistanceBetweenRooms = 1;

        [Header("Room Type Scoring")]
        [SerializeField] private BossRoomScoringWeights m_bossRoomScoring = new();
        [SerializeField] private TreasureRoomScoringWeights m_treasureRoomScoring = new();

        public int MaxRoomCount => m_maxRoomCount;
        public int MinCorridorLength => m_minCorridorLength;
        public int MaxCorridorLength => m_maxCorridorLength;
        public int DoorDistanceFromEdge => m_doorDistanceFromEdge;
        public int MinDistanceBetweenRooms => m_minDistanceBetweenRooms;
        public BossRoomScoringWeights BossRoomScoring => m_bossRoomScoring;
        public TreasureRoomScoringWeights TreasureRoomScoring => m_treasureRoomScoring;

        /*----------------------------------------------------------------------------------------------------
        | --- GetAvailableRooms: Retrieves a dictionary of available room templates and their max counts --- |
        ----------------------------------------------------------------------------------------------------*/
        public Dictionary<RoomTemplate, int> GetAvailableRooms()
        {
            var availableRooms = new Dictionary<RoomTemplate, int>();
            if (m_roomLimits == null)
                return availableRooms;

            foreach (RoomLimit roomLimit in m_roomLimits)
            {
                if (roomLimit?.RoomTemplate != null && roomLimit.MaxCountPerTemplate > 0)
                {
                    if (!availableRooms.ContainsKey(roomLimit.RoomTemplate))
                    {
                        availableRooms.Add(roomLimit.RoomTemplate, roomLimit.MaxCountPerTemplate);
                    }
                }
            }

            return availableRooms;
        }
    }
}