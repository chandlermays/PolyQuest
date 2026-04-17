using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    [Serializable]
    [CreateAssetMenu(menuName = "PolyQuest/PCG/Decorators/AND Pattern", fileName = "New 'AND' Pattern")]
    public class AndPattern : CompositePattern
    {
        /*--------------------------------------------------------------------------------------
        | --- CanBeApplied: Checks if all of the patterns can be applied to the given room --- |
        --------------------------------------------------------------------------------------*/
        public override bool CanBeApplied(TileType[,] decoratedLevel, Room room)
        {
            foreach (DecoratorPattern pattern in Patterns)
            {
                if (!pattern.CanBeApplied(decoratedLevel, room))
                {
                    return false;
                }
            }

            return true;
        }

        /*-------------------------------------------------------------------------------------
        | --- Apply: Applies all of the patterns to the given room in the decorated level --- |
        -------------------------------------------------------------------------------------*/
        public override void Apply(TileType[,] decoratedLevel, Room room, Transform parent, long seed)
        {
            foreach (DecoratorPattern pattern in Patterns)
            {
                pattern.Apply(decoratedLevel, room, parent, seed);
            }
        }
    }
}