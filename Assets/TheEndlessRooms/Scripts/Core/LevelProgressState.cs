using System.Collections.Generic;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Pure completed-levels tracking — no <see cref="UnityEngine.MonoBehaviour"/> or
    /// file I/O dependency, so it's EditMode-testable directly, the same way
    /// <see cref="InventoryState"/> separates from its MonoBehaviour wrapper
    /// (<see cref="LevelProgressService"/>).
    /// </summary>
    public sealed class LevelProgressState
    {
        private readonly HashSet<string> _completedLevelIds = new();

        public IReadOnlyCollection<string> CompletedLevelIds => _completedLevelIds;

        public bool IsCompleted(string levelId)
        {
            return !string.IsNullOrEmpty(levelId) && _completedLevelIds.Contains(levelId);
        }

        public void MarkCompleted(string levelId)
        {
            if (!string.IsNullOrEmpty(levelId))
            {
                _completedLevelIds.Add(levelId);
            }
        }

        /// <summary>Whether <paramref name="levelId"/> is unlocked in <paramref name="catalog"/> — index 0 always is; index N needs index N-1 completed.</summary>
        public bool IsUnlocked(string levelId, LevelCatalog catalog)
        {
            if (catalog == null || catalog.Levels == null)
            {
                return false;
            }

            for (int i = 0; i < catalog.Levels.Length; i++)
            {
                if (catalog.Levels[i] == null || catalog.Levels[i].LevelId != levelId)
                {
                    continue;
                }

                return i == 0 || IsCompleted(catalog.Levels[i - 1].LevelId);
            }

            return false;
        }

        public void ReplaceWith(IEnumerable<string> completedLevelIds)
        {
            _completedLevelIds.Clear();
            if (completedLevelIds == null)
            {
                return;
            }

            foreach (string id in completedLevelIds)
            {
                MarkCompleted(id);
            }
        }
    }
}
