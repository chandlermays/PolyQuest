/*---------------------------
File: HealthBar.cs
Author: Chandler Mays
----------------------------*/
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
        [SerializeField] private CanvasGroup m_healthBarRoot;
        [SerializeField] private Image m_healthBarFill;
        [SerializeField] private bool m_alwaysVisible = false;

        private BaseStats m_baseStats;

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
            m_healthComponent.OnCombatEntered += ShowHealthBar;
            m_healthComponent.OnCombatExited += HideHealthBar;

            if (!m_alwaysVisible)
            {
                HideHealthBar();
            }
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_healthComponent.OnHealthChanged -= UpdateHealthBar;
            m_healthComponent.OnCombatEntered -= ShowHealthBar;
            m_healthComponent.OnCombatExited -= HideHealthBar;
        }

        /*-------------------------------------------------------------------------------------
        | --- UpdateHealthBar: Adjust the Fill Amount to the Current Health of the Entity --- |
        -------------------------------------------------------------------------------------*/
        private void UpdateHealthBar()
        {
            float maxHealth = m_baseStats.GetHealth();
            if (maxHealth <= 0f)
                return;

            float healthPercentage = Mathf.Clamp(m_healthComponent.CurrentHealth / maxHealth, 0f, 1f);
            m_healthBarFill.fillAmount = healthPercentage;

            if (m_healthBarFill.fillAmount <= 0f)
            {
                if (m_healthBarRoot != null)
                {
                    m_healthBarRoot.alpha = 0f;
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        /*------------------------------------------------------------------------------
        | --- ShowHealthBar: Displays the health bar when the entity enters combat --- |
        ------------------------------------------------------------------------------*/
        private void ShowHealthBar()
        {
            if (!m_alwaysVisible)
            {
                m_healthBarRoot.alpha = 1f;
                UpdateHealthBar();
            }
        }

        /*--------------------------------------------------------------------------
        | --- HideHealthBar: Hides the health bar when the entity exits combat --- |
        --------------------------------------------------------------------------*/
        private void HideHealthBar()
        {
            if (!m_alwaysVisible)
            {
                m_healthBarRoot.alpha = 0f;
            }
        }
    }
}