using System.Collections.Generic;
using UnityEngine;

namespace PolyQuest
{
    [CreateAssetMenu(fileName = "New Quest", menuName = "PolyQuest/Quest", order = 0)]
    public class Quest : ScriptableObject
    {
        [SerializeField] private List<Objective> m_objectives = new();
        [SerializeField] private List<Reward> m_rewards = new();

        private Dictionary<string, Objective> m_objectiveLookup = new();

        [System.Serializable]
        public class Reward
        {
            public InventoryItem m_item;
            [Min(1)] public int m_amount;
        }

        [System.Serializable]
        public class Objective
        {
            public string m_identifier;
            public string m_description;
        }

        public string GetTitle() { return name; }
        public List<Objective> GetObjectives() { return m_objectives; }
        public int GetObjectiveCount() { return m_objectives.Count; }
        public List<Reward> GetRewards() { return m_rewards; }

        /*----------------------------------------------------------------------
        | --- OnEnable: Called when this object becomes enabled and active --- |
        ----------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_objectiveLookup.Clear();

            foreach (var obj in m_objectives)
            {
                if (!string.IsNullOrEmpty(obj.m_identifier))
                {
                    m_objectiveLookup[obj.m_identifier] = obj;
                }
            }
        }

        /*---------------------------------------------------------------------------
        | --- GetByName: Retrieve a Quest by its name from the Resources folder --- |
        ---------------------------------------------------------------------------*/
        public static Quest GetByName(string questName)
        {
            foreach (Quest quest in Resources.LoadAll<Quest>(""))
            {
                if (quest.name == questName)
                {
                    return quest;
                }
            }
            return null;
        }

        /*-------------------------------------------------------------------------------
        | --- HasObjective: Check if the quest has a specific objective by its name --- |
        -------------------------------------------------------------------------------*/
        public bool HasObjective(string objective)
        {
            foreach (Objective obj in m_objectives)
            {
                if (obj.m_identifier == objective)
                {
                    return true;
                }
            }
            return false;
        }
    }
}