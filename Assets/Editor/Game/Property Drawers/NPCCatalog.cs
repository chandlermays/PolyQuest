/*---------------------------
| File: NpcCatalog.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//---------------------------------
using PolyQuest.Dialogues;

namespace PolyQuest.Edit
{
    // Every NPC in the game's prefabs, found by the AIDialogueHandler that carries its NPC ID.
    // Shared by the kHasSpokenTo dropdown, the condition warning and the content tests.
    [InitializeOnLoad]
    public static class NpcCatalog
    {
        public readonly struct Entry
        {
            public readonly string ID;
            public readonly string DisplayName;
            public readonly string PrefabPath;

            public Entry(string id, string displayName, string prefabPath)
            {
                ID = id;
                DisplayName = displayName;
                PrefabPath = prefabPath;
            }
        }

        private const string kGameFolder = "Assets/Game";

        private static List<Entry> s_cached;

        /*--------------------------------------------------------------------------------
        | --- NpcCatalog: Drops the cached list whenever the project changes on disk --- |
        --------------------------------------------------------------------------------*/
        static NpcCatalog()
        {
            EditorApplication.projectChanged += () => s_cached = null;
        }

        /*-----------------------------------------------------------------------------------
        | --- All: Every NPC, cached for the drawer, which asks for it on every repaint --- |
        -----------------------------------------------------------------------------------*/
        public static IReadOnlyList<Entry> All => s_cached ??= FindAll();

        /*------------------------------------------------------------
        | --- Exists: Whether an NPC with this ID is in the game --- |
        ------------------------------------------------------------*/
        public static bool Exists(string id)
        {
            foreach (Entry entry in All)
            {
                if (entry.ID == id)
                    return true;
            }
            return false;
        }

        /*-------------------------------------------------------------------------------
        | --- FindAll: Scans the game's prefabs for NPCs, bypassing the cached list --- |
        -------------------------------------------------------------------------------*/
        public static List<Entry> FindAll()
        {
            List<Entry> entries = new();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { kGameFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                foreach (AIDialogueHandler npc in prefab.GetComponentsInChildren<AIDialogueHandler>(true))
                {
                    // An NPC placed inside another prefab, or a variant of one, is that same NPC: count it where it's defined
                    if (PrefabUtility.GetCorrespondingObjectFromSource(npc) != null)
                        continue;

                    entries.Add(new Entry(npc.NpcID, npc.SpeakerName, path));
                }
            }
            return entries;
        }
    }
}