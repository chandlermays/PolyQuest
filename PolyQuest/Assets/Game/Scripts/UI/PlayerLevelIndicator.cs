using UnityEngine;
using UnityEngine.UI;
//---------------------------------

namespace PolyQuest
{
    public class PlayerLevelIndicator : MonoBehaviour
    {
        [Header("Level Display Settings")]
        [SerializeField] private Text m_levelText;
        [SerializeField] private Experience m_experienceComponent;

        private Stats m_stats;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            Utilities.CheckForNull(m_experienceComponent, nameof(m_experienceComponent));

            m_stats = m_experienceComponent.Stats;
            if (m_stats == null)
            {
                Debug.LogError("Stats component is not found on the Experience component.");
                return;
            }

            UpdateLevelText();
            m_experienceComponent.OnLevelUp += UpdateLevelText;
        }

        /*---------------------------------------------------------------------
        | --- OnDestroy: Called when this MonoBehaviour will be destroyed --- |
        ---------------------------------------------------------------------*/
        private void OnDestroy()
        {
            // Unsubscribe from the Event
            if (m_experienceComponent != null)
            {
                m_experienceComponent.OnLevelUp -= UpdateLevelText;
            }
        }

        /*--------------------------------------------------------------
        | --- UpdateLevelText: Update the Text to the Current Level --- |
        --------------------------------------------------------------*/
        private void UpdateLevelText()
        {
            if (m_levelText != null)
            {
                m_levelText.text = $"{m_stats.Level}";
            }
        }
    }
}