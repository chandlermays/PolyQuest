/*---------------------------
| File: ThrowingSaveable.cs
| Author: Chandler Mays
----------------------------*/
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    // A saveable component whose RestoreState throws, standing in for a component with a bug.
    public class ThrowingSaveable : MonoBehaviour, ISaveable
    {
        /*-------------------------------------------------
        | --- CaptureState: Saves a placeholder value --- |
        -------------------------------------------------*/
        public JToken CaptureState() => new JValue(1);

        /*-----------------------------------------------------------------------------
        | --- RestoreState: Always throws, standing in for a component with a bug --- |
        -----------------------------------------------------------------------------*/
        public void RestoreState(JToken state) => throw new InvalidOperationException("ThrowingSaveable always fails to restore");
    }
}