using UnityEngine;
using UnityEditor;
//---------------------------------

public class RenameChildren : EditorWindow
{
    private string baseName = "GameObject";

    [MenuItem("Tools/Rename Selected Object's Children")]
    static void ShowWindow()
    {
        GetWindow<RenameChildren>("Rename Children");
    }

    void OnGUI()
    {
        GUILayout.Label("Base Name for Children", EditorStyles.boldLabel);
        baseName = EditorGUILayout.TextField("Base Name", baseName);

        if (GUILayout.Button("Rename Children"))
        {
            RenameSelectedChildren(baseName);
        }
    }

    static void RenameSelectedChildren(string baseName)
    {
        foreach (Transform parent in Selection.transforms)
        {
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);
                child.name = $"{baseName} ({i + 1})";
            }
        }
    }
}