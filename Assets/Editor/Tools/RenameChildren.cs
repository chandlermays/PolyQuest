using UnityEngine;
using UnityEditor;
//---------------------------------

namespace PolyQuest.Edit
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Provides a Unity Editor window to batch-rename all children of selected GameObjects.   *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Displays a custom editor window for entering a base name.                             *
     *      - Renames each child of all selected GameObjects in the hierarchy.                      *
     *      - Appends a unique index to each child’s name for clarity and organization.             *
     *      - Integrates with the Unity Editor via the Tools menu.                                  *
     * -------------------------------------------------------------------------------------------- */
    public class RenameChildren : EditorWindow
    {
        private string m_baseName = "GameObject";

        /*-------------------------------------------------------------------
        | --- ShowWindow: Creates and shows the 'RenameChildren' window --- |
        -------------------------------------------------------------------*/
        [MenuItem("Tools/Rename Selected Object's Children")]
        private static void ShowWindow()
        {
            GetWindow<RenameChildren>("Rename Children");
        }

        /*--------------------------------------------------------------
        | --- OnGUI: Draws the GUI for the 'RenameChildren' window --- |
        --------------------------------------------------------------*/
        private void OnGUI()
        {
            GUILayout.Label("Base Title for Children", EditorStyles.boldLabel);
            m_baseName = EditorGUILayout.TextField("Base Title", m_baseName);

            if (GUILayout.Button("Rename Children"))
            {
                RenameSelectedChildren(m_baseName);
            }
        }

        /*--------------------------------------------------------------------------
        | --- RenameSelectedChildren: Renames all children of selected objects --- |
        --------------------------------------------------------------------------*/
        private static void RenameSelectedChildren(string baseName)
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
}