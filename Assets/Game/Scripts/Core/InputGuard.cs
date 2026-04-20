/*---------------------------
File: InputGuard.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;

namespace PolyQuest.Core
{
    public class InputGuard : MonoBehaviour
    {
        public static InputGuard Instance { get; private set; }

        private int m_lockCount = 0;

        public bool IsInputLocked => m_lockCount > 0;

        /*---------------------------------------------------------------- 
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /*----------------------------------------------- 
        | --- LockInput: Increment the lock counter --- |
        -----------------------------------------------*/
        public void LockInput()
        {
            m_lockCount++;
        }

        /*------------------------------------------------- 
        | --- UnlockInput: Decrement the lock counter --- |
        -------------------------------------------------*/
        public void UnlockInput()
        {
            m_lockCount = Mathf.Max(0, m_lockCount - 1);
        }
    }
}