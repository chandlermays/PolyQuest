/*---------------------------
| File: WorldFlags.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;
using PolyQuest.Tools;

namespace PolyQuest.Quests
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Records the facts about the world the player has caused, regardless of quests, so      *
     *       objectives can read the state of the world instead of only events.                     *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Sets flags one way: once set, a flag stays set.                                       *
     *      - Answers kWorldFlag conditions.                                                        *
     *      - Saves and restores the set flags by ID.                                               *
     *      - Raises OnFlagSet so active objectives can be re-checked.                              *
     * -------------------------------------------------------------------------------------------- */
    public class WorldFlags : MonoBehaviour, ISaveable, IConditionChecker
    {
        private readonly HashSet<string> m_setFlags = new();

        public event Action<WorldFlag> OnFlagSet;

        /*----------------------------------------------
        | --- IsSet: Whether the flag has been set --- |
        ----------------------------------------------*/
        public bool IsSet(WorldFlag flag)
        {
            return flag != null && m_setFlags.Contains(flag.ID);
        }

        /*----------------------------------------------------------------------------------
        | --- Set: Records the fact; setting it again does nothing (flags are one-way) --- |
        ----------------------------------------------------------------------------------*/
        public void Set(WorldFlag flag)
        {
            if (flag == null || string.IsNullOrWhiteSpace(flag.ID))
            {
                WarnInEditor($"WorldFlags on '{name}': asked to set a flag that is missing or has no ID, so nothing was recorded.");
                return;
            }

            if (!m_setFlags.Add(flag.ID))
                return;

            OnFlagSet?.Invoke(flag);
        }

        /*-----------------------------------------------------------------------------------------
        | --- Evaluate: Answers kWorldFlag; a missing or unknown flag ID is unanswered (null) --- |
        -----------------------------------------------------------------------------------------*/
        public bool? Evaluate(ConditionType condition, string[] parameters)
        {
            if (condition != ConditionType.kWorldFlag)
                return null;

            if (parameters == null || parameters.Length == 0 || WorldFlag.GetByID(parameters[0]) == null)
                return null;

            return m_setFlags.Contains(parameters[0]);
        }

        /*-------------------------------------------------------
        | --- CaptureState: Saves the IDs of every set flag --- |
        -------------------------------------------------------*/
        public JToken CaptureState()
        {
            return new JArray(m_setFlags.OrderBy(id => id, StringComparer.Ordinal));
        }

        /*------------------------------------------------------------------------------------------------
        | --- RestoreState: Restores the saved flags; an entry that isn't a known flag ID is skipped --- |
        ------------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            // The whole save is the wrong shape: ignore it and keep the current flags
            if (state is not JArray entries)
                return;

            m_setFlags.Clear();
            foreach (JToken entry in entries)
            {
                string flagID = entry.Type == JTokenType.String ? entry.Value<string>() : null;
                if (WorldFlag.GetByID(flagID) == null)
                {
                    WarnInEditor($"WorldFlags on '{name}': skipped saved flag '{entry}' because no WorldFlag has that ID. The other flags loaded.");
                    continue;
                }

                m_setFlags.Add(flagID);
            }
        }

        /*-------------------------------------------
        | --- WarnInEditor: Editor-only warning --- |
        -------------------------------------------*/
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void WarnInEditor(string message)
        {
            Debug.LogWarning(message, this);
        }
    }
}