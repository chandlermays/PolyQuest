using UnityEngine;
using UnityEngine.UI;
//---------------------------------

// NOTE FOR LATER: Consolidate Experience and Health bar to be a single, parameteried class, or extract repeated functionality to parent class

namespace PolyQuest
{
    public class ExperienceBar : MonoBehaviour
    {
        [Header("Experience Bar Settings")]
        [SerializeField] private Image m_experienceBarFill;
        [SerializeField] private Experience m_experienceComponent;

        private float m_maxExperience;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_maxExperience = m_experienceComponent.GetComponent<Stats>().GetExperience();
            m_experienceBarFill.fillAmount = 0f;
            UpdateExperienceBar();
            m_experienceComponent.OnExperienceChanged += UpdateExperienceBar;
            m_experienceComponent.OnLevelUp += ResetExperienceBar;
        }

        /*---------------------------------------------------------------------
        | --- OnDestroy: Called when this MonoBehaviour will be destroyed --- |
        ---------------------------------------------------------------------*/
        private void OnDestroy()
        {
            // Unsubscribe from the Event
            if (m_experienceComponent != null)
            {
                m_experienceComponent.OnExperienceChanged -= UpdateExperienceBar;
                m_experienceComponent.OnLevelUp -= ResetExperienceBar;
            }
        }

        /*---------------------------------------------------------------------------------------------
        | --- UpdateExperienceBar: Adjust the Fill Amount to the Current Experience of the Entity --- |
        ---------------------------------------------------------------------------------------------*/
        private void UpdateExperienceBar()
        {
            if (m_experienceBarFill != null)
            {
                float experiencePercentage = Mathf.Clamp(m_experienceComponent.CurrentExperience / m_maxExperience, 0f, 1f);
                m_experienceBarFill.fillAmount = experiencePercentage;
            }
        }

        /*------------------------------------------------------------------
        | --- ResetExperienceBar: Reset the Experience Bar on Level Up --- |
        ------------------------------------------------------------------*/
        private void ResetExperienceBar()
        {
            m_maxExperience = m_experienceComponent.GetComponent<Stats>().GetExperience();
            UpdateExperienceBar();
        }
    }
}