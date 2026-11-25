/*---------------------------
| File: WorldFlag.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Quests
{
    /* ---------------------------------------------------------------------------------------------
     * Role: One fact about the world (camp_entered, camp_cleared) that stays true once it happens, *
     *       whether or not a quest asks for it yet.                                                *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Holds the stable ID that saves and kWorldFlag conditions refer to.                    *
     *      - Looks flags up by ID from the Resources folder.                                       *
     * -------------------------------------------------------------------------------------------- */
    [CreateAssetMenu(menuName = "PolyQuest/Quests/World Flag", fileName = "New World Flag")]
    public class WorldFlag : ScriptableObject
    {
        [Tooltip("A stable, unique ID (e.g. camp_cleared). Saves and kWorldFlag conditions use it, so don't change it once it's in use.")]
        [SerializeField] private string m_flagID;

        private static Dictionary<string, WorldFlag> m_flagRegistry;

        public string ID => m_flagID;

        /*------------------------------------------------------------------------------------
        | --- GetByID: Retrieve a WorldFlag by its ID from the Resources folder, or null --- |
        ------------------------------------------------------------------------------------*/
        public static WorldFlag GetByID(string flagID)
        {
            if (string.IsNullOrWhiteSpace(flagID))
                return null;

            if (m_flagRegistry == null)
            {
                m_flagRegistry = new Dictionary<string, WorldFlag>();
                foreach (WorldFlag flag in Resources.LoadAll<WorldFlag>(""))
                {
                    if (!string.IsNullOrWhiteSpace(flag.m_flagID) && !m_flagRegistry.ContainsKey(flag.m_flagID))
                    {
                        m_flagRegistry[flag.m_flagID] = flag;
                    }
                }
            }

            return m_flagRegistry.TryGetValue(flagID, out WorldFlag found) ? found : null;
        }
    }
}