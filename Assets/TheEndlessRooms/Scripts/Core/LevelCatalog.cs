using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// The ordered list of levels the main menu shows — order is significant: index 0 is
    /// always unlocked, and index N is unlocked once index N-1 is completed (see
    /// <see cref="LevelProgressService"/>). Levels aren't unlocked by any other rule
    /// (branching paths, item gates, etc.) — a straight progression matches the PRD's
    /// single-track level list as it stands today.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "The Endless Rooms/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelDefinition[] Levels = System.Array.Empty<LevelDefinition>();
    }
}
