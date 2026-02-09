using UnityEngine;
using UnityEngine.UI;
//---------------------------------
using PolyQuest.Attributes;

// NOTE FOR LATER: Consolidate Experience and Health bar to be a single, parameteried class, or extract repeated functionality to parent class

namespace PolyQuest.UI.HUD
{
    /* ------------------------------------------------------------------------------------------------
     * Role: Displays the player's current experience as a UI bar.                                     *
     *                                                                                                 *
     * Responsibilities:                                                                               *
     *      - Retrieves and displays the current and maximum experience from the Experience component. *
     *      - Updates the experience bar fill amount when experience changes.                          *
     *      - Resets the experience bar when the player levels up.                                     *
     *      - Subscribes to and unsubscribes from experience and level-up events.                      *
     * ----------------------------------------------------------------------------------------------- */

    public class ExperienceBar : MonoBehaviour
    {
        [Header("Experience Bar Settings")]
        [SerializeField] private Experience m_experienceComponent;
        private Slider m_experienceBarFill;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_experienceComponent, nameof(m_experienceComponent));

            m_experienceBarFill = GetComponent<Slider>();
            Utilities.CheckForNull(m_experienceBarFill, nameof(m_experienceBarFill));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when this object becomes enabled or active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_experienceComponent.OnExperienceChanged += UpdateExperienceBar;
            m_experienceComponent.OnLevelUp += UpdateExperienceBar;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_experienceComponent.OnExperienceChanged -= UpdateExperienceBar;
            m_experienceComponent.OnLevelUp -= UpdateExperienceBar;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            UpdateExperienceBar();
        }

        /*---------------------------------------------------------------------------------------------
        | --- UpdateExperienceBar: Adjust the Fill Amount to the Current Experience of the Entity --- |
        ---------------------------------------------------------------------------------------------*/
        private void UpdateExperienceBar()
        {
            float currentLevelExp = m_experienceComponent.GetCurrentLevelXP;
            float maxLevelExp = m_experienceComponent.GetCurrentLevelXPRequired;

            float experiencePercentage = maxLevelExp > 0 ? Mathf.Clamp(currentLevelExp / maxLevelExp, 0f, 1f) : 0f;
            m_experienceBarFill.value = experiencePercentage;
        }
    }
}