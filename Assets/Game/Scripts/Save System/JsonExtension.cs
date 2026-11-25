/*---------------------------
| File: JsonExtension.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Saving
{
    using SaveState = IDictionary<string, JToken>;

    public static class JsonExtension
    {
        /*------------------------------------------------------------
        | --- ToToken: Converts a Vector3 to a JToken for saving --- |
        ------------------------------------------------------------*/
        public static JToken ToToken(this Vector3 vector)
        {
            JObject state = new();
            SaveState stateDict = state;
            stateDict["x"] = vector.x;
            stateDict["y"] = vector.y;
            stateDict["z"] = vector.z;
            return state;
        }

        /*--------------------------------------------------------------------
        | --- ToVector3: Converts a JToken back to a Vector3 for loading --- |
        --------------------------------------------------------------------*/
        public static Vector3 ToVector3(this JToken state)
        {
            Vector3 vector = new();
            if (state is JObject jObject)
            {
                SaveState stateDict = jObject;

                if (stateDict.TryGetValue("x", out JToken x))
                {
                    vector.x = x.ToObject<float>();
                }

                if (stateDict.TryGetValue("y", out JToken y))
                {
                    vector.y = y.ToObject<float>();
                }

                if (stateDict.TryGetValue("z", out JToken z))
                {
                    vector.z = z.ToObject<float>();
                }
            }
            return vector;
        }

        /*-------------------------------------------------------------------------------------------------------
        | --- TryToVector3: Reads a saved Vector3; false unless it's an object with a number for x, y and z --- |
        -------------------------------------------------------------------------------------------------------*/
        public static bool TryToVector3(this JToken state, out Vector3 vector)
        {
            vector = default;
            if (state is not JObject axes)
                return false;

            foreach (string axis in new[] { "x", "y", "z" })
            {
                JToken value = axes[axis];
                if (value == null || (value.Type != JTokenType.Float && value.Type != JTokenType.Integer))
                    return false;
            }

            vector = new Vector3(axes["x"].Value<float>(), axes["y"].Value<float>(), axes["z"].Value<float>());
            return true;
        }
    }
}