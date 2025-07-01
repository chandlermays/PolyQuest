using UnityEngine;

public static class Utilities
{
    public static void CheckForNull(Object component, string componentName)
    {
        if (component == null)
        {
            Debug.LogError($"{componentName} is null. Please ensure it is assigned in the inspector.");
        }
    }
}