using System.Collections.Generic;
using EndlessRooms.Puzzles;
using NUnit.Framework;

namespace EndlessRooms.Tests.EditMode
{
    public class SwitchSequencePuzzleTests
    {
        [Test]
        public void Activate_InCorrectOrder_Solves()
        {
            var puzzle = new SwitchSequencePuzzle(new List<int> { 2, 0, 1 });
            bool solvedRaised = false;
            puzzle.Solved += () => solvedRaised = true;

            puzzle.Activate(2);
            puzzle.Activate(0);
            Assert.IsFalse(puzzle.IsSolved, "Should not solve before the full sequence is entered.");

            puzzle.Activate(1);

            Assert.IsTrue(puzzle.IsSolved);
            Assert.IsTrue(solvedRaised);
        }

        [Test]
        public void Activate_WithWrongIndex_ResetsProgressWithoutSolving()
        {
            var puzzle = new SwitchSequencePuzzle(new List<int> { 2, 0, 1 });
            bool mismatchedRaised = false;
            puzzle.Mismatched += () => mismatchedRaised = true;

            puzzle.Activate(2);
            puzzle.Activate(1); // wrong — expected 0 next

            Assert.IsTrue(mismatchedRaised);
            Assert.AreEqual(0, puzzle.ProgressCount, "A wrong activation must clear progress.");
            Assert.IsFalse(puzzle.IsSolved);

            // Solving still works after a mismatch reset.
            puzzle.Activate(2);
            puzzle.Activate(0);
            puzzle.Activate(1);
            Assert.IsTrue(puzzle.IsSolved);
        }

        [Test]
        public void Activate_AfterAlreadySolved_DoesNothing()
        {
            var puzzle = new SwitchSequencePuzzle(new List<int> { 0, 1 });
            puzzle.Activate(0);
            puzzle.Activate(1);
            Assert.IsTrue(puzzle.IsSolved);

            puzzle.Activate(1); // wrong index, but puzzle is already solved

            Assert.IsTrue(puzzle.IsSolved, "An already-solved puzzle must never un-solve.");
        }

        [Test]
        public void Reset_ClearsProgressAndSolvedState()
        {
            var puzzle = new SwitchSequencePuzzle(new List<int> { 0, 1 });
            puzzle.Activate(0);
            puzzle.Activate(1);
            Assert.IsTrue(puzzle.IsSolved);

            puzzle.Reset();

            Assert.IsFalse(puzzle.IsSolved);
            Assert.AreEqual(0, puzzle.ProgressCount);
        }

        [Test]
        public void RestoreProgress_ThenActivate_ContinuesCorrectly()
        {
            var puzzle = new SwitchSequencePuzzle(new List<int> { 2, 0, 1 });

            // Simulate a save/load: a fresh puzzle instance restored to "already pulled switch 2."
            puzzle.RestoreProgress(new List<int> { 2 }, isSolved: false);

            Assert.AreEqual(1, puzzle.ProgressCount);
            CollectionAssert.AreEqual(new[] { 2 }, puzzle.Progress);
            Assert.IsFalse(puzzle.IsSolved);

            puzzle.Activate(0);
            puzzle.Activate(1);

            Assert.IsTrue(puzzle.IsSolved);
        }

        [Test]
        public void RestoreProgress_WithIsSolvedTrue_DoesNotRaiseSolved()
        {
            var puzzle = new SwitchSequencePuzzle(new List<int> { 0, 1 });
            bool solvedRaised = false;
            puzzle.Solved += () => solvedRaised = true;

            puzzle.RestoreProgress(new List<int> { 0, 1 }, isSolved: true);

            Assert.IsTrue(puzzle.IsSolved);
            Assert.IsFalse(solvedRaised, "Restoring an already-solved state is not the same event as solving it live.");
        }

        [Test]
        public void GenerateSequence_IsDeterministicForASeed_AndIsAPermutation()
        {
            IReadOnlyList<int> first = SwitchSequencePuzzle.GenerateSequence(seed: 99, switchCount: 5);
            IReadOnlyList<int> second = SwitchSequencePuzzle.GenerateSequence(seed: 99, switchCount: 5);

            CollectionAssert.AreEqual(first, second, "The same seed must produce the same sequence.");
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4 }, first, "The sequence must be a permutation of every switch index.");
        }
    }
}
