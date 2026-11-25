/*---------------------------
| File: IRestoreCompleted.cs
| Author: Chandler Mays
----------------------------*/

namespace PolyQuest.Saving
{
    // For an ISaveable whose restored value depends on what other components restore (health and mana depend on the
    // level, attributes and equipment). It's told once every entity in the load has been restored, so it can put its
    // saved value back exactly, whatever order the components restored in.
    public interface IRestoreCompleted
    {
        void OnRestoreCompleted();
    }
}