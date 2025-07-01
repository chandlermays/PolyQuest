namespace PolyQuest
{
    public interface IRaycastable
    {
        CursorSettings.CursorType GetCursorType();
        bool HandleRaycast(PlayerController playerController);
    }
}