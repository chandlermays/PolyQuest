namespace PolyQuest.Saving
{
    /* ---------------------------------------------------------------------------------------------
     * Role: Defines a contract for components that support saving and restoring their state.       *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Provides methods to capture the current state of a component for persistence.         *
     *      - Provides methods to restore a component's state from saved data.                      *
     *      - Enables integration with the save system for serializing and deserializing objects.   *
     *      - Ensures consistent save/load behavior across different game systems.                  *
     * -------------------------------------------------------------------------------------------- */
    public interface ISaveable
    {
        object CaptureState();
        void RestoreState(object state);
    }
}