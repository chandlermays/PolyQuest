using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
namespace PolyQuest.PCG
{
    public class LayoutGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject m_layoutDisplay;
        [SerializeField] private long m_seed = 0;
        [SerializeField] private LevelLayoutConfig m_levelLayoutConfig;

        private XorShift128Plus m_rng;
        private Level m_level;
        private List<Corridor> m_openCorridors;
        private Dictionary<RoomTemplate, int> m_availableRooms;

        public XorShift128Plus RNG => m_rng;

        /*--------------------------------------------------------------------------------------
        | --- GenerateLayout: Generates a level layout based on the provided configuration --- |
        --------------------------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Generate Layout")]
        public Level GenerateLayout()
        {
            InitializeGeneration();

            RectInt startRoomRect = CreateStartRoom();
            GenerateRooms();
            GenerateCorridors();
            AssignStartAndEndRooms();
            PopulateLevelGrid();

#if UNITY_EDITOR
            DrawLayout(startRoomRect);
#endif

            return m_level;
        }

        /*--------------------------------------------------------------------------------
        | --- GenerateNewSeed: Generates a new random seed based on the current time --- |
        --------------------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Generate New Seed")]
        public void GenerateNewSeed()
        {
            m_seed = DateTime.Now.Ticks;
        }

        /*-------------------------------------------------------------------
        | --- GenerateNewSeedAndLayout: Generates a new seed and layout --- |
        -------------------------------------------------------------------*/
        [ContextMenu("DEBUG: Generate New Seed and Layout")]
        public void GenerateNewSeedAndLayout()
        {
            GenerateNewSeed();
            GenerateLayout();
        }

        /*------------------------------------------------------------------
        | --- InitializeGeneration: Initializes the generation process --- |
        ------------------------------------------------------------------*/
        private void InitializeGeneration()
        {
            m_rng = new XorShift128Plus(unchecked((ulong)m_seed));
            m_level = new Level();
            m_availableRooms = m_levelLayoutConfig.GetAvailableRooms();
            m_openCorridors = new();
        }

        /*-------------------------------------------------------------------------
        | --- CreateStartRoom: Creates the starting room for the level layout --- |
        -------------------------------------------------------------------------*/
        private RectInt CreateStartRoom()
        {
            RoomTemplate startRoomTemplate = GetRandomRoomTemplate();
            RectInt startRoomRect = GetStartRoomRect(startRoomTemplate);
            Room startRoom = CreateNewRoom(startRoomRect, startRoomTemplate);
            List<Corridor> corridors = startRoom.GenerateCorridorCandidates(startRoom.Area.width, startRoom.Area.height, m_levelLayoutConfig.DoorDistanceFromEdge);
            foreach (Corridor corridor in corridors)
            {
                corridor.StartRoom = startRoom;
                m_openCorridors.Add(corridor);
            }
            m_level.AddRoom(startRoom);
            return startRoomRect;
        }

        /*-----------------------------------------------------------------------------------------
        | --- GenerateRooms: Generates rooms connected by corridors until constraints are met --- |
        -----------------------------------------------------------------------------------------*/
        private void GenerateRooms()
        {
            while (m_openCorridors.Count > 0 && m_level.Rooms.Count < m_levelLayoutConfig.MaxRoomCount && m_availableRooms.Count > 0)
            {
                Corridor entry = m_openCorridors[m_rng.RandomRange(0, m_openCorridors.Count)];
                Room newRoom = CreateAdjacentRoom(entry);
                if (newRoom == null)
                {
                    m_openCorridors.Remove(entry);
                    continue;
                }
                m_level.AddRoom(newRoom);
                m_level.AddCorridor(entry);
                entry.EndRoom = newRoom;
                List<Corridor> newCorridors = newRoom.GenerateCorridorCandidates(newRoom.Area.width, newRoom.Area.height, m_levelLayoutConfig.DoorDistanceFromEdge);
                foreach (Corridor corridor in newCorridors)
                {
                    corridor.StartRoom = newRoom;
                }
                m_openCorridors.Remove(entry);
                m_openCorridors.AddRange(newCorridors);
            }
        }

