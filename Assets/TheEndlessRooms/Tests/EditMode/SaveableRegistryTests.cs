using EndlessRooms.Core;
using EndlessRooms.World;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    /// <summary>
    /// Covers the plain-C# registry logic directly (Register/Unregister/GetAll), and
    /// Door's RestoreState. Deliberately does NOT test Door/PickupTestItem's OnEnable
    /// calling Register — MonoBehaviour Awake/OnEnable/Start/Update only run in Play
    /// mode (or with [ExecuteAlways]), never in Edit mode, which covers EditMode tests,
    /// bare -executeMethod scripts, and plain scene loading alike. That one-line wiring
    /// is confirmed by a real Play-mode session instead (see the Milestone 5 PR).
    /// </summary>
    public class SaveableRegistryTests
    {
        private sealed class FakeSaveable : ISaveable
        {
            public string SaveId => "Fake";
            public object CaptureState() => null;
            public void RestoreState(object state) { }
        }

        [Test]
        public void Register_ThenGetAll_ContainsTheSaveable()
        {
            var registry = new SaveableRegistry();
            var saveable = new FakeSaveable();

            registry.Register(saveable);

            CollectionAssert.Contains(registry.GetAll(), saveable);
        }

        [Test]
        public void Register_CalledTwiceForTheSameInstance_DoesNotDuplicate()
        {
            var registry = new SaveableRegistry();
            var saveable = new FakeSaveable();

            registry.Register(saveable);
            registry.Register(saveable);

            Assert.AreEqual(1, registry.GetAll().Count);
        }

        [Test]
        public void Unregister_RemovesTheSaveable()
        {
            var registry = new SaveableRegistry();
            var saveable = new FakeSaveable();
            registry.Register(saveable);

            registry.Unregister(saveable);

            CollectionAssert.DoesNotContain(registry.GetAll(), saveable);
        }

        [Test]
        public void Door_RestoreState_AppliesSavedOpenAndLockedFlags()
        {
            var go = new GameObject("TestDoor");
            var door = go.AddComponent<Door>();

            door.RestoreState(new Door.DoorState(isOpen: true, isLocked: true));

            Assert.IsTrue(door.IsOpen);
            Assert.IsTrue(door.IsLocked);

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
