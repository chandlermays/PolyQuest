/*---------------------------
| File: RecordingDialogueEvent.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Dialogues;

namespace PolyQuest.Tests
{
    // A dialogue event that writes its label to a shared log, so tests can see which events fired and in what order.
    internal sealed class RecordingDialogueEvent : DialogueEvent
    {
        public List<string> Log;
        public string Label;
        public GameObject LastInstigator;
        public GameObject LastTarget;

        public override void Execute(GameObject instigator, GameObject target)
        {
            LastInstigator = instigator;
            LastTarget = target;
            Log.Add(Label);
        }
    }
}