/*---------------------------
| File: IConditionChecker.cs
| Author: Chandler Mays
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
        kWorldFlag,
        //... add more at the end only: assets store these by number, so never insert or reorder
    }

    public interface IConditionChecker
    {
        bool? Evaluate(ConditionType condition, string[] parameters);
    }
}