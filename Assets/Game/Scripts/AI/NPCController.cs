using UnityEngine;
//---------------------------------

namespace PolyQuest.AI
{
    /*----------------------------------------------------------- 
    | --- Responsible for Controlling an AI's Actions (NPC) --- |
    -----------------------------------------------------------*/
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