using System;

namespace EndlessRooms.World
{
    /// <summary>
    /// A boolean condition that starts locked and flips to unlocked exactly once —
    /// implemented by anything a <see cref="SpiderWebClue"/> needs to gate on besides a
    /// plain inventory-item check (a drawer's lock state, a safe's lock state, whether
    /// the UV flashlight's hidden code has been revealed yet). Kept generic so
    /// SpiderWebClue never needs to know which concrete system it's watching.
    /// </summary>
    public interface IProgressionGate
    {
        bool IsUnlocked { get; }

        event Action Changed;
    }
}