        /*-----------------------------------------------------------------------------
        | --- GenerateCorridors: Associates corridors with their respective rooms --- |
        -----------------------------------------------------------------------------*/
        private void GenerateCorridors()
        {
            foreach (Room room in m_level.Rooms)
            {
                foreach (Corridor incomingCorridor in m_level.Corridors)
                {
                    if (incomingCorridor.StartRoom == room)
                        room.AddCorridor(incomingCorridor);
                }
                foreach (Corridor outgoingCorridor in m_level.Corridors)
                {
                    if (outgoingCorridor.EndRoom == room)
                        room.AddCorridor(outgoingCorridor);
                }
            }
        }

        /*-------------------------------------------------------------------------------------
        | --- AssignStartAndEndRooms: Assigns the start and end rooms in the level layout --- |
        -------------------------------------------------------------------------------------*/
        private void AssignStartAndEndRooms()
        {
            List<Room> rooms = new();
            foreach (Room room in m_level.Rooms)
            {
                if (room.NumCorridors == 1)
                {
                    rooms.Add(room);
                }
            }
            if (rooms.Count < 2)
                return;

            // Assign Start Room
            int startRoomIndex = m_rng.RandomRange(0, rooms.Count);
            Room startRoom = rooms[startRoomIndex];
            m_level.StartRoom = startRoom;
            startRoom.Type = RoomType.kStart;
            rooms.Remove(startRoom);

            // Assign End Room (farthest from start)
            Room endRoom = null;
            float maxDistance = float.MinValue;
            foreach (Room room in rooms)
            {
                float distance = Vector2.Distance(startRoom.Area.center, room.Area.center);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    endRoom = room;
                }
            }
            if (endRoom != null)
            {
                endRoom.Type = RoomType.kEnd;
                rooms.Remove(endRoom);
            }

