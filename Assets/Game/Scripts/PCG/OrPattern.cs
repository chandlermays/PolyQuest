using System.Collections.Generic;
using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    [Serializable]
    [CreateAssetMenu(fileName = "New 'OR' Pattern", menuName = "PolyQuest/PCG/Decorators/OR Pattern", order = 2)]
    public class OrPattern : CompositePattern
    {
        /*--------------------------------------------------------------------------------------
        | --- CanBeApplied: Checks if any of the patterns can be applied to the given room --- |
        --------------------------------------------------------------------------------------*/
        public override bool CanBeApplied(TileType[,] decoratedLevel, Room room)
        {
            foreach (DecoratorPattern pattern in Patterns)
            {
                if (pattern.CanBeApplied(decoratedLevel, room))
                {
                    return true;
                }
            }
            return false;
        }

        /*-----------------------------------------------------------------------------------
        | --- Apply: Randomly selects and applies one of the patterns to the given room --- |
        -----------------------------------------------------------------------------------*/
        public override void Apply(TileType[,] decoratedLevel, Room room, Transform parent)
        {
            List<DecoratorPattern> applicablePatterns = new();
            foreach (DecoratorPattern pattern in Patterns)
            {
                if (pattern.CanBeApplied(decoratedLevel, room))
                {
                    applicablePatterns.Add(pattern);
                }
            }

            if (applicablePatterns.Count == 0)
                return;

            XorShift128Plus rng = new((ulong)(DateTime.Now.Ticks));
            int patternIndex = rng.RandomRange(0, applicablePatterns.Count);
            applicablePatterns[patternIndex].Apply(decoratedLevel, room, parent);
        }
    }
}