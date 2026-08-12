using System;
using EndlessRooms.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class LandmarkNodeSelectorTests
    {
        // Entry -- B -- C -- D -- Exit  (linear chain; C also connects to E, making it
        // the most-connected non-entry/non-exit node once far enough from entry)
        //                |
        //                E
        private static (RoomGraph graph, Guid entry, Guid b, Guid c, Guid d, Guid e, Guid exit) MakeBranchingGraph()
        {
            var graph = new RoomGraph();
            Guid entry = Guid.NewGuid();
            Guid b = Guid.NewGuid();
            Guid c = Guid.NewGuid();
            Guid d = Guid.NewGuid();
            Guid e = Guid.NewGuid();
            Guid exit = Guid.NewGuid();

            graph.AddNode(new RoomNode(entry, null, new Vector2Int(0, 0)));
            graph.AddNode(new RoomNode(b, null, new Vector2Int(1, 0)));
            graph.AddNode(new RoomNode(c, null, new Vector2Int(2, 0)));
            graph.AddNode(new RoomNode(d, null, new Vector2Int(3, 0)));
            graph.AddNode(new RoomNode(e, null, new Vector2Int(2, 1)));
            graph.AddNode(new RoomNode(exit, null, new Vector2Int(4, 0)));

            graph.AddConnection(new RoomConnection(entry, b, Direction.East));
            graph.AddConnection(new RoomConnection(b, c, Direction.East));
            graph.AddConnection(new RoomConnection(c, d, Direction.East));
            graph.AddConnection(new RoomConnection(c, e, Direction.North));
            graph.AddConnection(new RoomConnection(d, exit, Direction.East));

            graph.SetEntry(entry);
            graph.SetExit(exit);

            return (graph, entry, b, c, d, e, exit);
        }

        [Test]
        public void SelectLandmarkNode_PicksMostConnectedEligibleNode()
        {
            (RoomGraph graph, _, _, Guid c, _, _, _) = MakeBranchingGraph();

            Guid? selected = LandmarkNodeSelector.SelectLandmarkNode(graph, minHopsFromEntry: 1);

            Assert.AreEqual(c, selected);
        }

        [Test]
        public void SelectLandmarkNode_NeverPicksEntryOrExit()
        {
            (RoomGraph graph, Guid entry, _, _, _, _, Guid exit) = MakeBranchingGraph();

            Guid? selected = LandmarkNodeSelector.SelectLandmarkNode(graph, minHopsFromEntry: 1);

            Assert.AreNotEqual(entry, selected);
            Assert.AreNotEqual(exit, selected);
        }

        [Test]
        public void SelectLandmarkNode_RespectsMinHopsFromEntry()
        {
            (RoomGraph graph, _, Guid b, Guid c, _, _, _) = MakeBranchingGraph();

            // With a 3-hop minimum, B (1 hop) and C (2 hops) are both excluded even
            // though C has the most connections — only D (3 hops) qualifies.
            Guid? selected = LandmarkNodeSelector.SelectLandmarkNode(graph, minHopsFromEntry: 3);

            Assert.AreNotEqual(b, selected);
            Assert.AreNotEqual(c, selected);
        }

        [Test]
        public void SelectLandmarkNode_NoEligibleNode_ReturnsNull()
        {
            var graph = new RoomGraph();
            Guid entry = Guid.NewGuid();
            Guid exit = Guid.NewGuid();
            graph.AddNode(new RoomNode(entry, null, new Vector2Int(0, 0)));
            graph.AddNode(new RoomNode(exit, null, new Vector2Int(1, 0)));
            graph.AddConnection(new RoomConnection(entry, exit, Direction.East));
            graph.SetEntry(entry);
            graph.SetExit(exit);

            Guid? selected = LandmarkNodeSelector.SelectLandmarkNode(graph, minHopsFromEntry: 1);

            Assert.IsNull(selected);
        }

        [Test]
        public void SelectLandmarkNode_IsDeterministicAcrossRepeatedCalls()
        {
            (RoomGraph graph, _, _, _, _, _, _) = MakeBranchingGraph();

            Guid? first = LandmarkNodeSelector.SelectLandmarkNode(graph, minHopsFromEntry: 1);
            Guid? second = LandmarkNodeSelector.SelectLandmarkNode(graph, minHopsFromEntry: 1);

            Assert.AreEqual(first, second);
        }
    }
}
