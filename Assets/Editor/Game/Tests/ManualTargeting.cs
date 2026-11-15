/*---------------------------
| File: ManualTargeting.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Abilities;

namespace PolyQuest.Tests
{
    // A targeting strategy the test controls: it picks the given targets, and finishes only when the test calls
    // Finish, standing in for the player picking a spot or target with the cursor.
    public class ManualTargeting : TargetingStrategy
    {
        public List<GameObject> Targets = new();

        private AbilityConfig m_config;
        private Action m_onComplete;

        public override void StartTargeting(AbilityConfig config, Action onComplete)
        {
            m_config = config;
            m_onComplete = onComplete;
        }

        public void Finish()
        {
            m_config.Targets = new List<GameObject>(Targets);
            m_onComplete?.Invoke();
        }
    }
}