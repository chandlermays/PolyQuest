/*---------------------------
File: IConditionChecker.cs
Author: Chandler Mays
----------------------------*/
namespace PolyQuest.Tools
{
    public enum ConditionType
    {
        kSelect,
        kHasQuest,
        kCompletedObjective,
        kCompletedQuest,
        kHasLevel,
        kHasItem,
        kHasItems,
        kHasItemEquipped,
        kHasSpokenTo,
        //... add more as needed
    }

    public interface IConditionChecker
    {
        bool? Evaluate(ConditionType condition, string[] parameters);
    }
}