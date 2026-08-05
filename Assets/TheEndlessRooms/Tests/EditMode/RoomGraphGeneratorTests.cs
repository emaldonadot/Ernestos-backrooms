using EndlessRooms.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class RoomGraphGeneratorTests
    {
        private RoomDefinition _entry;
        private RoomDefinition _exit;
        private RoomDefinition _corridor;
        private RoomDefinition _junction;
        private RoomDefinition _deadEnd;
        private RoomDefinition[] _fillers;

        [SetUp]
        public void CreateDefinitions()
        {
            _entry = CreateDefinition(RoomCategory.Standard, isMandatory: false);
            _exit = CreateDefinition(RoomCategory.Exit, isMandatory: true);
            _corridor = CreateDefinition(RoomCategory.Corridor, isMandatory: false);
            _junction = CreateDefinition(RoomCategory.Junction, isMandatory: false);
            _deadEnd = CreateDefinition(RoomCategory.DeadEnd, isMandatory: false);
            _fillers = new[] { _entry, _corridor, _junction, _deadEnd };

            // No AllowedNeighborCategories set on any definition, so every category is
            // compatible with every other — the generator's own connectivity/branching
            // logic is what these tests exercise, not category restrictions.
        }

        [TearDown]
        public void DestroyDefinitions()
        {
            foreach (var definition in new[] { _entry, _exit, _corridor, _junction, _deadEnd })
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static RoomDefinition CreateDefinition(RoomCategory category, bool isMandatory)
        {
            var definition = ScriptableObject.CreateInstance<RoomDefinition>();
            definition.Category = category;
            definition.IsMandatory = isMandatory;
            return definition;
        }

        private RoomGraphGenerationSettings CreateSettings(int seed, int roomCount = 12)
        {
            return new RoomGraphGenerationSettings
            {
                Seed = seed,
                RoomCount = roomCount,
                EntryDefinition = _entry,
                ExitDefinition = _exit,
                FillerDefinitions = _fillers,
            };
        }

        [Test]
        public void GenerateValidated_AcrossManySeeds_AlwaysProducesAValidGraph()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                RoomGraph graph = RoomGraphGenerator.GenerateValidated(CreateSettings(seed));
                RoomGraphValidationResult result = RoomGraphValidator.Validate(graph);

                Assert.IsTrue(result.IsValid, $"Seed {seed} produced an invalid graph (exit reachable: {result.ExitReachable}).");
                Assert.IsTrue(result.ExitReachable, $"Seed {seed}: exit was not reachable from entry.");
            }
        }

        [Test]
        public void Generate_WithSameSeed_ProducesIdenticalLayout()
        {
            RoomGraph first = RoomGraphGenerator.Generate(CreateSettings(42), seedOverride: 42);
            RoomGraph second = RoomGraphGenerator.Generate(CreateSettings(42), seedOverride: 42);

            var firstPositions = new System.Collections.Generic.HashSet<Vector2Int>();
            foreach (var node in first.Nodes)
            {
                firstPositions.Add(node.GridPosition);
            }

            var secondPositions = new System.Collections.Generic.HashSet<Vector2Int>();
            foreach (var node in second.Nodes)
            {
                secondPositions.Add(node.GridPosition);
            }

            Assert.AreEqual(first.Nodes.Count, second.Nodes.Count);
            Assert.AreEqual(first.Connections.Count, second.Connections.Count);
            CollectionAssert.AreEquivalent(firstPositions, secondPositions);
        }

        [Test]
        public void Generate_NeverPlacesTwoNodesOnTheSameGridCell()
        {
            RoomGraph graph = RoomGraphGenerator.Generate(CreateSettings(7), seedOverride: 7);

            var seenPositions = new System.Collections.Generic.HashSet<Vector2Int>();
            foreach (var node in graph.Nodes)
            {
                Assert.IsTrue(seenPositions.Add(node.GridPosition), $"Grid cell {node.GridPosition} is occupied by more than one room.");
            }
        }

        [Test]
        public void GenerateValidated_ThrowsWhenNoExitDefinitionCanEverConnect()
        {
            var isolatedExit = ScriptableObject.CreateInstance<RoomDefinition>();
            isolatedExit.Category = RoomCategory.Exit;
            isolatedExit.IsMandatory = true;

            // Every filler (including the entry) only reciprocates Corridor neighbors, so
            // nothing will ever connect back to the Standard-category entry room, let
            // alone to Exit. The graph can never have a reachable exit for any seed.
            foreach (var filler in _fillers)
            {
                filler.AllowedNeighborCategories = new[] { RoomCategory.Corridor };
            }

            var settings = new RoomGraphGenerationSettings
            {
                Seed = 1,
                RoomCount = 6,
                EntryDefinition = _entry,
                ExitDefinition = isolatedExit,
                FillerDefinitions = _fillers,
            };

            Assert.Throws<System.InvalidOperationException>(() => RoomGraphGenerator.GenerateValidated(settings, maxAttempts: 5));

            Object.DestroyImmediate(isolatedExit);
        }
    }
}
