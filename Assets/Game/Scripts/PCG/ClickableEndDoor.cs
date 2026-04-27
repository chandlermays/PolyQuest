/*---------------------------
File: ClickableEndDoor.cs
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
    [RequireComponent(typeof(EndDoor))]
    public class ClickableEndDoor : MonoBehaviour, IRaycastable
    {
        private EndDoor m_endDoor;

        private Outline m_outline;
        private WorldLabel m_worldLabel;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_endDoor = GetComponent<EndDoor>();
            Utilities.CheckForNull(m_endDoor, nameof(m_endDoor));

            m_outline = GetComponent<Outline>();
            Utilities.CheckForNull(m_outline, nameof(m_outline));
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

        /*--------------------------------------------------------------------------------------------
        | --- ToggleHighlight: Enables or disables the highlight and item label for the end door --- |
        --------------------------------------------------------------------------------------------*/
        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
        }

        /*----------------------------------------------------------------------------------
        | --- ToggleLabel: Controls the visibility of the world label for the end door --- |
        ----------------------------------------------------------------------------------*/
        public void ToggleLabel(bool visible)
        {
            m_worldLabel.Toggle(visible);
        }
    }
}