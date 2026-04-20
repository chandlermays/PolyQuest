/*---------------------------
File: EffectStrategy.cs
Author: Chandler Mays
----------------------------*/
using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    public abstract class EffectStrategy : ScriptableObject
    {
        public abstract void StartEffect(AbilityConfig config, Action onComplete);
    }
}