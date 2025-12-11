using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /*----------------------------------------------------------- 
    | --- Responsible for Controlling an AI's Actions (NPC) --- |
    | --- DEPRECATED: Use NewAIController with AIType.NPC --- |
    | --- This class is maintained for backward compatibility --- |
    | --- TODO: Migrate prefabs to NewAIController system --- |
    -----------------------------------------------------------*/
    [System.Obsolete("NPCController is deprecated. Use NewAIController with AIType.NPC instead.")]
    public class NPCController : AIController
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