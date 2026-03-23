using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.UI.Core
{
    /* --------------------------------------------------------------------------------------------
     * Role: Configures and manages cursor appearance for different gameplay contexts.             *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Defines cursor types and their associated textures and hotspots.                     *
     *      - Stores mappings between gameplay actions and cursor visuals.                         *
     *      - Provides access to cursor mappings for use by UI and gameplay systems.               *
     * ------------------------------------------------------------------------------------------- */
    [CreateAssetMenu(fileName = "CursorSettings", menuName = "PolyQuest/CursorSettings")]
    public class CursorSettings : ScriptableObject
    {
        public enum CursorType
        {
            kNone,
            kMovement,
            kTargeting,
            kDialogue,
            kPickup,
            kShop
        }

        [System.Serializable]
        public struct CursorMapping
        {
            public CursorType m_type;
            public Texture2D m_texture;
            public Vector2 m_hotspot;

            public static readonly CursorMapping Default = new()
            {
                m_type = CursorType.kNone,
                m_texture = null,
                m_hotspot = Vector2.zero
            };
        }

        [SerializeField]
        private CursorMapping[] m_cursorMappings;

        private Dictionary<CursorType, CursorMapping> m_cursorMappingDictionary;

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_cursorMappingDictionary = new Dictionary<CursorType, CursorMapping>();
            foreach (var mapping in m_cursorMappings)
            {
                m_cursorMappingDictionary[mapping.m_type] = mapping;
            }
        }

        /*-----------------------------------------------------------------------
        | --- GetCursorMapping: Returns the cursor mapping for a given type --- |
        -----------------------------------------------------------------------*/
        public CursorMapping GetCursorMapping(CursorType type)
        {
            if (m_cursorMappingDictionary.TryGetValue(type, out var mapping))
            {
                return mapping;
            }

            return CursorMapping.Default;
        }
    }
}