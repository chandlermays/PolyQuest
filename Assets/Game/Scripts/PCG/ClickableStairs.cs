/*---------------------------
File: ClickableStairs.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Player;
using PolyQuest.UI.Core;
using PolyQuest.UI;

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
        private WorldLabel m_worldLabel;

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

        /*------------------------------------------------------------------------------------------
        | --- ToggleHighlight: Enables or disables the highlight and item label for the stairs --- |
        ------------------------------------------------------------------------------------------*/
        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
        }

        /*--------------------------------------------------------------------------------
        | --- ToggleLabel: Controls the visibility of the world label for the stairs --- |
        --------------------------------------------------------------------------------*/
        public void ToggleLabel(bool visible)
        {
            m_worldLabel.Toggle(visible);
        }
    }
}