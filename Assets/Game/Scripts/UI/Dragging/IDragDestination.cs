namespace PolyQuest.UI.Dragging
{
    /* --------------------------------------------------------------------------------------------
     * Role: Defines a contract for any UI element that can act as a destination in drag-and-drop. *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Specifies how many items of a given type can be accepted.                            *
     *      - Handles adding items to the destination during a drag-and-drop operation.            *
     *      - Enables generic handling of drag destinations in UI systems.                         *
     * ------------------------------------------------------------------------------------------- */
    public interface IDragDestination<T> where T : class
    {
        /// <summary>Max items that can be accepted.</summary>
        int GetMaxItemsCapacity(T item);

        /// <summary>Adds items to the destination.</summary>
        void AddItems(T item, int quantity);
    }
}