using System.Collections.Generic;
using System.Linq;
using EndlessRooms.Procedural;
using EndlessRooms.World;
using NUnit.Framework;

namespace EndlessRooms.Tests.EditMode
{
    public class Level1RoomGraphProviderTests
    {
        // 9 main corridor cells + 2 cross-arm cells + 14 offices + 2 bathrooms + 2 courtyards.
        private const int ExpectedNodeCount = 9 + 2 + 14 + 2 + 2;

        [Test]
        public void BuildGraph_HasExpectedNodeCount()
        {
            RoomGraph graph = Level1RoomGraphProvider.BuildGraph();

            Assert.AreEqual(ExpectedNodeCount, graph.Nodes.Count);
        }

        [Test]
        public void BuildGraph_EveryNodeIsReachableFromEntry()
        {
            RoomGraph graph = Level1RoomGraphProvider.BuildGraph();

            var visited = new HashSet<System.Guid> { graph.EntryNodeId };
            var queue = new Queue<System.Guid>();
            queue.Enqueue(graph.EntryNodeId);

            while (queue.Count > 0)
            {
                System.Guid current = queue.Dequeue();
                foreach (System.Guid neighbor in graph.GetNeighborIds(current))
                {
                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            Assert.AreEqual(graph.Nodes.Count, visited.Count, "Every node in the fixed office building should be reachable from the entry.");
        }

        [Test]
        public void BuildGraph_ExitIsReachableFromEntry()
        {
            RoomGraph graph = Level1RoomGraphProvider.BuildGraph();

            RoomGraphValidationResult result = RoomGraphValidator.Validate(graph);

            Assert.IsTrue(result.ExitReachable);
        }

        [Test]
        public void BuildGraph_HasNoDuplicateGridPositions()
        {
            RoomGraph graph = Level1RoomGraphProvider.BuildGraph();

            var positions = graph.Nodes.Select(n => n.GridPosition).ToList();
            var distinctPositions = positions.Distinct().ToList();

            Assert.AreEqual(positions.Count, distinctPositions.Count, "Two rooms/corridor cells ended up mapped to the same grid position.");
        }

        [Test]
        public void BuildGraph_EntryAndExitAreDifferentNodes()
        {
            RoomGraph graph = Level1RoomGraphProvider.BuildGraph();

            Assert.AreNotEqual(graph.EntryNodeId, graph.ExitNodeId);
        }
    }
}
