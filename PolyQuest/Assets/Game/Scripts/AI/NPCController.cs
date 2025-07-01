using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    /*----------------------------------------------------------- 
    | --- Responsible for Controlling an AI's Actions (NPC) --- |
    -----------------------------------------------------------*/
    public class NPCController : NonPlayerController
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