            // Assign Boss and Treasure rooms
            AssignBossAndTreasureRooms(startRoom, endRoom);
        }

        /*-----------------------------------------------------------------------------------------
        | --- AssignBossAndTreasureRooms: Assigns Boss and Treasure rooms using scoring model --- |
        -----------------------------------------------------------------------------------------*/
        private void AssignBossAndTreasureRooms(Room startRoom, Room endRoom)
        {
            if (startRoom == null || endRoom == null)
                return;

            List<Room> availableRooms = new();
            foreach (Room room in m_level.Rooms)
            {
                if (room.Type == RoomType.kNormal)
                {
                    availableRooms.Add(room);
                }
            }

            if (availableRooms.Count < 2)
                return;

            // Calculate distances for scoring
            Vector2 startPos = startRoom.Area.center;
            Vector2 endPos = endRoom.Area.center;
            float totalDistance = Vector2.Distance(startPos, endPos);

            // Score and assign Boss Room
            Room bossRoom = SelectBossRoom(availableRooms, startPos, endPos, totalDistance);
            if (bossRoom != null)
            {
                bossRoom.Type = RoomType.kBoss;
                availableRooms.Remove(bossRoom);

                // Score and assign Treasure Room (prefer rooms near Boss)
                Room treasureRoom = SelectTreasureRoom(availableRooms, bossRoom.Area.center, endPos);
                if (treasureRoom != null)
                {
                    treasureRoom.Type = RoomType.kTreasure;
                }
            }
        }

        /*-------------------------------------------------------------------------------------
        | --- SelectBossRoom: Selects the Boss room based on midpoint positioning scoring --- |
        -------------------------------------------------------------------------------------*/
        private Room SelectBossRoom(List<Room> availableRooms, Vector2 startPos, Vector2 endPos, float totalDistance)
        {
            Room bestRoom = null;
            float bestScore = float.MinValue;

            Vector2 idealMidpoint = (startPos + endPos) / 2f;
            BossRoomScoringWeights weights = m_levelLayoutConfig.BossRoomScoring;

            foreach (Room room in availableRooms)
            {
                Vector2 roomPos = room.Area.center;

                // Score based on proximity to midpoint
                float distanceToMidpoint = Vector2.Distance(roomPos, idealMidpoint);
                float midpointScore = 1f - Mathf.Clamp01(distanceToMidpoint / (totalDistance * weights.MidpointTolerance));

                // Score based on distance from start (prefer not too close to start)
                float distanceFromStart = Vector2.Distance(roomPos, startPos);
                float startDistanceScore = Mathf.Clamp01(distanceFromStart / (totalDistance * weights.MinStartDistance));

                // Score based on distance from end (prefer not too close to end)
                float distanceFromEnd = Vector2.Distance(roomPos, endPos);
                float endDistanceScore = Mathf.Clamp01(distanceFromEnd / (totalDistance * weights.MinEndDistance));

                // Prefer rooms with more connections (more "central" to the layout)
                float connectivityScore = Mathf.Clamp01(room.NumCorridors / 4f);

                // Weighted total score
                float totalScore = (midpointScore * weights.MidpointWeight) +
                                  (startDistanceScore * weights.StartDistanceWeight) +
                                  (endDistanceScore * weights.EndDistanceWeight) +
                                  (connectivityScore * weights.ConnectivityWeight);

                // Add random factor for variety
                totalScore += m_rng.RandomFloat() * weights.RandomVariance;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestRoom = room;
                }
            }

            return bestRoom;
        }

        /*--------------------------------------------------------------------------------------
        | --- SelectTreasureRoom: Selects the Treasure room based on proximity to Boss/End --- |
        --------------------------------------------------------------------------------------*/
        private Room SelectTreasureRoom(List<Room> availableRooms, Vector2 bossPos, Vector2 endPos)
        {
            Room bestRoom = null;
            float bestScore = float.MinValue;

            TreasureRoomScoringWeights weights = m_levelLayoutConfig.TreasureRoomScoring;

            foreach (Room room in availableRooms)
            {
                Vector2 roomPos = room.Area.center;

                // Score based on proximity to Boss (prefer nearby)
                float distanceToBoss = Vector2.Distance(roomPos, bossPos);
                float bossProximityScore = 1f / (1f + distanceToBoss * weights.BossProximityFalloff);

                // Score based on position relative to end (prefer between Boss and End, or near End)
                float distanceToEnd = Vector2.Distance(roomPos, endPos);
                float endProximityScore = 1f / (1f + distanceToEnd * weights.EndProximityFalloff);

                // Prefer rooms with fewer connections (more "side room" feel)
                float connectivityScore;
                if (room.NumCorridors == 1)
                    connectivityScore = weights.DeadEndBonus;
                else if (room.NumCorridors == 2)
                    connectivityScore = weights.TwoCorridorScore;
                else
                    connectivityScore = weights.MultiCorridorScore;

                // Weighted total score
                float totalScore = (bossProximityScore * weights.BossProximityWeight) +
                                  (endProximityScore * weights.EndProximityWeight) +
                                  (connectivityScore * weights.ConnectivityWeight);

                // Add random factor for variety
                totalScore += m_rng.RandomFloat() * weights.RandomVariance;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestRoom = room;
                }
            }

            return bestRoom;
        }

        /*------------------------------------------------------------------------------------------
        | --- PopulateLevelGrid: Fills the level grid with floor tiles for rooms and corridors --- |
        ------------------------------------------------------------------------------------------*/
        private void PopulateLevelGrid()
        {
            foreach (Room room in m_level.Rooms)
            {
                m_level.DrawRect(room.Area, TileType.kFloor);
            }

            foreach (Corridor corridor in m_level.Corridors)
            {
                m_level.DrawLine(corridor.StartPositionAbs, corridor.EndPositionAbs, TileType.kFloor);
            }
        }

        /*---------------------------------------------------------------------------------
        | --- GetStartRoomRect: Determines the position and size of the starting room --- |
        ---------------------------------------------------------------------------------*/
        private RectInt GetStartRoomRect(RoomTemplate roomTemplate)
        {
            RectInt roomSize = roomTemplate.GenerateRoomCandidate(m_rng);

            int roomWidth = roomSize.width;
            int availableWidth = (m_level.Width / 2) - roomWidth;
            int randomX = m_rng.RandomRange(0, availableWidth);
            int roomX = randomX + (m_level.Width / 4);

            int roomLength = roomSize.height;
            int availableLength = (m_level.Length / 2) - roomLength;
            int randomY = m_rng.RandomRange(0, availableLength);
            int roomY = randomY + (m_level.Length / 4);
            return new RectInt(roomX, roomY, roomWidth, roomLength);
        }

        /*-------------------------------------------------------------------------------------
        | --- CreateAdjacentRoom: Creates a new room adjacent to the given corridor entry --- |
        -------------------------------------------------------------------------------------*/
        private Room CreateAdjacentRoom(Corridor entry)
        {
            RoomTemplate roomTemplate = GetRandomRoomTemplate();
            RectInt roomCandidate = roomTemplate.GenerateRoomCandidate(m_rng);
            Corridor exit = SelectCorridorCandidate(roomCandidate, roomTemplate, entry);
            if (exit == null)
                return null;

            int distance = m_rng.RandomRange(m_levelLayoutConfig.MinCorridorLength, m_levelLayoutConfig.MaxCorridorLength);
            Vector2Int roomCandidatePosition = CalculateRoomPosition(entry, roomCandidate.width, roomCandidate.height, distance, exit.StartPosition);
            roomCandidate.position = roomCandidatePosition;
            if (!IsRoomPositionValid(roomCandidate))
                return null;

            Room newRoom = CreateNewRoom(roomCandidate, roomTemplate);
            entry.EndRoom = newRoom;
            entry.EndPosition = exit.StartPosition;
            return newRoom;
        }

        /*---------------------------------------------------------------------------------
        | --- SelectCorridorCandidate: Selects a corridor candidate from the new room --- |
        ---------------------------------------------------------------------------------*/
        private Corridor SelectCorridorCandidate(RectInt roomRect, RoomTemplate roomTemplate, Corridor entry)
        {
            Room room = CreateNewRoom(roomRect, roomTemplate, false);
            List<Corridor> candidates = room.GenerateCorridorCandidates(room.Area.width, room.Area.height, m_levelLayoutConfig.DoorDistanceFromEdge);
            Direction oppositeDirection = entry.GetOppositeDirection(entry.StartDirection);
            List<Corridor> filteredCandidates = new();

            foreach (Corridor candidate in candidates)
            {
                if (candidate.StartDirection == oppositeDirection)
                {
                    filteredCandidates.Add(candidate);
                }
            }
            return filteredCandidates.Count > 0 ? filteredCandidates[m_rng.RandomRange(0, filteredCandidates.Count)] : null;
        }

        /*--------------------------------------------------------------------------------------------------
        | --- CalculateRoomPosition: Calculates the position of a new room based on the entry corridor --- |
        --------------------------------------------------------------------------------------------------*/
        private Vector2Int CalculateRoomPosition(Corridor entry, int roomWidth, int roomLength, int distance, Vector2Int endPosition)
        {
            Vector2Int roomPosition = entry.StartPositionAbs;
            switch (entry.StartDirection)
            {
                case Direction.kNorth:
                    roomPosition.x -= endPosition.x;
                    roomPosition.y += distance + 1;
                    break;
                case Direction.kSouth:
                    roomPosition.x -= endPosition.x;
                    roomPosition.y -= distance + roomLength;
                    break;
                case Direction.kWest:
                    roomPosition.x -= distance + roomWidth;
                    roomPosition.y -= endPosition.y;
                    break;
                case Direction.kEast:
                    roomPosition.x += distance + roomWidth;
                    roomPosition.y -= endPosition.y;
                    break;
            }
            return roomPosition;
        }

        /*----------------------------------------------------------------------------
        | --- IsRoomPositionValid: Checks if the proposed room position is valid --- |
        ----------------------------------------------------------------------------*/
        private bool IsRoomPositionValid(RectInt roomRect)
        {
            RectInt levelRect = new(1, 1, m_level.Width - 2, m_level.Length - 2);
            bool valid = roomRect.xMin >= levelRect.xMin && roomRect.yMin >= levelRect.yMin && roomRect.xMax <= levelRect.xMax && roomRect.yMax <= levelRect.yMax
                && !DoesRoomOverlap(roomRect, m_level.Rooms, m_level.Corridors, m_levelLayoutConfig.MinDistanceBetweenRooms);

            return valid;
        }

        /*------------------------------------------------------------------------------------------------
        | --- DoesRoomOverlap: Checks if the proposed room overlaps with existing rooms or corridors --- |
        ------------------------------------------------------------------------------------------------*/
        private bool DoesRoomOverlap(RectInt roomRect, IReadOnlyList<Room> rooms, IReadOnlyList<Corridor> corridors, int minRoomDistance)
        {
            RectInt paddedRoomRect = new()
            {
                x = roomRect.x - minRoomDistance,
                y = roomRect.y - minRoomDistance,
                width = roomRect.width + (2 * minRoomDistance),
                height = roomRect.height + (2 * minRoomDistance)
            };

            foreach (Room room in rooms)
            {
                if (paddedRoomRect.Overlaps(room.Area))
                    return true;
            }
            foreach (Corridor corridor in corridors)
            {
                if (paddedRoomRect.Overlaps(corridor.Area))
                    return true;
            }

            return false;
        }

        /*-------------------------------------------------------------------------------------------
        | --- CreateNewRoom: Creates a new room based on the candidate, rectangle, and template --- |
        -------------------------------------------------------------------------------------------*/
        private Room CreateNewRoom(RectInt roomCandidate, RoomTemplate roomTemplate, bool apply = true)
        {
            if (apply)
            {
                ApplyRoomTemplate(roomTemplate);
            }
            return new Room(roomCandidate);
        }

        /*-----------------------------------------------------------------------------------
        | --- ApplyRoomTemplate: Updates the available rooms based on the used template --- |
        -----------------------------------------------------------------------------------*/
        private void ApplyRoomTemplate(RoomTemplate roomTemplate)
        {
            --m_availableRooms[roomTemplate];
            if (m_availableRooms[roomTemplate] == 0)
            {
                m_availableRooms.Remove(roomTemplate);
            }
        }

        /*----------------------------------------------------------------------------------------
        | --- GetRandomRoomTemplate: Selects a random room template from the available rooms --- |
        ----------------------------------------------------------------------------------------*/
        private RoomTemplate GetRandomRoomTemplate()
        {
            int index = m_rng.RandomRange(0, m_availableRooms.Count);
            int currentIndex = 0;
            foreach (RoomTemplate template in m_availableRooms.Keys)
            {
                if (currentIndex == index)
                {
                    return template;
                }
                ++currentIndex;
            }
            return null;
        }

