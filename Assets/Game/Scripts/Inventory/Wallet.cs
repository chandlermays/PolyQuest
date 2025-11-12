using System;
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

        /*--------------------------------------------------------------------------------
        | --- UpdateSiver: Updates the current silver amount by the specified amount --- |
        --------------------------------------------------------------------------------*/
        public void UpdateSiver(int amount)
        {
            m_currentSilver += amount;
            OnWalletUpdated?.Invoke();
        }

        /*----------------------------------------------------------------
        | --- CaptureState: Captures the current state of the wallet --- |
        ----------------------------------------------------------------*/
        public object CaptureState()
        {
            return m_currentSilver;
        }

        /*---------------------------------------------------------------------
        | --- RestoreState: Restores the wallet state from the saved data --- |
        ---------------------------------------------------------------------*/
        public void RestoreState(object state)
        {
            m_currentSilver = (int)state;
            OnWalletUpdated?.Invoke();
        }
    }
}