/*---------------------------
File: IDragContainer.cs
Author: Chandler Mays
----------------------------*/
namespace PolyQuest.UI.Dragging
{
    /* --------------------------------------------------------------------------------------------
     * Role: Combines drag source and destination functionality for UI containers.                 *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Acts as both a drag source and a drag destination for items.                         *
     *      - Enables two-way item transfer and swapping between containers.                       *
     *      - Supports generic item handling for flexible UI systems.                              *
     * ------------------------------------------------------------------------------------------- */
    public interface IDragContainer<T> : IDragDestination<T>, IDragSource<T> where T : class
    {}
}