#if UNITY_EDITOR
        /*----------------------------------------------------------------------
        | --- DrawLayout: Visualizes the generated layout onto a Texture2D --- |
        ----------------------------------------------------------------------*/
        private void DrawLayout(RectInt roomRect)
        {
            if (!m_layoutDisplay.TryGetComponent<Renderer>(out var renderer))
                return;

            Texture2D layoutTexture = (Texture2D)renderer.sharedMaterial.mainTexture;
            if (layoutTexture == null)
                return;

            layoutTexture.Reinitialize(m_level.Width, m_level.Length);
            m_layoutDisplay.transform.localScale = new Vector3(m_level.Width * 2, m_level.Length * 2, 1);
            float xPos = (m_level.Width * 2 / 2.0f) - 2;
            float zPos = (m_level.Length * 2 / 2.0f) - 2;
            m_layoutDisplay.transform.position = new Vector3(xPos, 0.01f, zPos);
            layoutTexture.FillWithColor(Color.black);
            layoutTexture.ConvertToBlackAndWhite();

            foreach (Room room in m_level.Rooms)
            {
                layoutTexture.DrawRectangle(room.Area, Color.white);
            }
            foreach (Corridor corridor in m_level.Corridors)
            {
                layoutTexture.DrawLine(corridor.StartPositionAbs, corridor.EndPositionAbs, Color.white);
            }

            layoutTexture.DrawRectangle(roomRect, Color.white);

            layoutTexture.SaveAsset();
        }
#endif
    }
}