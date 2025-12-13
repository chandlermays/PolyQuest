# Stack Splitting Feature

## Overview
This feature enables players to split stackable items in their inventory by holding the Shift key (Split Modifier) and clicking on an item stack. A dialog will appear allowing them to specify how many items to split from the stack, which can then be dragged to a new location.

## User Workflow

1. **Initiate Split**: Hold **Shift** key and **click** on a stackable item with quantity > 1
2. **Enter Quantity**: A dialog appears asking how many items to split (max is total quantity - 1)
3. **Confirm Split**: Enter the desired quantity and click "Confirm"
4. **Drag Items**: After confirming, the next drag operation will transfer only the specified quantity
5. **Drop Items**: Drop the split items into any valid destination (inventory slot, equipment, etc.)

## Key Bindings

- **Split Modifier**: Left Shift or Right Shift (configured in PolyQuestInputActions.inputactions)

## Architecture

### Components

#### 1. InputManager (Core/InputManager.cs)
- **Purpose**: Provides centralized access to input actions
- **New Addition**: `IsSplitModifierPressed` property to check if Shift is pressed
- **Usage**: `InputManager.Instance.IsSplitModifierPressed`

#### 2. ItemSplitDialog (UI/Core/ItemSplitDialog.cs)
- **Purpose**: UI dialog for entering split quantity
- **Features**:
  - Numeric input field with validation
  - Confirm/Cancel buttons
  - Default value set to half the stack
  - Prevents invalid input (negative, zero, or exceeding max)
- **Unity Setup Required**: 
  - Create a Canvas with a Panel GameObject
  - Attach ItemSplitDialog component
  - Assign UI elements in inspector:
    - m_dialogPanel: The panel GameObject to show/hide
    - m_quantityInputField: TMP_InputField for number entry
    - m_titleText: TextMeshProUGUI for title display
    - m_confirmButton: Button to confirm split
    - m_cancelButton: Button to cancel split

#### 3. IDragSource Interface (UI/Dragging/IDragSource.cs)
- **Purpose**: Contract for drag source objects
- **New Method**: `GetDragQuantityOverride()` 
  - Returns 0 for normal drag (full quantity)
  - Returns split quantity when splitting is active

#### 4. DragItem (UI/Dragging/DragItem.cs)
- **Purpose**: Generic drag-and-drop handler
- **Modifications**:
  - Checks `GetDragQuantityOverride()` from source
  - Uses override quantity for transfers and swaps
  - Supports both split and full quantity operations

#### 5. InventorySlotUI (Inventory/InventorySlotUI.cs)
- **Purpose**: Handles individual inventory slot UI and interactions
- **New Features**:
  - Implements `IPointerClickHandler` to detect Shift+Click
  - Manages split state (m_splitRequested, m_splitQuantity)
  - Shows ItemSplitDialog when appropriate
  - Returns split quantity via `GetDragQuantityOverride()`

#### 6. InventoryDragItem (Inventory/InventoryDragItem.cs)
- **Purpose**: Inventory-specific drag handler
- **Note**: Currently minimal, as split logic is handled at the InventorySlotUI level

#### 7. ActionSlotUI & EquipmentSlotUI
- **Purpose**: Action bar and equipment slots
- **Implementation**: Return 0 from `GetDragQuantityOverride()` (no splitting support)

## Implementation Details

### Split Flow

```
1. User: Hold Shift + Click on stackable item (quantity > 1)
   ↓
2. InventorySlotUI.OnPointerClick() detects Shift is pressed
   ↓
3. ItemSplitDialog.Show() displays with max = (quantity - 1)
   ↓
4. User enters quantity and clicks Confirm
   ↓
5. OnSplitConfirmed() sets m_splitQuantity in InventorySlotUI
   ↓
6. User drags the item
   ↓
7. DragItem checks GetDragQuantityOverride() from source
   ↓
8. Only the split quantity is transferred/swapped
   ↓
9. Split quantity resets on next interaction
```

### Validation Rules

- **Stackability**: Only stackable items can be split (InventoryItem.IsStackable == true)
- **Minimum Quantity**: Stack must have at least 2 items to split
- **Split Range**: Can split 1 to (quantity - 1) items (must leave at least 1 in source)
- **Input Validation**: Dialog prevents invalid values and disables Confirm button

### Edge Cases Handled

