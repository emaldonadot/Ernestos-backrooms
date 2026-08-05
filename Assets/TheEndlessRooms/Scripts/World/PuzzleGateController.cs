using System.Collections.Generic;
using EndlessRooms.Core;
using EndlessRooms.Puzzles;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Ties a set of <see cref="PuzzleSwitch"/> instances to a
    /// <see cref="SwitchSequencePuzzle"/> whose solution is derived from the level's
    /// seed, and unlocks the door leading to the exit once solved. Discovers that door
    /// itself from <see cref="ProceduralLevelBuilder.ExitDoor"/> rather than needing it
    /// wired by hand — one less Inspector reference that could be left unset.
    /// </summary>
    public sealed class PuzzleGateController : MonoBehaviour
    {
        [SerializeField] private ProceduralLevelBuilder _levelBuilder;
        [SerializeField] private PuzzleSwitch[] _switches;

        private SwitchSequencePuzzle _puzzle;
        private Door _doorToUnlock;

        private void Awake()
        {
            foreach (PuzzleSwitch switchInstance in _switches)
            {
                switchInstance.Activated += OnSwitchActivated;
            }

            if (_levelBuilder != null)
            {
                _levelBuilder.LevelBuilt += OnLevelBuilt;
            }
        }

        private void OnDestroy()
        {
            foreach (PuzzleSwitch switchInstance in _switches)
            {
                if (switchInstance != null)
                {
                    switchInstance.Activated -= OnSwitchActivated;
                }
            }

            if (_levelBuilder != null)
            {
                _levelBuilder.LevelBuilt -= OnLevelBuilt;
            }

            if (_puzzle != null)
            {
                _puzzle.Solved -= OnPuzzleSolved;
            }
        }

        private void OnLevelBuilt(EndlessRooms.Procedural.RoomGraph graph)
        {
            IReadOnlyList<int> sequence = SwitchSequencePuzzle.GenerateSequence(_levelBuilder.Seed, _switches.Length);
            _puzzle = new SwitchSequencePuzzle(sequence);
            _puzzle.Solved += OnPuzzleSolved;

            _doorToUnlock = _levelBuilder.ExitDoor;
            if (_doorToUnlock != null)
            {
                _doorToUnlock.SetLocked(true);
            }
            else
            {
                Debug.LogError($"[{nameof(PuzzleGateController)}] No exit door found to lock.", this);
            }
        }

        private void OnSwitchActivated(PuzzleSwitch switchInstance)
        {
            _puzzle?.Activate(switchInstance.SwitchIndex);
        }

        private void OnPuzzleSolved()
        {
            if (_doorToUnlock == null)
            {
                return;
            }

            var command = new UnlockDoorCommand(_doorToUnlock);
            if (GameServices.TryGet<WorldCommandExecutor>(out var executor))
            {
                executor.Submit(command);
            }
            else
            {
                Debug.LogWarning($"No {nameof(WorldCommandExecutor)} registered; executing '{command.CommandId}' directly.", this);
                command.Execute();
            }
        }
    }
}
