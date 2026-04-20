/*---------------------------
File: MissingScriptReferences.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Edit
{
    public static class MissingScriptReferences
    {
        private const string kSearchPath = "Assets/Game";

        /*------------------------------------------------------------------------------
        | --- ScanAllPrefabs: Scans all prefabs in the project for missing scripts --- |
        ------------------------------------------------------------------------------*/
        [MenuItem("Tools/Scan All Prefabs for Missing Scripts")]
        public static void ScanAllPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { kSearchPath });

            if (guids.Length == 0)
            {
                Debug.LogWarning($"No prefabs found in {kSearchPath}.");
                return;
            }

            int totalMissing = 0;
            int prefabsScanned = 0;
            List<string> problematicPrefabs = new();

            Debug.Log($"Scanning {guids.Length} prefabs in {kSearchPath}...");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                    continue;

                ++prefabsScanned;
                int missingInPrefab = CountMissingScripts(prefab);

                if (missingInPrefab > 0)
                {
                    totalMissing += missingInPrefab;
                    problematicPrefabs.Add(path);
                    Debug.LogError($"[ {missingInPrefab} ] Missing script(s) in: {path}", prefab);
                }
            }

            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"Scan Complete: {prefabsScanned}/{guids.Length} prefabs scanned");

            if (totalMissing == 0)
            {
                Debug.Log($"✓ No missing scripts found!");
            }
            else
            {
                Debug.LogError($"✗ Found {totalMissing} missing script reference(s) across {problematicPrefabs.Count} prefab(s)");
            }
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        /*-----------------------------------------------------------------
        | --- CountMissingScripts: Counts missing scripts in a prefab --- |
        -----------------------------------------------------------------*/
        private static int CountMissingScripts(GameObject prefab)
        {
            int count = 0;
            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            foreach (Component component in components)
            {
                if (component == null)
                    ++count;
            }

            return count;
        }

        /*-----------------------------------------------------------------------
        | --- ScanCurrentScene: Scans the current scene for missing scripts --- |
        -----------------------------------------------------------------------*/
        [MenuItem("Tools/Scan Current Scene for Missing Scripts")]
        public static void ScanCurrentScene()
        {
            GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int totalMissing = 0;
            List<GameObject> problematicObjects = new();

            Debug.Log($"Scanning {sceneObjects.Length} GameObjects in current scene...");

            foreach (GameObject gameObject in sceneObjects)
            {
                Component[] components = gameObject.GetComponents<Component>();

                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        ++totalMissing;

                        if (!problematicObjects.Contains(gameObject))
                        {
                            problematicObjects.Add(gameObject);
                            Debug.LogError($"Missing script on GameObject: {GetGameObjectPath(gameObject)}", gameObject);
                        }
                    }
                }
            }

            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"Scene Scan Complete");

            if (totalMissing == 0)
            {
                Debug.Log($"✓ No missing scripts found in scene!");
            }
            else
            {
                Debug.LogError($"✗ Found {totalMissing} missing script reference(s) on {problematicObjects.Count} GameObject(s)");
            }
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        /*-----------------------------------------------------------------------------------
        | --- GetGameObjectPath: Returns the full path of a GameObject in the hierarchy --- |
        -----------------------------------------------------------------------------------*/
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}