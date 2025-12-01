using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Saving
{
    [ExecuteAlways]
    public class JsonSaveableEntity : MonoBehaviour
    {
        [SerializeField] private string m_uniqueIdentifier = "";
        [SerializeField] private bool m_lockIdentifier = false;

        private static readonly Dictionary<string, JsonSaveableEntity> m_idGlossary = new();

        private IJsonSaveable[] m_saveables;
        public string UID => m_uniqueIdentifier;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_saveables = GetComponents<IJsonSaveable>();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            //   SaveSystem.RegisterSaveableEntity(this);
        }

        /*------------------------------------------------------------------------
        | --- OnDisable: Called when the object becomes disabled or inactive --- |
        ------------------------------------------------------------------------*/
        private void OnDisable()
        {
            //   SaveSystem.UnregisterSaveableEntity(this);
        }

        /*----------------------------------------------------------------
        | --- CaptureState: Captures the current State of the Entity --- |
        ----------------------------------------------------------------*/
        public JToken CaptureAsJToken()
        {
            // early exit if there are no saveables
            if (m_saveables == null || m_saveables.Length == 0)
                return null;

            JObject state = new JObject();
            IDictionary<string, JToken> stateDict = state;

            foreach (IJsonSaveable saveable in m_saveables)
            {
                JToken token = saveable.CaptureJToken();
                stateDict[saveable.GetType().ToString()] = token;
            }
            return state;
        }

        /*---------------------------------------------------------------
        | --- RestoreState: Restores the Entity to a previous State --- |
        ---------------------------------------------------------------*/
        public void RestoreFromJToken(JToken s)
        {
            JObject state = s.ToObject<JObject>();
            IDictionary<string, JToken> stateDict = state;

            foreach (IJsonSaveable saveable in m_saveables)
            {
                string component = saveable.GetType().ToString();
                if (stateDict.ContainsKey(component))
                {
                    saveable.RestoreJToken(stateDict[component]);
                }
            }
        }

#if UNITY_EDITOR
        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            // Only run this logic in Edit Mode
            if (Application.IsPlaying(gameObject))
                return;

            // If the Unique Identifier is already set, we do not need to generate a new one
            if (string.IsNullOrEmpty(gameObject.scene.path))
                return;

            SerializedObject serializedObject = new(this);
            SerializedProperty property = serializedObject.FindProperty(nameof(m_uniqueIdentifier));
            SerializedProperty lockProperty = serializedObject.FindProperty(nameof(m_lockIdentifier));

            // Skip regeneration if locked
            if (lockProperty.boolValue)
            {
                m_idGlossary[property.stringValue] = this;
                return;
            }

            if (string.IsNullOrEmpty(property.stringValue) || !IsUnique(property.stringValue))
            {
                property.stringValue = System.Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
            }

            m_idGlossary[property.stringValue] = this;
        }

        /*-------------------------------------------------------------------------
        | --- IsUnique: Checks the ID Collection if the Entity's ID is Unique --- |
        -------------------------------------------------------------------------*/
        private bool IsUnique(string entity)
        {
            // Check if the ID belongs to this instance (least expensive)
            if (m_idGlossary.TryGetValue(entity, out var existingEntity) && existingEntity == this)
            {
                return true;
            }

            // Check if the ID does not already exist in the glossary
            if (!m_idGlossary.ContainsKey(entity))
            {
                m_idGlossary[entity] = this;
                return true;
            }

            // Check if the existing ID is null (most expensive)
            if (existingEntity == null)
            {
                m_idGlossary[entity] = this;
                return true;
            }

            return false;
        }
#endif
    }
}