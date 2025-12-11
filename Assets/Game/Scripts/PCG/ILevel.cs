namespace PolyQuest.PCG
{
    public interface ILevel
    {
        int Width { get; }
        int Length { get; }

        bool IsBlocked(int x, int y);
    }
}