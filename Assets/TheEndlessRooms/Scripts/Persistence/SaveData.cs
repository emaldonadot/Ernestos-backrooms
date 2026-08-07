using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRooms.Persistence
{
    /// <summary>
    /// The full save schema, serialized with <see cref="JsonUtility"/> (built into
    /// Unity — no third-party JSON package needed for a schema this simple).
    /// <see cref="System.Guid"/> has no public fields, so <c>JsonUtility</c> can't see
    /// it; every id below is stored as its <see cref="Guid.ToString()"/> instead.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int Seed;
        public Vector3 PlayerPosition;
        public List<SaveableEntry> Saveables = new();
        public List<RoomDiscoverySaveEntry> DiscoveredRooms = new();
        public List<FieldMarkSaveEntry> Marks = new();
        public PuzzleGateSaveEntry Puzzle = new();
    }

    /// <summary>One <see cref="Core.ISaveable"/>'s captured state. <see cref="TypeId"/> picks which concrete state type to deserialize <see cref="StateJson"/> into — <c>JsonUtility</c> has no polymorphic support, so this is an explicit, small switch in <see cref="SaveService"/> rather than reflection.</summary>
    [Serializable]
    public sealed class SaveableEntry
    {
        public string SaveId;
        public string TypeId;
        public string StateJson;
    }

    [Serializable]
    public sealed class RoomDiscoverySaveEntry
    {
        public string RoomId;
        public Map.RoomDiscoveryState State;
    }

    [Serializable]
    public sealed class FieldMarkSaveEntry
    {
        public string MarkId;
        public string RoomId;
        public Vector2 LocalOffset;
        public Map.FieldMarkType Type;
        public string Note;
        public string OwnerId;
    }

    [Serializable]
    public sealed class PuzzleGateSaveEntry
    {
        public bool IsSolved;
        public List<int> Progress = new();
    }
}
