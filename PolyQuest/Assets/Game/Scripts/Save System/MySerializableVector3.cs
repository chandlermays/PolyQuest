using UnityEngine;

namespace PolyQuest.Saving
{
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