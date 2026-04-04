using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Inventories
{
    public class Wallet : MonoBehaviour, ISaveable
    {
        private int m_currentSilver = 0;

        public event Action OnWalletUpdated;

        public int CurrentSilver => m_currentSilver;

        /*---------------------------------------------------------------------------------
        | --- UpdateSilver: Updates the current silver amount by the specified amount --- |
        ---------------------------------------------------------------------------------*/
        public void UpdateSilver(int amount)
        {
            m_currentSilver += amount;
            OnWalletUpdated?.Invoke();
        }

        /*----------------------------------------------------------------
        | --- CaptureState: Captures the current state of the wallet --- |
        ----------------------------------------------------------------*/
        public JToken CaptureState()
        {
            return JToken.FromObject(m_currentSilver);
        }

        /*---------------------------------------------------------------------
        | --- RestoreState: Restores the wallet state from the saved data --- |
        ---------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            m_currentSilver = state.ToObject<int>();
            OnWalletUpdated?.Invoke();
        }
    }
}