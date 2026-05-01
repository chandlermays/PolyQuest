/*---------------------------
File: Conjunction.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;
using PolyQuest.Tools;

namespace PolyQuest.Dialogues
{
    public class DialogueHistory : MonoBehaviour, IConditionChecker, ISaveable
    {
        private readonly HashSet<string> m_spokenTo = new();

        /*------------------------------------------------------------------
        | --- RecordConversation: Mark an NPC as having been spoken to --- |
        ------------------------------------------------------------------*/
        public void RecordConversation(string npcID)
        {
            m_spokenTo.Add(npcID);
        }

        /*---------------------------------------------------------------
        | --- HasSpokenTo: Check if the player has spoken to an NPC --- |
        ---------------------------------------------------------------*/
        public bool HasSpokenTo(string npcID)
        {
            return m_spokenTo.Contains(npcID);
        }

        /*-------------------------------------------------------------
        | --- Evaluate: Evaluate kHasSpokenTo condition predicate --- |
        -------------------------------------------------------------*/
        public bool? Evaluate(ConditionType predicate, string[] parameters)
        {
            if (predicate != ConditionType.kHasSpokenTo)
                return null;

            if (parameters == null || parameters.Length == 0)
                return null;

            return m_spokenTo.Contains(parameters[0]);
        }

        /*---------------------------------------------------------------------
        | --- CaptureState: Capture the dialogue history state for saving --- |
        ---------------------------------------------------------------------*/
        public JToken CaptureState()
        {
            return JToken.FromObject(new List<string>(m_spokenTo));
        }

        /*--------------------------------------------------------------------------
        | --- RestoreState: Restore the dialogue history state from saved data --- |
        --------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            m_spokenTo.Clear();
            foreach (string id in state.ToObject<List<string>>())
            {
                m_spokenTo.Add(id);
            }
        }
    }
}