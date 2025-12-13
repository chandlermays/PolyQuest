using UnityEngine;
using UnityEngine.EventSystems;
//---------------------------------

namespace PolyQuest.UI.Dragging
{
    /* -----------------------------------------------------------------------------------------------
     * Role: Provides a reusable, generic drag-and-drop handler for UI elements.                     *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Enables UI elements to be dragged and dropped between containers or slots.             *
     *      - Handles drag events and manages item transfer or swapping between sources and targets. *
     *      - Supports both item transfer and item swapping logic for inventory-like systems.        *
     * --------------------------------------------------------------------------------------------- */
    public class DragItem<T> : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler where T : class
    {
        private Vector3 m_startPosition;
        private Transform m_originalParent;
        private IDragSource<T> m_dragSource;
        private Canvas m_parentCanvas;
        private CanvasGroup m_canvasGroup;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_dragSource = GetComponentInParent<IDragSource<T>>();

            m_parentCanvas = GetComponentInParent<Canvas>();
            Utilities.CheckForNull(m_parentCanvas, nameof(m_parentCanvas));

            m_canvasGroup = GetComponent<CanvasGroup>();
            Utilities.CheckForNull(m_canvasGroup, nameof(m_canvasGroup));
        }

        /*-----------------------------------------------------------
        | --- OnBeginDrag: Called when the user starts dragging --- |
        -----------------------------------------------------------*/
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            m_startPosition = transform.position;
            m_originalParent = transform.parent;

            // Block input when dragging items
            m_canvasGroup.blocksRaycasts = false;
            transform.SetParent(m_parentCanvas.transform, true);
        }

        /*-----------------------------------------------------------
        | --- OnDrag: Called when the user is dragging the m_item --- |
        -----------------------------------------------------------*/
        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        /*-----------------------------------------------------------------
        | --- OnEndDrag: Called when the user stops dragging the m_item --- |
        -----------------------------------------------------------------*/
        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            transform.position = m_startPosition;

            // Re-enable input after dragging
            m_canvasGroup.blocksRaycasts = true;
            transform.SetParent(m_originalParent, true);

            IDragDestination<T> targetContainer;
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                targetContainer = m_parentCanvas.GetComponent<IDragDestination<T>>();
            }
            else
            {
                targetContainer = GetContainer(eventData);
            }

            if (targetContainer != null)
            {
                DropItemIntoContainer(targetContainer);
            }
        }

        /*-------------------------------------------------------------
        | --- GetContainer: Finds the container under the pointer --- |
        -------------------------------------------------------------*/
        private IDragDestination<T> GetContainer(PointerEventData eventData)
        {
            if (eventData.pointerEnter)
            {
                var container = eventData.pointerEnter.GetComponentInParent<IDragDestination<T>>();

                return container;
            }

            return null;
        }

        /*---------------------------------------------------------------------------
        | --- DropItemIntoContainer: Handles dropping the m_item into a container --- |
        ---------------------------------------------------------------------------*/
        private void DropItemIntoContainer(IDragDestination<T> destContainer)
        {
            if (object.ReferenceEquals(destContainer, m_dragSource))
                return;


            if (destContainer is not IDragContainer<T> destinationContainer ||
                m_dragSource is not IDragContainer<T> sourceContainer ||
                destinationContainer.GetItem() == null ||
                object.ReferenceEquals(destinationContainer.GetItem(), sourceContainer.GetItem()))
            {
                ProcessItemTransfer(destContainer);
                return;
            }

            SwapItems(destinationContainer, sourceContainer);
        }

        /*-------------------------------------------------------------------
        | --- SwapItems: Swaps items between two containers if possible --- |
        -------------------------------------------------------------------*/
        private void SwapItems(IDragContainer<T> destination, IDragContainer<T> source)
        {
            T prevSourceItem = source.GetItem();
            int prevSourceQuantity = source.GetQuantity();
            
            // Check if source has a split quantity override
            int overrideQuantity = source.GetDragQuantityOverride();
            if (overrideQuantity > 0 && overrideQuantity < prevSourceQuantity)
            {
                prevSourceQuantity = overrideQuantity;
            }
            
            T prevDestinationItem = destination.GetItem();
            int prevDestinationQuantity = destination.GetQuantity();

            source.RemoveItems(prevSourceQuantity);
            destination.RemoveItems(prevDestinationQuantity);

            int sourceSplitQuantity = CalculateSplit(prevSourceItem, prevSourceQuantity, source, destination);
            int destinationSplitQuantity = CalculateSplit(prevDestinationItem, prevDestinationQuantity, destination, source);

            if (sourceSplitQuantity > 0)
            {
                source.AddItems(prevSourceItem, sourceSplitQuantity);
                prevSourceQuantity -= sourceSplitQuantity;
            }

            if (destinationSplitQuantity > 0)
            {
                destination.AddItems(prevDestinationItem, destinationSplitQuantity);
                prevDestinationQuantity -= destinationSplitQuantity;
            }

            // If the swap was unsuccessful, revert and abort
            if (source.GetMaxItemsCapacity(prevDestinationItem) < prevDestinationQuantity || destination.GetMaxItemsCapacity(prevSourceItem) < prevSourceQuantity)
            {
                destination.AddItems(prevDestinationItem, prevDestinationQuantity);
                source.AddItems(prevSourceItem, prevSourceQuantity);
                return;
            }

            // Perform the swap!
            if (prevDestinationQuantity > 0)
            {
                source.AddItems(prevDestinationItem, prevDestinationQuantity);
            }

            if (prevSourceQuantity > 0)
            {
                destination.AddItems(prevSourceItem, prevSourceQuantity);
            }
        }

        /*-------------------------------------------------------------------------------
        | --- ProcessItemTransfer: Transfers items from the source to a destination --- |
        -------------------------------------------------------------------------------*/
        private bool ProcessItemTransfer(IDragDestination<T> destination)
        {
            T draggingItem = m_dragSource.GetItem();
            int draggingQuantity = m_dragSource.GetQuantity();
            
            // Check if source has a split quantity override
            int overrideQuantity = m_dragSource.GetDragQuantityOverride();
            if (overrideQuantity > 0 && overrideQuantity < draggingQuantity)
            {
                draggingQuantity = overrideQuantity;
            }

            int transferableItemLimit = destination.GetMaxItemsCapacity(draggingItem);
            int itemsToTransfer = Mathf.Min(transferableItemLimit, draggingQuantity);

            if (itemsToTransfer > 0)
            {
                m_dragSource.RemoveItems(itemsToTransfer);
                destination.AddItems(draggingItem, itemsToTransfer);
                return false;
            }
            return true;
        }

        /*-------------------------------------------------------------------------------
        | --- CalculateSplit: Calculates how many items to split between containers --- |
        -------------------------------------------------------------------------------*/
        private int CalculateSplit(T prevItem, int prevQuantity, IDragContainer<T> prevSource, IDragContainer<T> destination)
        {
            int splitQuantity = 0;
            int destinationMaxItems = destination.GetMaxItemsCapacity(prevItem);

            if (destinationMaxItems < prevQuantity)
            {
                splitQuantity = prevQuantity - destinationMaxItems;

                int prevSplitMaxItems = prevSource.GetMaxItemsCapacity(prevItem);

                if (prevSplitMaxItems < splitQuantity)
                {
                    return 0;
                }
            }
            return splitQuantity;
        }
    }
}