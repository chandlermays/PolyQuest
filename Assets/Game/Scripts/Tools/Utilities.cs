using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    /* --------------------------------------------------------------------------------------------
     * Role: Provides general-purpose utility methods for use throughout the project.              *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Offers static helper functions to simplify common programming tasks.                 *
     *      - Includes methods for error checking and debugging support.                           *
     *      - Promotes code reuse and consistency across scripts.                                  *
     * ------------------------------------------------------------------------------------------- */
    public static class Utilities
    {
        /*----------------------------------------------------------------
        | --- CheckForNull: Utility method to check for null objects --- |
        ----------------------------------------------------------------*/
        public static void CheckForNull<T>(T obj, string name)
        {
            // Handle custom classes
            if (obj == null)
            {
                Debug.LogError($"{name} is null. Please ensure it is assigned in the inspector.");
            }
            // Handle UnityEngine.Object types
            else if (obj is Object unityObj)
            {
                if (unityObj == null)
                {
                    Debug.LogError($"{name} is not a valid Object. Please ensure it is assigned in the inspector.");
                }
                return;
            }
        }

    }
}