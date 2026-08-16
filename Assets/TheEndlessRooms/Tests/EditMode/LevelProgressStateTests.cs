using EndlessRooms.Core;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    /// <summary>Covers the pure completion/unlock logic behind the main menu's level list (see LevelProgressState's own doc comment for why this is split from LevelProgressService).</summary>
    public class LevelProgressStateTests
    {
        private static LevelDefinition MakeLevel(string id)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = id;
            return level;
        }

        private static LevelCatalog MakeCatalog(params LevelDefinition[] levels)
        {
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            catalog.Levels = levels;
            return catalog;
        }

        [Test]
        public void IsCompleted_NeverMarked_ReturnsFalse()
        {
            var state = new LevelProgressState();

            Assert.IsFalse(state.IsCompleted("level1"));
        }

        [Test]
        public void MarkCompleted_ThenIsCompleted_ReturnsTrue()
        {
            var state = new LevelProgressState();

            state.MarkCompleted("level1");

            Assert.IsTrue(state.IsCompleted("level1"));
        }

        [Test]
        public void IsUnlocked_FirstLevelInCatalog_IsAlwaysUnlocked()
        {
            var state = new LevelProgressState();
            LevelCatalog catalog = MakeCatalog(MakeLevel("level1"), MakeLevel("level2"));

            Assert.IsTrue(state.IsUnlocked("level1", catalog));
        }

        [Test]
        public void IsUnlocked_SecondLevel_LockedUntilFirstCompleted()
        {
            var state = new LevelProgressState();
            LevelCatalog catalog = MakeCatalog(MakeLevel("level1"), MakeLevel("level2"));

            Assert.IsFalse(state.IsUnlocked("level2", catalog));

            state.MarkCompleted("level1");

            Assert.IsTrue(state.IsUnlocked("level2", catalog));
        }

        [Test]
        public void IsUnlocked_LevelNotInCatalog_ReturnsFalse()
        {
            var state = new LevelProgressState();
            LevelCatalog catalog = MakeCatalog(MakeLevel("level1"));

            Assert.IsFalse(state.IsUnlocked("does_not_exist", catalog));
        }

        [Test]
        public void ReplaceWith_LoadsCompletedIdsFromSave()
        {
            var state = new LevelProgressState();

            state.ReplaceWith(new[] { "level1", "level2" });

            Assert.IsTrue(state.IsCompleted("level1"));
            Assert.IsTrue(state.IsCompleted("level2"));
            Assert.IsFalse(state.IsCompleted("level3"));
        }

        [Test]
        public void ReplaceWith_Null_ClearsExistingProgress()
        {
            var state = new LevelProgressState();
            state.MarkCompleted("level1");

            state.ReplaceWith(null);

            Assert.IsFalse(state.IsCompleted("level1"));
        }
    }
}
