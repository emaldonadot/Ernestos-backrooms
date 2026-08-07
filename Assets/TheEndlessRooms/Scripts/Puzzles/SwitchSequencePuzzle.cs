using System;
using System.Collections.Generic;

namespace EndlessRooms.Puzzles
{
    /// <summary>
    /// "Activate these switches in the right order" (PRD Section 11). The solution is
    /// a permutation of switch indices, generated from the level seed via
    /// <see cref="GenerateSequence"/> so the same seed always poses the same puzzle.
    /// Pure C# — no <c>UnityEngine</c> dependency — so it's testable without a scene.
    /// </summary>
    public sealed class SwitchSequencePuzzle : IPuzzle
    {
        private readonly IReadOnlyList<int> _requiredSequence;
        private readonly List<int> _progress = new();

        public SwitchSequencePuzzle(IReadOnlyList<int> requiredSequence)
        {
            if (requiredSequence == null || requiredSequence.Count == 0)
            {
                throw new ArgumentException("A switch sequence puzzle needs at least one switch.", nameof(requiredSequence));
            }

            _requiredSequence = requiredSequence;
        }

        public bool IsSolved { get; private set; }

        public event Action Solved;

        /// <summary>Raised when a switch is activated out of order — the environmental cue that something reset.</summary>
        public event Action Mismatched;

        public IReadOnlyList<int> RequiredSequence => _requiredSequence;
        public int ProgressCount => _progress.Count;

        /// <summary>The indices activated so far, in order — what save data needs to resume a partial sequence exactly, not just remember a count.</summary>
        public IReadOnlyList<int> Progress => _progress;

        /// <summary>Restores a previously saved partial (or complete) attempt without re-raising <see cref="Solved"/> — the caller already knows the outcome and, if solved, has its own record of the door already being unlocked.</summary>
        public void RestoreProgress(IReadOnlyList<int> progress, bool isSolved)
        {
            _progress.Clear();
            if (progress != null)
            {
                _progress.AddRange(progress);
            }

            IsSolved = isSolved;
        }

        /// <summary>Reports one switch being activated. Ignored once the puzzle is already solved.</summary>
        public void Activate(int switchIndex)
        {
            if (IsSolved)
            {
                return;
            }

            int expectedIndex = _requiredSequence[_progress.Count];
            if (switchIndex != expectedIndex)
            {
                _progress.Clear();
                Mismatched?.Invoke();
                return;
            }

            _progress.Add(switchIndex);

            if (_progress.Count == _requiredSequence.Count)
            {
                IsSolved = true;
                Solved?.Invoke();
            }
        }

        public void Reset()
        {
            _progress.Clear();
            IsSolved = false;
        }

        /// <summary>Deterministically shuffles 0..switchCount-1 with a seeded RNG — never <c>UnityEngine.Random</c>.</summary>
        public static IReadOnlyList<int> GenerateSequence(int seed, int switchCount)
        {
            if (switchCount <= 0)
            {
                throw new ArgumentException("switchCount must be positive.", nameof(switchCount));
            }

            var sequence = new List<int>(switchCount);
            for (int i = 0; i < switchCount; i++)
            {
                sequence.Add(i);
            }

            var rng = new Random(seed);
            for (int i = sequence.Count - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                (sequence[i], sequence[swapIndex]) = (sequence[swapIndex], sequence[i]);
            }

            return sequence;
        }
    }
}
