using UnityEngine;
//---------------------------------

namespace PolyQuest.Saving
{
    /* --------------------------------------------------------------------------------------------
     * Role: Provides a serializable representation of a Vector3 for use in the save system.       *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores the x, y, and z components of a Vector3 in a serializable format.             *
     *      - Converts between Unity's Vector3 and the serializable structure.                     *
     *      - Enables reliable saving and loading of position data and other vector values.        *
     * ------------------------------------------------------------------------------------------- */
    [System.Serializable]
    public class MySerializableVector3
    {
        private readonly float x;
        private readonly float y;
        private readonly float z;

        public MySerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}