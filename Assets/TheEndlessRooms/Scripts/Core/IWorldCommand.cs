namespace EndlessRooms.Core
{
    /// <summary>
    /// A single world-mutating action (open a door, flip a switch, complete a puzzle
    /// step). Routing all such mutations through commands and a single
    /// <see cref="WorldCommandExecutor"/> gives future networking one seam to add
    /// authority checks at, instead of every call site that changes world state.
    /// </summary>
    public interface IWorldCommand
    {
        /// <summary>Stable identifier for logging/replay/future network dedup, e.g. "ToggleDoor:Door_04".</summary>
        string CommandId { get; }

        void Execute();
    }
}
