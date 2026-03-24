using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Player;
using PolyQuest.UI.Core;

namespace PolyQuest.SceneManagement
{
    /* --------------------------------------------------------------------------------------------------
     * Role: Enables the Stairs to be interacted with via mouse click using raycast interaction.         *
     *                                                                                                   *
     * Responsibilities:                                                                                 *
     *      - Implements IRaycastable to allow the Stairs to be detected and interacted with.            *
     *      - Sets the mouse cursor to the appropriate icon when hovered.                                *
     *      - Handles mouse click input to begin moving the player toward the Stairs.                    *
     * ------------------------------------------------------------------------------------------------- */
    [RequireComponent(typeof(Stairs))]
    public class ClickableStairs : MonoBehaviour, IRaycastable
    {
        private Stairs m_stairs;

        private Outline m_outline;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_stairs = GetComponent<Stairs>();
            Utilities.CheckForNull(m_stairs, nameof(m_stairs));

            m_outline = GetComponent<Outline>();
            Utilities.CheckForNull(m_outline, nameof(m_outline));
        }

        /*---------------------------------------------------------------
        | --- GetCursorType: Returns the cursor type for the Stairs --- |
        ---------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kTargeting;
        }

        /*------------------------------------------------------------------------
        | --- HandleRaycast: Handles the raycast interaction with the Stairs --- |
        ------------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                m_stairs.SetAsTarget(playerController);
            }
            return true;
        }

        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
        }
    }
}