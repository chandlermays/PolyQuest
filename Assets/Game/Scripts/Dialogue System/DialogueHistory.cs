/*---------------------------
| File: DialogueHistory.cs
| Author: Chandler Mays
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

        /*----------------------------------------------------------------------
        | --- RecordConversation: Mark an NPC, by its NPC ID, as spoken to --- |
        ----------------------------------------------------------------------*/
        public void RecordConversation(string npcID)
        {
            if (string.IsNullOrWhiteSpace(npcID))
                return;

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

            if (parameters == null || parameters.Length == 0 || string.IsNullOrWhiteSpace(parameters[0]))
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

        /*---------------------------------------------------------------------------------------------
        | --- RestoreState: Restores the saved history; data that can't be read is left out of it --- |
        ---------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            // The whole save is the wrong shape: ignore it and keep the current history
            if (state is not JArray entries)
                return;

            m_spokenTo.Clear();
            foreach (JToken entry in entries)
            {
                // An entry that isn't an NPC ID is skipped; the rest load
                if (entry.Type != JTokenType.String)
                    continue;

                RecordConversation(entry.Value<string>());
            }
        }
    }
}