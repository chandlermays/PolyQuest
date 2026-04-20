/*---------------------------
File: ActionManager.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.Core
{
    public class ActionManager : MonoBehaviour
    {
        IAction m_currentAction;            // The current action being executed

        /*------------------------------------------------
        | --- StartAction: Sets a new current action --- |
        ------------------------------------------------*/
        public void StartAction(IAction action)
        {
            // If the new action is the same as the current one, do nothing
            if (m_currentAction == action)
                return;

            // If there is a current action, cancel it before starting the new one
            if (m_currentAction != null)
            {
                m_currentAction.Cancel();
            }

            // Start the new action
            m_currentAction = action;
        }

        /*---------------------------------------------------------
        | --- CancelCurrentAction: Cancels the current action --- |
        ---------------------------------------------------------*/
        public void CancelCurrentAction()
        {
            if (m_currentAction == null)
                return;

            m_currentAction.Cancel();
            m_currentAction = null;
        }

        //.. anything else?
    }
}