using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    public abstract class FilteringStrategy : ScriptableObject
    {
        public abstract IEnumerable<GameObject> Filter(IEnumerable<GameObject> targets);
    }
}