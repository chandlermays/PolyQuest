using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /*----------------------------------------------------------- 
    | --- Responsible for Controlling an AI's Actions (NPC) --- |
    -----------------------------------------------------------*/
    /// <summary>
    /// DEPRECATED: This class is deprecated. Use the new AIController with AIType.NPC instead.
    /// 
    /// Migration guide:
    /// 1. Replace NPCController with the new AIController component
    /// 2. Set AIType to NPC
    /// 3. Create an AIData ScriptableObject and assign it
    /// 4. Add PatrolComponent for waypoint patrolling
    /// 5. Keep the existing MovementComponent
    /// 
    /// See Assets/Docs/AI_Migration.md for detailed instructions.
    /// </summary>
    [System.Obsolete("Use the new AIController with state machine instead. See Assets/Docs/AI_Migration.md")]
    public class NPCController : AIControllerBase
    {
        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            PatrolState();

            m_timeSinceArrivedAtWaypoint += Time.deltaTime;
        }
    }
}