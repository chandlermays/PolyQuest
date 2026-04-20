/*---------------------------
File: DialogueEvent.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.Dialogues
{
    public abstract class DialogueEvent : ScriptableObject
    {
        public abstract void Execute(GameObject instigator, GameObject target);
    }
}