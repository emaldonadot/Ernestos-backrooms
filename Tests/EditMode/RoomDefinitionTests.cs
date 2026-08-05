using EndlessRooms.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class RoomDefinitionTests
    {
        [Test]
        public void AllowsNeighbor_WithNoRestrictions_ReturnsTrueForAnyCategory()
        {
            var definition = ScriptableObject.CreateInstance<RoomDefinition>();

            Assert.IsTrue(definition.AllowsNeighbor(RoomCategory.Puzzle));

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void AllowsNeighbor_WithRestrictions_OnlyAllowsListedCategories()
        {
            var definition = ScriptableObject.CreateInstance<RoomDefinition>();
            definition.AllowedNeighborCategories = new[] { RoomCategory.Corridor, RoomCategory.Standard };

            Assert.IsTrue(definition.AllowsNeighbor(RoomCategory.Corridor));
            Assert.IsFalse(definition.AllowsNeighbor(RoomCategory.Exit));

            Object.DestroyImmediate(definition);
        }
    }
}
