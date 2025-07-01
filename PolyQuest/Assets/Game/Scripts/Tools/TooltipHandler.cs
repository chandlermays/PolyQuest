using UnityEngine;
using UnityEngine.EventSystems;

namespace PolyQuest
{
    public abstract class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject m_tooltipPrefab;
        private GameObject m_tooltip;
        private const int kCornerCount = 4;

        public abstract void UpdateTooltip(GameObject tooltip);
        public abstract bool CanDisplayTooltip();

        /*---------------------------------------------------------
        | --- OnDestroy: Called when this object is destroyed --- |
        ---------------------------------------------------------*/
        private void OnDestroy()
        {
            ClearTooltip();
        }

        /*--------------------------------------------------------------------
        | --- OnDisable: Called when this object is disabled or inactive --- |
        --------------------------------------------------------------------*/
        private void OnDisable()
        {
            ClearTooltip();
        }

        /*-------------------------------------------------------------
        | --- PositionTooltip: Position the tooltip on the screen --- |
        -------------------------------------------------------------*/
        private void PositionTooltip()
        {
            Canvas.ForceUpdateCanvases();

            Vector3[] tooltipCorners = new Vector3[kCornerCount];
            m_tooltip.GetComponent<RectTransform>().GetWorldCorners(tooltipCorners);
            Vector3[] slotCorners = new Vector3[kCornerCount];
            GetComponent<RectTransform>().GetWorldCorners(slotCorners);

            bool isBottom = transform.position.y > Screen.height / 2;
            bool isRight = transform.position.x < Screen.width / 2;

            int slotCorner = GetCornerIndex(isBottom, isRight);
            int tooltipCorner = GetCornerIndex(!isBottom, !isRight);

            m_tooltip.transform.position = slotCorners[slotCorner] - tooltipCorners[tooltipCorner] + m_tooltip.transform.position;
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
        private void ClearTooltip()
        {
            if (m_tooltip)
            {
                Destroy(m_tooltip);
                m_tooltip = null;
            }
        }

        /*--------------------------------------------------------------------
        | --- OnPointerEnter: Called when the pointer enters this object --- |
        --------------------------------------------------------------------*/
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();

            if (m_tooltip && !CanDisplayTooltip())
            {
                ClearTooltip();
            }

            if (!m_tooltip && CanDisplayTooltip())
            {
                m_tooltip = Instantiate(m_tooltipPrefab, parentCanvas.transform);
            }

            if (m_tooltip)
            {
                UpdateTooltip(m_tooltip);
                PositionTooltip();
            }
        }

        /*------------------------------------------------------------------
        | --- OnPointerExit: Called when the pointer exits this object --- |
        ------------------------------------------------------------------*/
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            ClearTooltip();
        }
    }
}