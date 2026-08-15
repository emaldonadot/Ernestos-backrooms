using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Cross-level, cross-session completion tracking — deliberately separate from
    /// <c>EndlessRooms.Persistence.SaveService</c>, which snapshots one procedural
    /// level's in-progress state (seed, doors, discovered rooms) and doesn't apply to a
    /// fixed hand-authored level like Level 1 at all. This is a single small JSON file
    /// (just a list of completed level IDs) at <see cref="Application.persistentDataPath"/>,
    /// which resolves to a real writable per-app directory on both PC and Quest/Android,
    /// so no platform-specific storage code is needed.
    /// Not a DontDestroyOnLoad singleton — every scene that needs it (the menu, or a
    /// level's own LevelCompletionRecorder) creates its own instance and reloads fresh
    /// from disk in <see cref="Awake"/>, registering into <see cref="GameServices"/>
    /// (which overwrites any stale reference from a previous scene automatically). The
    /// file is tiny, so re-reading it once per scene load costs nothing.
    /// </summary>
    public sealed class LevelProgressService : MonoBehaviour
    {
        private const string SaveFileName = "level_progress.json";

        private readonly LevelProgressState _state = new();

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public LevelProgressState State => _state;

        private void Awake()
        {
            Load();
            GameServices.Register(this);
        }

        public bool IsCompleted(string levelId) => _state.IsCompleted(levelId);

        public bool IsUnlocked(string levelId, LevelCatalog catalog) => _state.IsUnlocked(levelId, catalog);

        public void MarkCompleted(string levelId)
        {
            _state.MarkCompleted(levelId);
            Save();
        }

        private void Load()
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                _state.ReplaceWith(data?.CompletedLevelIds);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LevelProgressService] Could not read '{SavePath}': {exception.Message}");
            }
        }

        private void Save()
        {
            var data = new SaveData { CompletedLevelIds = new List<string>(_state.CompletedLevelIds) };
            File.WriteAllText(SavePath, JsonUtility.ToJson(data));
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> CompletedLevelIds = new();
        }
    }
}
