using EndlessRooms.Core;
using EndlessRooms.World;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    /// <summary>
    /// Covers Milestone 9's named-item door lock (<c>Door.Interact</c>'s
    /// <c>TryUnlockWithRequiredItem</c>). Uses <c>RestoreState</c> to set the locked
    /// flag (public API) rather than the internal <c>SetLocked</c>, since
    /// EndlessRooms.Tests.EditMode is a separate assembly with no InternalsVisibleTo —
    /// same constraint <c>SaveableRegistryTests</c> already works around.
    /// </summary>
    public class DoorItemLockTests
    {
        private static InventoryItemDefinition MakeItem(string id, string displayName)
        {
            var item = ScriptableObject.CreateInstance<InventoryItemDefinition>();
            item.ItemId = id;
            item.DisplayName = displayName;
            return item;
        }

        private static (GameObject doorGo, Door door) MakeLockedDoor(InventoryItemDefinition requiredItem)
        {
            var doorGo = new GameObject("TestDoor");
            var door = doorGo.AddComponent<Door>();
            door.SetRequiredItem(requiredItem);
            door.RestoreState(new Door.DoorState(isOpen: false, isLocked: true));
            return (doorGo, door);
        }

        [Test]
        public void Interact_LockedWithRequiredItemInInventory_UnlocksAndConsumesItem()
        {
            InventoryItemDefinition item = MakeItem("key_brass", "Brass Key");
            (GameObject doorGo, Door door) = MakeLockedDoor(item);

            var playerGo = new GameObject("TestPlayer");
            var inventory = playerGo.AddComponent<Inventory>();
            inventory.TryAddItem(item);

            door.Interact(new InteractionContext(playerGo));

            Assert.IsFalse(door.IsLocked);
            Assert.IsFalse(inventory.HasItem("key_brass"));

            Object.DestroyImmediate(doorGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void Interact_LockedWithoutRequiredItemInInventory_StaysLocked()
        {
            InventoryItemDefinition item = MakeItem("key_brass", "Brass Key");
            (GameObject doorGo, Door door) = MakeLockedDoor(item);

            var playerGo = new GameObject("TestPlayer");
            playerGo.AddComponent<Inventory>();

            door.Interact(new InteractionContext(playerGo));

            Assert.IsTrue(door.IsLocked);

            Object.DestroyImmediate(doorGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void Interact_LockedWithNoInventoryOnInstigator_StaysLockedWithoutThrowing()
        {
            InventoryItemDefinition item = MakeItem("key_brass", "Brass Key");
            (GameObject doorGo, Door door) = MakeLockedDoor(item);

            var playerGo = new GameObject("TestPlayer");

            Assert.DoesNotThrow(() => door.Interact(new InteractionContext(playerGo)));
            Assert.IsTrue(door.IsLocked);

            Object.DestroyImmediate(doorGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void GetInteractionPrompt_LockedWithRequiredItem_MentionsTheItemName()
        {
            InventoryItemDefinition item = MakeItem("key_brass", "Brass Key");
            (GameObject doorGo, Door door) = MakeLockedDoor(item);

            StringAssert.Contains("Brass Key", door.GetInteractionPrompt());

            Object.DestroyImmediate(doorGo);
        }

        [Test]
        public void Interact_LockedWithNoRequiredItemConfigured_StaysLocked()
        {
            (GameObject doorGo, Door door) = MakeLockedDoor(null);

            var playerGo = new GameObject("TestPlayer");
            playerGo.AddComponent<Inventory>();

            door.Interact(new InteractionContext(playerGo));

            Assert.IsTrue(door.IsLocked);

            Object.DestroyImmediate(doorGo);
            Object.DestroyImmediate(playerGo);
        }
    }
}
