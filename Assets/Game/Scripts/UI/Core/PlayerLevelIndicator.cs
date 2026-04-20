/*---------------------------
File: PlayerLevelIndicator.cs
Author: Chandler Mays
----------------------------*/
using TMPro;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.UI.HUD
{
    /* --------------------------------------------------------------------------------------------
     * Role: Displays the player's current level in the UI.                                        *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Retrieves the player's level from the BaseStats component.                           *
     *      - Updates the level display when the player levels up.                                 *
     *      - Subscribes to and unsubscribes from level-up events.                                 *
     * ------------------------------------------------------------------------------------------- */
    public class PlayerLevelIndicator : MonoBehaviour
    {
        [Header("Level Display Settings")]
        [SerializeField] private Experience m_experienceComponent;

        private TextMeshProUGUI m_levelText;
        private BaseStats m_stats;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_experienceComponent, nameof(m_experienceComponent));

            m_levelText = GetComponent<TextMeshProUGUI>();
            Utilities.CheckForNull(m_levelText, nameof(m_levelText));
        }

        /*-----------------------------------------------------------------------
        | --- OnEnable: Called when the behaviour becomes enabled or active --- |
        -----------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_experienceComponent.OnLevelUp += UpdateLevelText;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_experienceComponent.OnLevelUp -= UpdateLevelText;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_stats = m_experienceComponent.Stats;
            Utilities.CheckForNull(m_stats, nameof(m_stats));

            UpdateLevelText();
        }

        /*---------------------------------------------------------------
        | --- UpdateLevelText: Update the Text to the Current Level --- |
        ---------------------------------------------------------------*/
        private void UpdateLevelText()
        {
            if (m_stats == null)
            {
                m_stats = m_experienceComponent.Stats;
            }
            m_levelText.text = $"{m_stats.Level}";
        }
    }
}