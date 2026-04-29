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
    public class ClickableEndDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Portal m_portal;

        private EndDoor m_endDoor;
        private Outline m_outline;
        private WorldLabel m_worldLabel;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            Utilities.CheckForNull(m_portal, nameof(m_portal));

            m_endDoor = GetComponent<EndDoor>();
            Utilities.CheckForNull(m_endDoor, nameof(m_endDoor));

            m_outline = GetComponent<Outline>();
            Utilities.CheckForNull(m_outline, nameof(m_outline));

            m_worldLabel = GetComponent<WorldLabel>();
            Utilities.CheckForNull(m_worldLabel, nameof(m_worldLabel));

            m_worldLabel.SetLabel("Travel to: " + m_portal.SceneName);
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
        public bool HandleInteraction(PlayerController playerController)
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