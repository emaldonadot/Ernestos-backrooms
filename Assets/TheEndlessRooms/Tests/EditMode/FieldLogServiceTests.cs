using System.Collections.Generic;
using System.Linq;
using EndlessRooms.Map;
using EndlessRooms.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class FieldLogServiceTests
    {
        private RoomDefinition _standard;
        private RoomGraph _graph;
        private RoomNode _roomA;
        private RoomNode _roomB;
        private RoomNode _roomC;
        private FieldLogService _service;

        [SetUp]
        public void BuildFixtureGraph()
        {
            _standard = ScriptableObject.CreateInstance<RoomDefinition>();
            _standard.Category = RoomCategory.Standard;

            _graph = new RoomGraph();
            _roomA = new RoomNode(System.Guid.NewGuid(), _standard, new Vector2Int(0, 0));
            _roomB = new RoomNode(System.Guid.NewGuid(), _standard, new Vector2Int(1, 0));
            _roomC = new RoomNode(System.Guid.NewGuid(), _standard, new Vector2Int(2, 0));

            _graph.AddNode(_roomA);
            _graph.AddNode(_roomB);
            _graph.AddNode(_roomC);
            _graph.AddConnection(new RoomConnection(_roomA.Id, _roomB.Id, Direction.East));
            _graph.AddConnection(new RoomConnection(_roomB.Id, _roomC.Id, Direction.East));
            _graph.SetEntry(_roomA.Id);
            _graph.SetExit(_roomC.Id);

            _service = new FieldLogService();
            _service.Initialize(_graph);
        }

        [TearDown]
        public void Cleanup()
        {
            _service.Dispose();
            Object.DestroyImmediate(_standard);
        }

        private Dictionary<System.Guid, FieldLogRoomView> KnownRoomsById()
        {
            return _service.GetKnownRooms().ToDictionary(v => v.RoomId);
        }

        [Test]
        public void MarkRoomEntered_RevealsTheRoomAndGlimpsesItsNeighbors()
        {
            _service.MarkRoomEntered(_roomA.Id);

            var known = KnownRoomsById();

            Assert.AreEqual(RoomDiscoveryState.Entered, known[_roomA.Id].State);
            Assert.AreEqual(RoomCategory.Standard, known[_roomA.Id].Category);

            Assert.AreEqual(RoomDiscoveryState.Glimpsed, known[_roomB.Id].State);
            Assert.IsNull(known[_roomB.Id].Category, "Glimpsed rooms must not reveal category.");

            Assert.IsFalse(known.ContainsKey(_roomC.Id), "Rooms with no discovered neighbor must not appear at all.");
        }

        [Test]
        public void MarkRoomEntered_NeverRegressesAnAlreadyEnteredRoom()
        {
            _service.MarkRoomEntered(_roomA.Id);
            _service.MarkRoomEntered(_roomB.Id);
            _service.MarkRoomEntered(_roomC.Id);

            // Re-entering B (e.g. walking back through it) must not downgrade anything.
            _service.MarkRoomEntered(_roomB.Id);

            var known = KnownRoomsById();
            Assert.AreEqual(RoomDiscoveryState.Entered, known[_roomA.Id].State);
            Assert.AreEqual(RoomDiscoveryState.Entered, known[_roomB.Id].State);
            Assert.AreEqual(RoomDiscoveryState.Entered, known[_roomC.Id].State);
        }

        [Test]
        public void MarkRoomEntered_UpdatesCurrentRoomId()
        {
            _service.MarkRoomEntered(_roomA.Id);
            Assert.AreEqual(_roomA.Id, _service.CurrentRoomId);

            _service.MarkRoomEntered(_roomB.Id);
            Assert.AreEqual(_roomB.Id, _service.CurrentRoomId);
        }

        [Test]
        public void GetKnownConnections_OnlyReturnsConnectionsTouchingAKnownRoom()
        {
            _service.MarkRoomEntered(_roomA.Id);

            var connections = _service.GetKnownConnections().ToList();

            Assert.IsTrue(connections.Any(c => c.FromId == _roomA.Id && c.ToId == _roomB.Id));
            Assert.IsFalse(connections.Any(c => c.FromId == _roomB.Id && c.ToId == _roomC.Id), "B-C is unknown until B or C is at least glimpsed/entered.");
        }

        [Test]
        public void AddMark_ThenRemoveMark_RoundTrips()
        {
            FieldMark mark = _service.AddMark(_roomA.Id, Vector2.zero, FieldMarkType.Danger, "growling sound");
            Assert.AreEqual(1, _service.Marks.Count);

            bool removed = _service.RemoveMark(mark.Id);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, _service.Marks.Count);
        }
    }
}
