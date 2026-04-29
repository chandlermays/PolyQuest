/*---------------------------
File: ManaBar.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Attributes;
using PolyQuest.Components;

// April 20th: Consolidate Experience and Health bar to be a single, parameteried class, or extract repeated functionality to parent class

namespace PolyQuest.UI.HUD
{
    /* --------------------------------------------------------------------------------------------
     * Role: Displays the player's or entity's current mana as a UI bar.                          *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Retrieves and displays the current and maximum mana from the ManaComponent.          *
     *      - Updates the mana bar fill amount when mana changes.                                  *
     *      - Subscribes to and unsubscribes from mana change events.                              *
     * ------------------------------------------------------------------------------------------- */
    public class ManaBar : MonoBehaviour
    {
        [Header("Mana Bar Settings")]
        [SerializeField] private ManaComponent m_manaComponent;
        private Image m_manaBarFill;

        private BaseStats m_baseStats;
        private float m_maxMana;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_manaBarFill = GetComponent<Image>();
            Utilities.CheckForNull(m_manaBarFill, nameof(m_manaBarFill));
            Utilities.CheckForNull(m_manaComponent, nameof(m_manaComponent));

            m_baseStats = m_manaComponent.GetComponent<BaseStats>();
            Utilities.CheckForNull(m_baseStats, nameof(m_baseStats));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when this object becomes enabled or active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_manaComponent.OnManaChanged += UpdateManaBar;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_manaComponent.OnManaChanged -= UpdateManaBar;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_maxMana = m_baseStats.GetMana();
        }

        /*---------------------------------------------------------------------------------
        | --- UpdateManaBar: Adjust the Fill Amount to the Current Mana of the Entity --- |
        ---------------------------------------------------------------------------------*/
        private void UpdateManaBar()
        {
            if (m_maxMana == 0)
            {
                m_maxMana = m_baseStats.GetMana();
            }

            float manaPercentage = Mathf.Clamp(m_manaComponent.CurrentMana / m_maxMana, 0f, 1f);
            m_manaBarFill.fillAmount = manaPercentage;
        }
    }
}