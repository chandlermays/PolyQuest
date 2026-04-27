/*---------------------------
File: IRaycastable.cs
Author: Chandler Mays
----------------------------*/
using PolyQuest.Player;
using PolyQuest.UI.Core;

namespace PolyQuest.Core
{
    /* --------------------------------------------------------------------------------------------
     * Role: Defines a contract for objects that can be interacted with via raycasting.            *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Specifies the cursor type to display when the object is hovered by the mouse.        *
     *      - Handles interaction logic when the object is clicked or otherwise raycasted.         *
     *      - Enables consistent interaction handling for in-world objects and UI elements.        *
     * ------------------------------------------------------------------------------------------- */
    public interface IRaycastable
    {
        CursorSettings.CursorType GetCursorType();
        bool HandleRaycast(PlayerController playerController);
        void ToggleHighlight(bool highlight);
        void ToggleLabel(bool visible);
    }
}