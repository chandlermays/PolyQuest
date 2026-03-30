namespace PolyQuest.Inventories
{
    /* --------------------------------------------------------------------------------------------
     * Role: Defines the possible categories for inventory items in the game.                      *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Provides a way to classify items (e.g., consumables, armor, weapons, etc.).          *
     *      - Enables filtering, sorting, and logic based on item category.                        *
     *      - Supports future expansion as new item types are added to the game.                   *
     * ------------------------------------------------------------------------------------------- */
    public enum ItemCategory
    {
        kNone,
        kWeapon,
        kArmor,
        kConsumables,
        kAbilities,
        kQuestItem,
        kCraftingMaterial,
        kJunk,
        kCurrency
    }
}