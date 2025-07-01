using UnityEngine;
using System;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest
{
    public class Experience : MonoBehaviour, ISaveable
    {
        [SerializeField] private float m_experiencePoints = 0;
        [SerializeField] private ParticleSystem m_levelUpVFX;

        private Stats m_stats;
        public Stats Stats => m_stats;

        public event Action OnExperienceChanged;
        public event Action OnLevelUp;

        public float CurrentExperience => m_experiencePoints;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_stats = GetComponent<Stats>();
            Utilities.CheckForNull(m_stats, nameof(m_stats));
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

        /*--------------------------------------------------------------
        | --- CheckForLevelUp: Check if the Player should Level Up --- |
        --------------------------------------------------------------*/
        private void CheckForLevelUp()
        {
            float experienceToLevelUp = m_stats.GetExperience();

            while (m_experiencePoints >= experienceToLevelUp)
            {
                m_stats.LevelUp();
                OnLevelUp?.Invoke();
                m_levelUpVFX.Play();

                // Update the experience goal for the next level
                experienceToLevelUp = m_stats.GetExperience();
            }
        }

        /*--------------------------------------------------------------------------
        | --- CaptureState: Save the Current State of the Experience Component --- |
        --------------------------------------------------------------------------*/
        public object CaptureState()
        {
            return m_experiencePoints;
        }

        /*--------------------------------------------------------------------------
        | --- RestoreState: Load the Current State of the Experience Component --- |
        --------------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            m_experiencePoints = (float)state;
            OnExperienceChanged?.Invoke();
            CheckForLevelUp();
        }
    }
}