using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    public class Stats : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private CharacterClass m_characterClass;
        [SerializeField] private ClassProgression m_classProgression;

        [Range(1, 99)]
        [SerializeField] private int m_level = 1;

        private HealthComponent m_healthComponent;

        public int Level => m_level;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));
        }

        /*----------------------------------------------------------------------
        | --- GetHealth: Returns the Amount of Health at the Current Level --- |
        ----------------------------------------------------------------------*/
        public float GetHealth()
        {
            return m_classProgression.GetHealth(m_level);
        }

        /*------------------------------------------------------------------------------
        | --- GetExperience: Returns the Amount of Experience at the Current Level --- |
        ------------------------------------------------------------------------------*/
        public float GetExperience()
        {
            return m_classProgression.GetExperience(m_level);
        }

        /*-----------------------------------------------
        | --- LevelUp: Increment the Player's Level --- |
        -----------------------------------------------*/
        public void LevelUp()
        {
            ++m_level;
            m_healthComponent?.ReplenishHealth();
            // Additional logic for increasing attributes would go here...
        }
    }
}