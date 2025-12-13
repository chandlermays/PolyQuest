using PolyQuest.UI.Dragging;

namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Handles inventory-specific drag-and-drop operations, including stack splitting.      *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Extends base DragItem functionality with inventory-specific behavior.                *
     *      - Provides extension point for visual feedback and inventory context awareness.        *
     *      - Works with split modifier detection handled by InventorySlotUI.                      *
     * ------------------------------------------------------------------------------------------- */
    public class InventoryDragItem : DragItem<InventoryItem>
    {
        // An extension point for inventory-specific drag-and-drop logic, while inheriting the base functionality of DragItem<T>
        // with the use of UI interfaces: IBeginDragHandler, IDragHandler, and IEndDragHandler.

        // Practical methods and properties to handle inventory system-specific behavior, such as:
        // - Visual Feedback (Highlight on drag, tooltips, ...)
        // - Inventory-specific Events
        // - Custom Drag Restrictions
        // - Inventory Context Awareness
        
        // Note: Stack splitting is handled at the InventorySlotUI level to properly manage
        // the source inventory state and dialog interactions.
    }
}