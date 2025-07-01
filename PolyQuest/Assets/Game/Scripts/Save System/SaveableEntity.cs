using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
//---------------------------------

namespace PolyQuest.Saving
{
    using SaveState = Dictionary<string, object>;

    [ExecuteAlways]
    public class SaveableEntity : MonoBehaviour
    {
        [SerializeField] private string m_uniqueIdentifier = "";
        private static readonly Dictionary<string, SaveableEntity> m_idGlossary = new();

        private ISaveable[] m_saveables;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_saveables = GetComponents<ISaveable>();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            SaveSystem.RegisterSaveableEntity(this);
        }

        /*------------------------------------------------------------------------
        | --- OnDisable: Called when the object becomes disabled or inactive --- |
        ------------------------------------------------------------------------*/
        private void OnDisable()
        {
            SaveSystem.UnregisterSaveableEntity(this);
        }

        /*------------------------------------------------------------------------
        | --- GetUniqueIdentifier: Returns a Unique Identifier for an Entity --- |
        ------------------------------------------------------------------------*/
        public string GetUniqueIdentifier()
        {
            return m_uniqueIdentifier;
        }

        /*----------------------------------------------------------------
        | --- CaptureState: Captures the current State of the Entity --- |
        ----------------------------------------------------------------*/
        public object CaptureState()
        {
            SaveState state = new();
            foreach (ISaveable saveable in m_saveables)
            {
                state[saveable.GetType().ToString()] = saveable.CaptureState();
            }
            return state;
        }

        /*---------------------------------------------------------------
        | --- RestoreState: Restores the Entity to a previous State --- |
        ---------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            SaveState stateDict = (SaveState)state;
            foreach (ISaveable saveable in m_saveables)
            {
                string typeString = saveable.GetType().ToString();
                if (stateDict.ContainsKey(typeString))
                {
                    saveable.RestoreState(stateDict[typeString]);
                }
            }
        }

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

#if UNITY_EDITOR
            SerializedObject serializedObject = new(this);
            SerializedProperty property = serializedObject.FindProperty(nameof(m_uniqueIdentifier));

            if (string.IsNullOrEmpty(property.stringValue) || !IsUnique(property.stringValue))
            {
                property.stringValue = System.Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
            }

            m_idGlossary[property.stringValue] = this;
#endif
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
    }
}