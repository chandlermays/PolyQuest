using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /*----------------------------------------------------------- 
    | --- Responsible for Controlling an AI's Actions (NPC) --- |
    | 
    | DEPRECATED: This class is maintained for backwards compatibility.
    | 
    | Please migrate to the new AI system:
    | - Replace this component with AIController (Assets/Scripts/AI/AIController.cs)
    | - Set AIType to NPC
    | - Configure behavior using AIData ScriptableObject
    | - Add PatrolComponent with waypoints (compatible with NavigationPath)
    | 
    | See Assets/Docs/AI_Migration.md for step-by-step migration instructions.
    | 
    | TODO: Migrate all NPCController instances to new AIController system.
    -----------------------------------------------------------*/
    [System.Obsolete("Use new AIController (Assets/Scripts/AI/AIController.cs) with AIType.NPC. See AI_Migration.md for migration guide.")]
    public class NPCController : AIControllerLegacy
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