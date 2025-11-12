 using UnityEngine;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Attributes;

// NOTE FOR LATER: Consolidate Experience and Health bar to be a single, parameteried class, or extract repeated functionality to parent class

namespace PolyQuest.UI.HUD
{
    /* --------------------------------------------------------------------------------------------
     * Role: Displays the player's or entity's current health as a UI bar.                         *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Retrieves and displays the current and maximum health from the HealthComponent.      *
     *      - Updates the health bar fill amount when health changes.                              *
     *      - Destroys the health bar UI when the entity dies.                                     *
     *      - Subscribes to and unsubscribes from health change events.                            *
     * ------------------------------------------------------------------------------------------- */
    public class HealthBar : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private HealthComponent m_healthComponent;
        private Image m_healthBarFill;

        private float m_maxHealth;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_healthBarFill = GetComponent<Image>();
            Utilities.CheckForNull(m_healthBarFill, nameof(m_healthBarFill));
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when this object becomes enabled or active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_healthComponent.OnHealthChanged += UpdateHealthBar;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_healthComponent.OnHealthChanged -= UpdateHealthBar;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_maxHealth = m_healthComponent.GetComponent<BaseStats>().GetHealth();
        }

        /*-------------------------------------------------------------------------------------
        | --- UpdateHealthBar: Adjust the Fill Amount to the Current Health of the Entity --- |
        -------------------------------------------------------------------------------------*/
        private void UpdateHealthBar()
        {
            if (m_maxHealth == 0)
            {
                m_maxHealth = m_healthComponent.GetComponent<BaseStats>().GetHealth();
            }

            float healthPercentage = Mathf.Clamp(m_healthComponent.CurrentHealth / m_maxHealth, 0f, 1f);
            m_healthBarFill.fillAmount = healthPercentage;

            if (m_healthBarFill.fillAmount <= 0f)
            {
                Destroy(gameObject);   // Destroy the Health Bar when the Entity is Dead
            }
        }
    }
}