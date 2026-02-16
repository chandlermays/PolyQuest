using Newtonsoft.Json.Linq;
using UnityEngine;
using System;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Attributes
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Tracks and manages experience points and level progression for a character.            *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Stores and updates the character's current experience points.                         *
     *      - Handles gaining experience and checks for level-up conditions.                        *
     *      - Triggers events for experience changes and level-ups.                                 *
     *      - Integrates with BaseStats to determine experience required for next level.            *
     *      - Supports saving and restoring experience state.                                       *
     * -------------------------------------------------------------------------------------------- */
    public class Experience : MonoBehaviour, ISaveable
    {
        [SerializeField] private float m_experiencePoints = 0;
        [SerializeField] private ParticleSystem m_levelUpVFX;

        private BaseStats m_stats;
        private PlayerProgression m_playerProgression;

        public event Action OnExperienceChanged;
        public event Action OnLevelUp;

        public BaseStats Stats => m_stats;
        public float ExperiencePoints => m_experiencePoints;

        // ## NOTE: GetCurrentLevelXP and GetCurrentLevelXPRequired should most likely be optimized in some way.
        // --> to be modified at a later time.

        // Get experience for current level only
        public float GetCurrentLevelXP
        {
            get
            {
                if (m_stats.Level <= 1)
                    return m_experiencePoints;

                float previousLevelTotalExp = m_playerProgression.GetExperience(m_stats.Level - 1);
                return m_experiencePoints - previousLevelTotalExp;
            }
        }

        // Get experience required for current level
        public float GetCurrentLevelXPRequired
        {
            get
            {
                if (m_stats.Level <= 1)
                    return m_playerProgression.GetExperience(1);

                float currentLevelTotalExp = m_playerProgression.GetExperience(m_stats.Level);
                float previousLevelTotalExp = m_playerProgression.GetExperience(m_stats.Level - 1);
                return currentLevelTotalExp - previousLevelTotalExp;
            }
        }

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_levelUpVFX, nameof(m_levelUpVFX));

            m_stats = GetComponent<BaseStats>();
            Utilities.CheckForNull(m_stats, nameof(m_stats));

            m_playerProgression = (PlayerProgression)m_stats.Progression;
            Utilities.CheckForNull(m_playerProgression, nameof(m_playerProgression));
        }

        /*-------------------------------------------------------
        | --- GainExperience: Gain Experience from an Event --- |
        -------------------------------------------------------*/
        public void GainExperience(float exp)
        {
            m_experiencePoints += exp;
            OnExperienceChanged?.Invoke();
            CheckForLevelUp();
        }

        /*--------------------------------------------------------------------------
        | --- CaptureState: Save the Current State of the Experience Component --- |
        --------------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            JObject state = new JObject();
            state["experiencePoints"] = m_experiencePoints;
            state["level"] = m_stats.Level;
            return state;
        }

        /*--------------------------------------------------------------------------
        | --- RestoreState: Load the Current State of the Experience Component --- |
        --------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            if (state is JObject stateObj)
            {
                m_experiencePoints = stateObj["experiencePoints"]?.ToObject<float>() ?? 0f;
                int savedLevel = stateObj["level"]?.ToObject<int>() ?? 1;

                m_stats.SetLevel(savedLevel);
            }

            OnExperienceChanged?.Invoke();
        }

        /*--------------------------------------------------------------
        | --- CheckForLevelUp: Check if the Player should Level Up --- |
        --------------------------------------------------------------*/
        private void CheckForLevelUp()
        {
            float experienceToLevelUp = m_playerProgression.GetExperience(m_stats.Level);

            while (m_experiencePoints >= experienceToLevelUp)
            {
                LevelUp();

                // Update the experience goal for the next level
                experienceToLevelUp = m_playerProgression.GetExperience(m_stats.Level);
            }
        }

        /*----------------------------------------------------------------
        | --- LevelUp: Handle the Level Up Process for the Character --- |
        ----------------------------------------------------------------*/
        private void LevelUp()
        {
            m_stats.LevelUp();
            OnLevelUp?.Invoke();
            m_levelUpVFX.Play();
        }
    }
}