using UnityEngine;
using UnityEngine.UI;
//---------------------------------

// NOTE FOR LATER: Consolidate Experience and Health bar to be a single, parameteried class, or extract repeated functionality to parent class

namespace PolyQuest
{
    public class HealthBar : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private Image m_healthBarFill;
        [SerializeField] private HealthComponent m_healthComponent;

        private float m_maxHealth;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_maxHealth = m_healthComponent.GetComponent<Stats>().GetHealth();
            m_healthBarFill.fillAmount = 1f;
            UpdateHealthBar();
            m_healthComponent.OnHealthChanged += UpdateHealthBar;
        }

        /*---------------------------------------------------------------------
        | --- OnDestroy: Called when this MonoBehaviour will be destroyed --- |
        ---------------------------------------------------------------------*/
        private void OnDestroy()
        {
            // Unsubscribe from the Event
            if (m_healthComponent != null)
            {
                m_healthComponent.OnHealthChanged -= UpdateHealthBar;
            }
        }

        /*-------------------------------------------------------------------------------------
        | --- UpdateHealthBar: Adjust the Fill Amount to the Current Health of the Entity --- |
        -------------------------------------------------------------------------------------*/
        private void UpdateHealthBar()
        {
            if (m_healthBarFill != null)
            {
                float healthPercentage = Mathf.Clamp(m_healthComponent.CurrentHealth / m_maxHealth, 0f, 1f);
                m_healthBarFill.fillAmount = healthPercentage;

                if (m_healthBarFill.fillAmount <= 0f)
                {
                    Destroy(gameObject);   // Destroy the Health Bar when the Entity is Dead
                }
            }
        }
    }
}