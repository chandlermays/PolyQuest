namespace PolyQuest
{
    public interface IConditionChecker
    {
        bool? Evaluate(string predicate, string[] parameters);
    }
}