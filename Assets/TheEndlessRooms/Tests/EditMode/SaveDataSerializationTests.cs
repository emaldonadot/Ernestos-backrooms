using System.Collections.Generic;
using EndlessRooms.Map;
using EndlessRooms.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class SaveDataSerializationTests
    {
        [Test]
        public void SaveData_RoundTripsThroughJsonUtility()
        {
            var original = new SaveData
            {
                Seed = 12345,
                PlayerPosition = new Vector3(1.5f, 2f, -3.25f),
            };

            original.Saveables.Add(new SaveableEntry
            {
                SaveId = "Door_abc_def",
                TypeId = "Door",
                StateJson = "{\"IsOpen\":true,\"IsLocked\":false}",
            });

            original.DiscoveredRooms.Add(new RoomDiscoverySaveEntry
            {
                RoomId = System.Guid.NewGuid().ToString(),
                State = RoomDiscoveryState.Entered,
            });

            original.Marks.Add(new FieldMarkSaveEntry
            {
                MarkId = System.Guid.NewGuid().ToString(),
                RoomId = System.Guid.NewGuid().ToString(),
                LocalOffset = new Vector2(0.1f, 0.2f),
                Type = FieldMarkType.Danger,
                Note = "growling sound",
                OwnerId = "local",
            });

            original.Puzzle.IsSolved = true;
            original.Puzzle.Progress = new List<int> { 2, 0, 1 };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(SaveData.CurrentVersion, restored.Version);
            Assert.AreEqual(original.Seed, restored.Seed);
            Assert.AreEqual(original.PlayerPosition, restored.PlayerPosition);

            Assert.AreEqual(1, restored.Saveables.Count);
            Assert.AreEqual(original.Saveables[0].SaveId, restored.Saveables[0].SaveId);
            Assert.AreEqual(original.Saveables[0].TypeId, restored.Saveables[0].TypeId);
            Assert.AreEqual(original.Saveables[0].StateJson, restored.Saveables[0].StateJson);

            Assert.AreEqual(1, restored.DiscoveredRooms.Count);
            Assert.AreEqual(original.DiscoveredRooms[0].RoomId, restored.DiscoveredRooms[0].RoomId);
            Assert.AreEqual(RoomDiscoveryState.Entered, restored.DiscoveredRooms[0].State);

            Assert.AreEqual(1, restored.Marks.Count);
            Assert.AreEqual(original.Marks[0].Note, restored.Marks[0].Note);
            Assert.AreEqual(FieldMarkType.Danger, restored.Marks[0].Type);
            Assert.AreEqual(original.Marks[0].LocalOffset, restored.Marks[0].LocalOffset);

            Assert.IsTrue(restored.Puzzle.IsSolved);
            CollectionAssert.AreEqual(new[] { 2, 0, 1 }, restored.Puzzle.Progress);
        }

        [Test]
        public void DoorState_RoundTripsThroughJsonUtility()
        {
            var original = new EndlessRooms.World.Door.DoorState(isOpen: true, isLocked: false);

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<EndlessRooms.World.Door.DoorState>(json);

            Assert.AreEqual(original.IsOpen, restored.IsOpen);
            Assert.AreEqual(original.IsLocked, restored.IsLocked);
        }
    }
}
