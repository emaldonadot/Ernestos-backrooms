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

        private bool _isInitialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// The actual wiring logic, exposed publicly so headless Edit-mode tooling can
        /// call it directly — batch-mode Unity without a Play session doesn't reliably
        /// run Awake for scene-resident objects. Real gameplay always goes through
        /// Awake. Idempotent: a second call is a no-op.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

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

        public PuzzleGateSaveState CaptureState()
        {
            return new PuzzleGateSaveState
            {
                IsSolved = _puzzle != null && _puzzle.IsSolved,
                Progress = _puzzle != null ? new List<int>(_puzzle.Progress) : new List<int>(),
            };
        }

        /// <summary>Must be called after the level (and therefore this controller's puzzle) has been built — see <see cref="OnLevelBuilt"/>.</summary>
        public void RestoreState(PuzzleGateSaveState state)
        {
            if (_puzzle == null)
            {
                Debug.LogWarning($"[{nameof(PuzzleGateController)}] Cannot restore puzzle state before the level has been built.", this);
                return;
            }

            _puzzle.RestoreProgress(state.Progress, state.IsSolved);

            if (state.IsSolved && _doorToUnlock != null)
            {
                _doorToUnlock.SetLocked(false);
            }
        }
    }

    [System.Serializable]
    public struct PuzzleGateSaveState
    {
        public bool IsSolved;
        public List<int> Progress;
    }
}
