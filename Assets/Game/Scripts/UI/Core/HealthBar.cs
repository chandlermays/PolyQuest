using UnityEngine;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;

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
        [SerializeField] private Image m_healthBarFill;

        private BaseStats m_baseStats;

        private float m_maxHealth;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));
            Utilities.CheckForNull(m_healthBarFill, nameof(m_healthBarFill));

            m_baseStats = m_healthComponent.GetComponent<BaseStats>();
            Utilities.CheckForNull(m_baseStats, nameof(m_baseStats));
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
            m_maxHealth = m_baseStats.GetHealth();
        }

        /*-------------------------------------------------------------------------------------
        | --- UpdateHealthBar: Adjust the Fill Amount to the Current Health of the Entity --- |
        -------------------------------------------------------------------------------------*/
        private void UpdateHealthBar()
        {
            m_maxHealth = m_baseStats.GetHealth();
            float healthPercentage = Mathf.Clamp(m_healthComponent.CurrentHealth / m_maxHealth, 0f, 1f);
            m_healthBarFill.fillAmount = healthPercentage;

            if (m_healthBarFill.fillAmount <= 0f)
            {
                Destroy(gameObject);   // Destroy the Health Bar when the Entity is Dead
            }
        }
    }
}