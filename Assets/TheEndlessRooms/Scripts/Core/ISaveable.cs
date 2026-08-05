namespace EndlessRooms.Core
{
    /// <summary>
    /// Anything whose runtime state must survive a save/load cycle: doors, items,
    /// puzzles, discovered rooms, etc. <see cref="SaveId"/> must be stable across
    /// play sessions (and, later, across networked clients).
    /// </summary>
    public interface ISaveable
    {
        string SaveId { get; }

        object CaptureState();

        void RestoreState(object state);
    }
}
