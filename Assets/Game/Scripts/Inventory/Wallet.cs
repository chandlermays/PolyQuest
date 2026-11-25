/*---------------------------
| File: Wallet.cs
| Author: Chandler Mays
----------------------------*/
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

        /*-------------------------------------------------------------------------------------------------------
        | --- RestoreState: Restores the saved balance; anything but a whole number of 0 or more is ignored --- |
        -------------------------------------------------------------------------------------------------------*/
        public void RestoreState(JToken state)
        {
            // Unreadable or negative: keep the current balance and raise nothing
            if (state == null || state.Type != JTokenType.Integer || state.Value<long>() < 0 || state.Value<long>() > int.MaxValue)
                return;

            m_currentSilver = state.Value<int>();
            OnWalletUpdated?.Invoke();
        }
    }
}