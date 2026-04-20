/*---------------------------
File: TriggerObjective.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.Quests
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Detects when the player enters a trigger area and signals quest objective completion.  *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Monitors trigger collisions for the player using Unity's physics system.              *
     *      - Checks for the presence of an ObjectiveCompletion component on the same GameObject.   *
     *      - Invokes objective completion logic when the player enters the trigger.                *
     *      - Enables designers to mark quest objectives as complete via in-game triggers.          *
     * -------------------------------------------------------------------------------------------- */
    [RequireComponent(typeof(Collider))]
    public class TriggerObjective : MonoBehaviour
    {
        private const string kPlayerTag = "Player";

        /*-------------------------------------------------------------------------
        | --- OnTriggerEnter: Called when another collider enters the trigger --- |
        -------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            // Check if the Player entered the trigger
            if (other.CompareTag(kPlayerTag))
            {
                // If the component exists, complete the objective
                if (TryGetComponent<ObjectiveCompletion>(out var questCompletion))
                {
                    questCompletion.CompleteObjective();
                }
            }
        }
    }
}