using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Player;
using PolyQuest.UI.Core;

namespace PolyQuest.SceneManagement
{
    /* --------------------------------------------------------------------------------------------------
     * Role: Enables the End Door to be interacted with via mouse click using raycast interaction.       *
     *                                                                                                   *
     * Responsibilities:                                                                                 *
     *      - Implements IRaycastable to allow the End Door to be detected and interacted with.          *
     *      - Sets the mouse cursor to the portal/interaction icon when hovered.                         *
     *      - Handles mouse click input to begin moving the player toward the End Door.                  *
     * ------------------------------------------------------------------------------------------------- */
    [RequireComponent(typeof(EndDoor))]
    public class ClickableEndDoor : MonoBehaviour, IRaycastable
    {
        private EndDoor m_endDoor;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_endDoor = GetComponent<EndDoor>();
            Utilities.CheckForNull(m_endDoor, nameof(m_endDoor));
        }

        /*-----------------------------------------------------------------
        | --- GetCursorType: Returns the cursor type for the End Door --- |
        -----------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kTargeting;
        }

        /*--------------------------------------------------------------------------
        | --- HandleRaycast: Handles the raycast interaction with the End Door --- |
        --------------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                m_endDoor.SetAsTarget(playerController);
            }
            return true;
        }
    }
}