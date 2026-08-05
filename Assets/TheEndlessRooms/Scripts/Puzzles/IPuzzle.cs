using System;

namespace EndlessRooms.Puzzles
{
    /// <summary>
    /// Minimal contract for anything that gates a locked route: it can be solved, it
    /// announces when that happens, and it can be reset. Deliberately small so any
    /// future puzzle category (Section 11: symbols, light/sound patterns, etc.) can
    /// implement it without inheriting behavior it doesn't need.
    /// </summary>
    public interface IPuzzle
    {
        bool IsSolved { get; }

        event Action Solved;

        void Reset();
    }
}
