using UnityEngine;
using System.Collections.Generic;
//---------------------------------

namespace PolyQuest
{
    [CreateAssetMenu(fileName = "CursorSettings", menuName = "PolyQuest/CursorSettings")]
    public class CursorSettings : ScriptableObject
    {
        public enum CursorType
        {
            kNormal,
            kMovement,
            kTargeting,
            kInteractable,
            kDialogue
        }

        [System.Serializable]
        public struct CursorMapping
        {
            public CursorType m_type;
            public Texture2D m_texture;
            private Vector2 m_hotspot;

            public readonly Vector2 Hotspot => m_hotspot;

            public static readonly CursorMapping Default = new()
            {
                m_type = CursorType.kNormal,
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
            // Initialize the dictionary when the ScriptableObject is loaded
            m_cursorMappingDictionary = new Dictionary<CursorType, CursorMapping>();
            foreach (var mapping in m_cursorMappings)
            {
                m_cursorMappingDictionary[mapping.m_type] = mapping;
            }
        }

        /*-----------------------------------------------------------------------
        | --- GetCursorMapping: Returns the cursor mapping for a given m_type --- |
        -----------------------------------------------------------------------*/
        public CursorMapping GetCursorMapping(CursorType type)
        {
            if (m_cursorMappingDictionary.TryGetValue(type, out var mapping))
            {
                return mapping;
            }

            // Return the default mapping as a fallback
            return CursorMapping.Default;
        }
    }
}