namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Provides a contract for any component that can hold or represent an inventory m_item.   *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Exposes a method to retrieve the currently held InventoryItem.                       *
     *      - Enables generic handling of m_item holders in UI and gameplay systems.                 *
     * ------------------------------------------------------------------------------------------- */
    public interface IItemHolder
    {
        InventoryItem GetItem();
    }
}