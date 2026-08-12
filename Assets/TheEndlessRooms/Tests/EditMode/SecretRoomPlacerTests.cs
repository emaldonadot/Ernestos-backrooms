using System;
using EndlessRooms.Procedural;
using EndlessRooms.World;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class SecretRoomPlacerTests
    {
        private static RoomGraph MakeGraphWithEntryAt(Vector2Int entryGridPosition, params Vector2Int[] otherOccupiedCells)
        {
            var graph = new RoomGraph();
            Guid entryId = Guid.NewGuid();
            graph.AddNode(new RoomNode(entryId, null, entryGridPosition));
            graph.SetEntry(entryId);

            foreach (Vector2Int cell in otherOccupiedCells)
            {
                graph.AddNode(new RoomNode(Guid.NewGuid(), null, cell));
            }

            return graph;
        }

        [Test]
        public void FindFreeSouthSteps_CellDirectlySouthIsFree_ReturnsOne()
        {
            RoomGraph graph = MakeGraphWithEntryAt(new Vector2Int(0, 0));

            int steps = SecretRoomPlacer.FindFreeSouthSteps(graph);

            Assert.AreEqual(1, steps);
        }

        [Test]
        public void FindFreeSouthSteps_CellDirectlySouthIsOccupied_SkipsToNextFreeCell()
        {
            // This is the exact bug this class exists to fix: Milestone 8's seed
            // placed a real procedural room at (0,-1), the same fixed offset the
            // secret room used to assume was always free, wedging the Attendant's
            // patrol point inside doubled-up geometry it could never move out of.
            RoomGraph graph = MakeGraphWithEntryAt(new Vector2Int(0, 0), new Vector2Int(0, -1));

            int steps = SecretRoomPlacer.FindFreeSouthSteps(graph);

            Assert.AreEqual(2, steps);
        }

        [Test]
        public void FindFreeSouthSteps_MultipleOccupiedCellsInARow_SkipsAllOfThem()
        {
            RoomGraph graph = MakeGraphWithEntryAt(new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2), new Vector2Int(0, -3));

            int steps = SecretRoomPlacer.FindFreeSouthSteps(graph);

            Assert.AreEqual(4, steps);
        }

        [Test]
        public void FindFreeSouthSteps_OccupiedCellsOffTheSouthLine_AreIgnored()
        {
            RoomGraph graph = MakeGraphWithEntryAt(new Vector2Int(0, 0), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(0, 1));

            int steps = SecretRoomPlacer.FindFreeSouthSteps(graph);

            Assert.AreEqual(1, steps);
        }

        [Test]
        public void FindFreeSouthSteps_NonOriginEntry_OffsetsRelativeToEntry()
        {
            RoomGraph graph = MakeGraphWithEntryAt(new Vector2Int(5, 5), new Vector2Int(5, 4));

            int steps = SecretRoomPlacer.FindFreeSouthSteps(graph);

            Assert.AreEqual(2, steps);
        }
    }
}
