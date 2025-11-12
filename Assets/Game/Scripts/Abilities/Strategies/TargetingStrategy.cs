using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    public abstract class TargetingStrategy : ScriptableObject
    {
        public abstract void StartTargeting(AbilityConfig config, Action onComplete);
    }
}