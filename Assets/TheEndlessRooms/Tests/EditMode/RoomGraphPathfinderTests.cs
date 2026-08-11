using System;
using EndlessRooms.AI;
using EndlessRooms.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class RoomGraphPathfinderTests
    {
        // A -- B -- C -- D  (linear chain; A/D also let hop-radius tests exercise branching-free graphs)
        private static (RoomGraph graph, Guid a, Guid b, Guid c, Guid d) MakeLinearGraph()
        {
            var graph = new RoomGraph();
            Guid a = Guid.NewGuid();
            Guid b = Guid.NewGuid();
            Guid c = Guid.NewGuid();
            Guid d = Guid.NewGuid();

            graph.AddNode(new RoomNode(a, null, new Vector2Int(0, 0)));
            graph.AddNode(new RoomNode(b, null, new Vector2Int(1, 0)));
            graph.AddNode(new RoomNode(c, null, new Vector2Int(2, 0)));
            graph.AddNode(new RoomNode(d, null, new Vector2Int(3, 0)));

            graph.AddConnection(new RoomConnection(a, b, Direction.East));
            graph.AddConnection(new RoomConnection(b, c, Direction.East));
            graph.AddConnection(new RoomConnection(c, d, Direction.East));

            return (graph, a, b, c, d);
        }

        [Test]
        public void FindPath_LinearGraph_ReturnsShortestOrderedPath()
        {
            (RoomGraph graph, Guid a, Guid b, Guid c, Guid d) = MakeLinearGraph();

            var path = RoomGraphPathfinder.FindPath(graph, a, d);

            Assert.AreEqual(new[] { a, b, c, d }, path);
        }

        [Test]
        public void FindPath_SameStartAndEnd_ReturnsSingleNode()
        {
            (RoomGraph graph, Guid a, _, _, _) = MakeLinearGraph();

            var path = RoomGraphPathfinder.FindPath(graph, a, a);

            Assert.AreEqual(new[] { a }, path);
        }

        [Test]
        public void FindPath_Unreachable_ReturnsEmpty()
        {
            var graph = new RoomGraph();
            Guid isolated = Guid.NewGuid();
            graph.AddNode(new RoomNode(isolated, null, Vector2Int.zero));
            (RoomGraph linearGraph, Guid a, _, _, Guid d) = MakeLinearGraph();
            // Merge: reuse the linear graph's node 'a' as the search source against an isolated target it has no path to.
            var path = RoomGraphPathfinder.FindPath(linearGraph, a, isolated);

            Assert.IsEmpty(path);
        }

        [Test]
        public void GetNodesWithinHops_ZeroHops_ReturnsOnlySelf()
        {
            (RoomGraph graph, Guid a, _, _, _) = MakeLinearGraph();

            var nodes = RoomGraphPathfinder.GetNodesWithinHops(graph, a, 0);

            Assert.AreEqual(new[] { a }, nodes);
        }

        [Test]
        public void GetNodesWithinHops_TwoHops_ReturnsExpectedSet()
        {
            (RoomGraph graph, Guid a, Guid b, Guid c, Guid d) = MakeLinearGraph();

            var nodes = RoomGraphPathfinder.GetNodesWithinHops(graph, a, 2);

            CollectionAssert.AreEquivalent(new[] { a, b, c }, nodes);
            CollectionAssert.DoesNotContain(nodes, d);
        }

        [Test]
        public void FindNearestNode_ReturnsClosestByWorldPosition()
        {
            (RoomGraph graph, Guid a, Guid b, Guid c, Guid d) = MakeLinearGraph();
            Vector3 NodeToWorld(Guid id) => new(graph.GetNode(id).GridPosition.x * 6f, 0f, 0f);

            Guid nearest = RoomGraphPathfinder.FindNearestNode(graph, new Vector3(11f, 0f, 0f), NodeToWorld);

            Assert.AreEqual(c, nearest);
        }
    }
}
