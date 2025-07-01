using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PolyQuest
{
    [System.Serializable]
    public class ActionTriggerPair
    {
        public string Action;
        public UnityEvent OnTrigger;
    }

    public class DialogueEventTrigger : MonoBehaviour
    {
        [SerializeField] private List<ActionTriggerPair> m_actionTriggerPairs = new();

        /*----------------------------------------------------------------------------
        | --- Trigger: Triggers the UnityEvent associated with the action string --- |
        ----------------------------------------------------------------------------*/
        public void Trigger(string action)
        {
            foreach (var pair in m_actionTriggerPairs)
            {
                if (pair.Action == action)
                {
                    pair.OnTrigger?.Invoke();
                    return;
                }
            }
        }
    }
}