using PolyQuest.UI.Dragging;

namespace PolyQuest.Inventories
{
    public class InventoryDragItem : DragItem<InventoryItem>
    {
        // An extension point for inventory-specific drag-and-drop logic, while inheriting the base functionality of DragItem<T>
        // with the use of UI interfaces: IBeginDragHandler, IDragHandler, and IEndDragHandler.

        // Practical methods and properties to handle inventory system-specific behavior, such as:
        // - Visual Feedback (Highlight on drag, tooltips, ...)
        // - Inventory-specific Events
        // - Custom Drag Restrictions
        // - Inventory Context Awareness
    }
}