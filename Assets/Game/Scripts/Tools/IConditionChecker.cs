/*---------------------------
File: IConditionChecker.cs
Author: Chandler Mays
----------------------------*/
namespace PolyQuest.Tools
{
    public enum PredicateType
    {
        kSelect,
        kHasQuest,
        kDoesNotHaveQuest,
        kCompletedObjective,
        kCompletedQuest,
        kHasLevel,
        kHasItem,
        kHasItems,
        kHasItemEquipped,
        //... add more as needed
    }

    public interface IConditionChecker
    {
        bool? Evaluate(PredicateType predicate, string[] parameters);
    }
}