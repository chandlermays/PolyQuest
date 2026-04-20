/*---------------------------
File: TooltipHandler.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
using UnityEngine.EventSystems;
//---------------------------------

namespace PolyQuest.UI.Tooltip
{
    /* -------------------------------------------------------------------------------------------
    * Role: Provides a reusable base for UI components that display tooltips on pointer hover.    *
    *                                                                                             *
    * Responsibilities:                                                                           *
    *      - Handles pointer enter/exit events to show or hide a tooltip.                         *
    *      - Instantiates, positions, and destroys tooltip UI elements as needed.                 *
    *      - Defines abstract methods for updating tooltip content and display conditions.        *
    *      - Ensures tooltips are positioned relative to the hovered UI element.                  *
    * ------------------------------------------------------------------------------------------- */
    public abstract class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject m_tooltipPrefab;

        private static GameObject s_activeTooltip;
        private static TooltipHandler s_activeHandler;

        private const int kCornerCount = 4;

        public abstract void UpdateTooltip(GameObject tooltip);
        public abstract bool CanDisplayTooltip();

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_tooltipPrefab, nameof(m_tooltipPrefab));
        }

        private void OnDestroy() => ClearIfOwner();
        private void OnDisable() => ClearIfOwner();

        /*------------------------------------------------------------------------
        | --- ClearIfOwner: Only destroy the tooltip if this handler owns it --- |
        ------------------------------------------------------------------------*/
        private void ClearIfOwner()
        {
            if (s_activeHandler == this)
            {
                ClearTooltip();
            }
        }

        /*-------------------------------------------------------------
        | --- PositionTooltip: Position the tooltip on the screen --- |
        -------------------------------------------------------------*/
        private void PositionTooltip()
        {
            Canvas.ForceUpdateCanvases();

            Vector3[] tooltipCorners = new Vector3[kCornerCount];
            s_activeTooltip.GetComponent<RectTransform>().GetWorldCorners(tooltipCorners);
            Vector3[] slotCorners = new Vector3[kCornerCount];
            GetComponent<RectTransform>().GetWorldCorners(slotCorners);

            bool isBottom = transform.position.y > Screen.height / 2;
            bool isRight = transform.position.x < Screen.width / 2;

            int slotCorner = GetCornerIndex(isBottom, isRight);
            int tooltipCorner = GetCornerIndex(!isBottom, !isRight);

            s_activeTooltip.transform.position = slotCorners[slotCorner] - tooltipCorners[tooltipCorner] + s_activeTooltip.transform.position;
        }

        /*-----------------------------------------------------
        | --- GetCornerIndex: Get the index of the corner --- |
        -----------------------------------------------------*/
        private int GetCornerIndex(bool bottom, bool right)
        {
            if (bottom && !right)
                return 0;

            else if (!bottom && !right)
                return 1;

            else if (!bottom && right)
                return 2;

            else return 3;
        }

        /*---------------------------------------------------
        | --- ClearTooltip: Destroy the tooltip display --- |
        ---------------------------------------------------*/
        private static void ClearTooltip()
        {
            if (s_activeTooltip != null)
            {
                Destroy(s_activeTooltip);
                s_activeTooltip = null;
            }
            s_activeHandler = null;
        }

        /*--------------------------------------------------------------------
        | --- OnPointerEnter: Called when the pointer enters this object --- |
        --------------------------------------------------------------------*/
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            ClearTooltip();

            if (!CanDisplayTooltip())
                return;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            s_activeTooltip = Instantiate(m_tooltipPrefab, parentCanvas.transform);
            s_activeHandler = this;

            UpdateTooltip(s_activeTooltip);
            PositionTooltip();
        }

        /*------------------------------------------------------------------
        | --- OnPointerExit: Called when the pointer exits this object --- |
        ------------------------------------------------------------------*/
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            ClearIfOwner();
        }
    }
}