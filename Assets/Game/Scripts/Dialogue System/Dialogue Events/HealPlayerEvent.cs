/*---------------------------
File: HealPlayerEvent.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------
using PolyQuest.Components;

namespace PolyQuest.Dialogues
{
    [CreateAssetMenu(menuName = "PolyQuest/Dialogue/Events/Heal Player", fileName = "New Heal Player Event")]
    public class HealPlayerEvent : DialogueEvent
    {
        public override void Execute(GameObject instigator, GameObject target)
        {
            if (!instigator.TryGetComponent<HealthComponent>(out var health))
                return;

            health.FullyReplenishHealth();
        }
    }
}