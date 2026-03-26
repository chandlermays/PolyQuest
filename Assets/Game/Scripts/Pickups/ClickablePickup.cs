using TMPro;
using UnityEngine;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Player;
using PolyQuest.UI.Core;

namespace PolyQuest.Pickups
{
    /* ---------------------------------------------------------------------------------------------------
     * Role: Enables pickups in the world to be collected via mouse click using raycast interaction.     *
     *                                                                                                   *
     * Responsibilities:                                                                                 *
     *      - Implements IRaycastable to allow pickups to be detected and interacted with by the player. *
     *      - Sets the mouse cursor to the pickup icon when hovered.                                     *
     *      - Handles mouse click input to collect the pickup and add it to the inventory.               *
     * ------------------------------------------------------------------------------------------------- */
    [RequireComponent(typeof(Pickup))]
    public class ClickablePickup : MonoBehaviour, IRaycastable
    {
        [SerializeField] private GameObject m_itemLabel;
        [SerializeField] private TextMeshProUGUI m_itemLabelText;

        private Pickup m_pickup;
        private Outline m_outline;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_pickup = GetComponent<Pickup>();
            Utilities.CheckForNull(m_pickup, nameof(m_pickup));

            m_outline = GetComponent<Outline>();
            Utilities.CheckForNull(m_outline, nameof(m_outline));
        }

        /*--------------------------------------------------------------------------------
        | --- Initialize: Sets up the pickup's item label and disables it by default --- |
        --------------------------------------------------------------------------------*/
        public void Initialize()
        {
            m_itemLabelText.text = m_pickup.Item.Name;
            m_itemLabel.SetActive(false);
        }

        /*-------------------------------------------------------------
        | --- GetCursorType: Returns the cursor type for a pickup --- |
        -------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kPickup;
        }

        /*------------------------------------------------------------------------
        | --- HandleRaycast: Handles the raycast interaction with the pickup --- |
        ------------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                m_pickup.SetAsTarget(playerController);
            }
            return true;
        }

        /*------------------------------------------------------------------------------------------
        | --- ToggleHighlight: Enables or disables the highlight and item label for the pickup --- |
        ------------------------------------------------------------------------------------------*/
        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
            m_itemLabel.SetActive(highlight);
        }
    }
}