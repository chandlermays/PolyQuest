/*---------------------------
File: SelfTargeting.cs
Author: Chandler Mays
----------------------------*/
using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Targeting/New Self-Targeting Strategy", fileName = "New Self-Targeting Strategy")]
    public class SelfTargeting : TargetingStrategy
    {
        /*------------------------------------------------------------------------- 
        | --- StartTargeting: Initiates the targeting process for the ability --- |
        -------------------------------------------------------------------------*/
        public override void StartTargeting(AbilityConfig config, Action onComplete)
        {
            config.SetSingleTarget(config.User);
            config.TargetPoint = config.User.transform.position;
            onComplete?.Invoke();
        }
    }
}