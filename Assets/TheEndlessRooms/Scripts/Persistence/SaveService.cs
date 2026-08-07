using System;
using System.Collections.Generic;
using System.IO;
using EndlessRooms.Core;
using EndlessRooms.Map;
using EndlessRooms.World;
using UnityEngine;

namespace EndlessRooms.Persistence
{
    /// <summary>
    /// Saves/loads by regenerating the deterministic world from its seed, then
    /// reattaching every saved per-object state to the freshly-instantiated objects —
    /// never by snapshotting scene geometry. <see cref="Load"/> must run after the
    /// scene's other bootstrap components (<c>MapBootstrap</c>, <c>PuzzleGateController</c>)
    /// have wired themselves to <see cref="World.ProceduralLevelBuilder.LevelBuilt"/>,
    /// since it calls <see cref="World.ProceduralLevelBuilder.BuildLevel(int)"/> itself
    /// and relies on those components reacting to rebuild their own state first.
    /// </summary>
    public sealed class SaveService : MonoBehaviour
    {
        [SerializeField] private ProceduralLevelBuilder _levelBuilder;
        [SerializeField] private PuzzleGateController _puzzleGate;
        [SerializeField] private Transform _player;
        [SerializeField] private CharacterController _playerCharacterController;
        [SerializeField] private string _saveFileName = "save1.json";

        private string SavePath => Path.Combine(Application.persistentDataPath, _saveFileName);

        public bool HasSaveFile() => File.Exists(SavePath);

        public void Save()
        {
            var data = new SaveData
            {
                Seed = _levelBuilder.Seed,
                PlayerPosition = _player != null ? _player.position : Vector3.zero,
            };

            CaptureSaveables(data);
            CaptureFieldLog(data);
            CapturePuzzle(data);

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[SaveService] Saved to '{SavePath}'.");
        }

        public void Load()
        {
            if (!HasSaveFile())
            {
                Debug.LogWarning("[SaveService] No save file to load.");
                return;
            }

            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data.Version != SaveData.CurrentVersion)
            {
                Debug.LogWarning($"[SaveService] Save file is version {data.Version}; current is {SaveData.CurrentVersion}. Loading best-effort.");
            }

            _levelBuilder.BuildLevel(data.Seed);

            RestoreSaveables(data);
            RestoreFieldLog(data);
            RestorePuzzle(data);
            RestorePlayerPosition(data);

            Debug.Log($"[SaveService] Loaded from '{SavePath}'.");
        }

        private static void CaptureSaveables(SaveData data)
        {
            if (!GameServices.TryGet<SaveableRegistry>(out var registry))
            {
                return;
            }

            foreach (ISaveable saveable in registry.GetAll())
            {
                string typeId = TypeIdFor(saveable);
                if (typeId == null)
                {
                    continue;
                }

                data.Saveables.Add(new SaveableEntry
                {
                    SaveId = saveable.SaveId,
                    TypeId = typeId,
                    StateJson = JsonUtility.ToJson(saveable.CaptureState()),
                });
            }
        }

        private static void CaptureFieldLog(SaveData data)
        {
            if (!GameServices.TryGet<FieldLogService>(out var fieldLog))
            {
                return;
            }

            foreach (FieldLogRoomView room in fieldLog.GetKnownRooms())
            {
                data.DiscoveredRooms.Add(new RoomDiscoverySaveEntry
                {
                    RoomId = room.RoomId.ToString(),
                    State = room.State,
                });
            }

            foreach (FieldMark mark in fieldLog.Marks)
            {
                data.Marks.Add(new FieldMarkSaveEntry
                {
                    MarkId = mark.Id.ToString(),
                    RoomId = mark.RoomId.ToString(),
                    LocalOffset = mark.LocalOffset,
                    Type = mark.Type,
                    Note = mark.Note,
                    OwnerId = mark.OwnerId,
                });
            }
        }

        private void CapturePuzzle(SaveData data)
        {
            if (_puzzleGate == null)
            {
                return;
            }

            PuzzleGateSaveState state = _puzzleGate.CaptureState();
            data.Puzzle.IsSolved = state.IsSolved;
            data.Puzzle.Progress = state.Progress;
        }

        private static void RestoreSaveables(SaveData data)
        {
            if (!GameServices.TryGet<SaveableRegistry>(out var registry))
            {
                return;
            }

            var byId = new Dictionary<string, ISaveable>();
            foreach (ISaveable saveable in registry.GetAll())
            {
                byId[saveable.SaveId] = saveable;
            }

            foreach (SaveableEntry entry in data.Saveables)
            {
                if (!byId.TryGetValue(entry.SaveId, out ISaveable saveable))
                {
                    continue;
                }

                object state = DeserializeState(entry.TypeId, entry.StateJson);
                if (state != null)
                {
                    saveable.RestoreState(state);
                }
            }
        }

        private static void RestoreFieldLog(SaveData data)
        {
            if (!GameServices.TryGet<FieldLogService>(out var fieldLog))
            {
                return;
            }

            foreach (RoomDiscoverySaveEntry entry in data.DiscoveredRooms)
            {
                fieldLog.RestoreDiscoveryState(Guid.Parse(entry.RoomId), entry.State);
            }

            foreach (FieldMarkSaveEntry entry in data.Marks)
            {
                fieldLog.RestoreMark(new FieldMark(Guid.Parse(entry.MarkId), Guid.Parse(entry.RoomId), entry.LocalOffset, entry.Type, entry.Note, entry.OwnerId));
            }

            Guid? lastEntered = FindLastEnteredRoom(data.DiscoveredRooms);
            if (lastEntered.HasValue)
            {
                fieldLog.RestoreCurrentRoomId(lastEntered.Value);
            }
        }

        private void RestorePuzzle(SaveData data)
        {
            if (_puzzleGate == null)
            {
                return;
            }

            _puzzleGate.RestoreState(new PuzzleGateSaveState
            {
                IsSolved = data.Puzzle.IsSolved,
                Progress = data.Puzzle.Progress,
            });
        }

        private void RestorePlayerPosition(SaveData data)
        {
            if (_player == null)
            {
                return;
            }

            if (_playerCharacterController != null)
            {
                _playerCharacterController.enabled = false;
            }

            _player.position = data.PlayerPosition;

            if (_playerCharacterController != null)
            {
                _playerCharacterController.enabled = true;
            }
        }

        private static string TypeIdFor(ISaveable saveable)
        {
            return saveable switch
            {
                Door => "Door",
                PickupTestItem => "PickupItem",
                _ => null,
            };
        }

        private static object DeserializeState(string typeId, string json)
        {
            return typeId switch
            {
                "Door" => JsonUtility.FromJson<Door.DoorState>(json),
                "PickupItem" => JsonUtility.FromJson<PickupTestItem.PickupState>(json),
                _ => null,
            };
        }

        /// <summary>
        /// The save schema doesn't separately track "current room"; this is a
        /// best-effort pick (the last Entered room in save order) whose only practical
        /// effect is which room the map highlights immediately after a load, before the
        /// player moves again.
        /// </summary>
        private static Guid? FindLastEnteredRoom(List<RoomDiscoverySaveEntry> entries)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].State == RoomDiscoveryState.Entered)
                {
                    return Guid.Parse(entries[i].RoomId);
                }
            }

            return null;
        }
    }
}
