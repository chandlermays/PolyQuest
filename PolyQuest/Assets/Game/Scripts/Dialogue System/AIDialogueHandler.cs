using UnityEngine;

namespace PolyQuest
{
    public class AIDialogueHandler : MonoBehaviour, IRaycastable
    {
        [SerializeField] private Dialogue m_dialogue;

        /*-------------------------------------------------------------
        | --- GetCursorType: Returns the Cursor Type for Dialogue --- |
        -------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kDialogue;
        }

        /*----------------------------------------------------------------------------
        | --- HandleRaycast: The Behavior of the Raycast for Initiating Dialogue --- |
        ----------------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (m_dialogue == null)
                return false;

            if (Input.GetMouseButtonDown(0))
            {
                playerController.GetComponent<PlayerDialogueHandler>().BeginDialogue(this, m_dialogue);
            }

            return true;
        }
    }
}