1. **Non-stackable Items**: Split dialog won't appear (e.g., weapons, armor)
2. **Single Item**: Split dialog won't appear (quantity == 1)
3. **Equipment Slots**: Always move entire item (GetDragQuantityOverride returns 0)
4. **Action Slots**: No split support (GetDragQuantityOverride returns 0)
5. **Cancel Dialog**: Clicking Cancel or closing dialog resets split state
6. **Normal Drag**: Without Shift, works as before (full quantity transfer)

## Testing Checklist

- [ ] Hold Shift + Click on stackable item shows dialog
- [ ] Dialog displays correct max quantity (total - 1)
- [ ] Entering valid quantity and confirming allows split drag
- [ ] Dragging split items transfers only the specified amount
- [ ] Source retains remaining items after split
- [ ] Non-stackable items don't show split dialog
- [ ] Single quantity items don't show split dialog
- [ ] Shift key indicator in dialog title
- [ ] Cancel button properly resets split state
- [ ] Normal drag (without Shift) still works
- [ ] Split works with inventory-to-inventory transfers
- [ ] Split works when dropping on empty slots
- [ ] Split respects destination capacity limits

## Unity Scene Setup

To use this feature, you need to add the ItemSplitDialog to your game scene:

1. **Create Dialog UI**:
   - Add Canvas (if not exists)
   - Create Panel as child of Canvas
   - Add TextMeshProUGUI for title
   - Add TMP_InputField for quantity input
   - Add two Buttons (Confirm, Cancel)

2. **Configure ItemSplitDialog**:
   - Add ItemSplitDialog script to the Panel
   - Assign all UI references in Inspector
   - Set m_dialogPanel to inactive by default

3. **Styling** (Optional):
   - Style the panel to match your game's UI theme
   - Configure input field to accept only integers
   - Add visual feedback for validation errors

## Extending the Feature

### Future Enhancements

1. **Keyboard Shortcuts**: Add Enter to confirm, Escape to cancel
2. **Right-Click Split**: Alternative to Shift+Click
3. **Quick Split**: Shift+Click without dialog splits stack in half automatically
4. **Visual Feedback**: Show split icon or indicator during Shift hover
5. **Drag Preview**: Show quantity being dragged on cursor
6. **Split History**: Remember last split value per item type
7. **Gamepad Support**: Add gamepad button for split modifier

### API for Developers

```csharp
// Check if split modifier is pressed
bool isSplitting = InputManager.Instance.IsSplitModifierPressed;

// Get current drag quantity override from a slot
var slot = GetComponent<InventorySlotUI>();
int splitQty = slot.GetDragQuantityOverride();

// Show split dialog programmatically
var dialog = FindObjectOfType<ItemSplitDialog>();
dialog.Show(maxQuantity: 10, 
    onConfirm: (qty) => Debug.Log($"Split {qty}"),
    onCancel: () => Debug.Log("Cancelled"));
```

## Related Files

- `/Assets/Game/Input Mapping/PolyQuestInputActions.inputactions` - Input action definitions
- `/Assets/Game/Scripts/Core/InputManager.cs` - Input system manager
- `/Assets/Game/Scripts/UI/Core/ItemSplitDialog.cs` - Split dialog UI component
- `/Assets/Game/Scripts/UI/Dragging/DragItem.cs` - Generic drag handler
- `/Assets/Game/Scripts/UI/Dragging/IDragSource.cs` - Drag source interface
- `/Assets/Game/Scripts/Inventory/InventorySlotUI.cs` - Inventory slot handler
- `/Assets/Game/Scripts/Inventory/InventoryItem.cs` - Item data definition

## Known Limitations

1. Unity editor is required to create and configure the ItemSplitDialog UI prefab
2. Split operation requires the dialog to be present in the scene
3. Currently supports only inventory items, not other draggable types
4. No visual indicator for split mode before clicking

## Troubleshooting

**Problem**: Split dialog doesn't appear when holding Shift
- **Solution**: Ensure ItemSplitDialog component is present in scene and properly configured

**Problem**: Dialog appears but dragging doesn't use split quantity
- **Solution**: Check that all implementations of IDragSource return correct value from GetDragQuantityOverride()

**Problem**: Can't enter quantity in dialog
- **Solution**: Verify TMP_InputField is configured for numeric input and not blocked by other UI

**Problem**: Shift key not detected
- **Solution**: Check InputManager is in scene and PolyQuestInputActions UI action map is enabled
