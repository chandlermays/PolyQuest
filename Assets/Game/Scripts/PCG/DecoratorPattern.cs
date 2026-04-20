/*---------------------------
File: DecoratorPattern.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public abstract class DecoratorPattern : ScriptableObject
    {
        public abstract bool CanBeApplied(TileType[,] decoratedLevel, Room room);
        public abstract void Apply(TileType[,] decoratedLevel, Room room, Transform parent, long seed);
    }
}