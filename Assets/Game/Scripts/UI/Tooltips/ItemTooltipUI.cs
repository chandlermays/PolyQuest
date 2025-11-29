using TMPro;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.UI.Tooltip
{
    /* --------------------------------------------------------------------------------------------
     * Role: Displays the name and description of an inventory m_item in a tooltip UI element.       *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Shows the m_item's name and description in the tooltip.                                *
     *      - Provides a setup method to initialize the tooltip with m_item data.                    *
     * ------------------------------------------------------------------------------------------- */
    public class ItemTooltipUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private TextMeshProUGUI m_bodyText;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_titleText, nameof(m_titleText));
            Utilities.CheckForNull(m_bodyText, nameof(m_bodyText));
        }

        /*------------------------------------------------------
        | --- Setup: InitializeDecorationArray the tooltip with m_item data --- |
        ------------------------------------------------------*/
        public void Setup(InventoryItem item)
        {
            m_titleText.text = item.Name;
            m_bodyText.text = item.Description;
        }
    }
}