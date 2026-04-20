/*---------------------------
File: IDragSource.cs
Author: Chandler Mays
----------------------------*/
namespace PolyQuest.UI.Dragging
{
    /* --------------------------------------------------------------------------------------------
     * Role: Defines a contract for any UI element that can act as a source in drag-and-drop.      *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Provides access to the item and quantity being dragged.                              *
     *      - Allows removal of items from the source during a drag-and-drop operation.            *
     *      - Enables generic handling of drag sources in UI systems.                              *
     * ------------------------------------------------------------------------------------------- */
    public interface IDragSource<T> where T : class
    {
        /// <summary>Get the m_item being dragged.</summary>
        T GetItem();

        /// <summary>Get the m_quantity of the m_item being dragged.</summary>
        int GetQuantity();

        /// <summary>Remove a given number of items from the source.</summary>
        void RemoveItems(int quantity);
    }
}