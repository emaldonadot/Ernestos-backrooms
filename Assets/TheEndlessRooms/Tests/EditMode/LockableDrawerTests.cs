using EndlessRooms.Core;
using EndlessRooms.World;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    /// <summary>
    /// Covers <see cref="LockableDrawer"/>'s two-step item-lock (interact only ever
    /// gives feedback; unlocking happens by selecting the required item and using it
    /// while this drawer is focused) plus the free/no-key-needed drawer every desk gets.
    /// </summary>
    public class LockableDrawerTests
    {
        private static InventoryItemDefinition MakeItem(string id, string displayName)
        {
            var item = ScriptableObject.CreateInstance<InventoryItemDefinition>();
            item.ItemId = id;
            item.DisplayName = displayName;
            return item;
        }

        private static (GameObject drawerGo, LockableDrawer drawer, GameObject content, Inventory inventory, GameObject playerGo) MakeLockedDrawer(InventoryItemDefinition requiredItem, bool consume)
        {
            var drawerGo = new GameObject("TestDrawer");
            var drawer = drawerGo.AddComponent<LockableDrawer>();
            var content = new GameObject("Content");
            content.transform.SetParent(drawerGo.transform);

            var playerGo = new GameObject("TestPlayer");
            var inventory = playerGo.AddComponent<Inventory>();

            drawer.Configure(requiredItem, consume, content, inventory);

            return (drawerGo, drawer, content, inventory, playerGo);
        }

        [Test]
        public void Interact_LockedWithoutRequiredItem_StaysLockedAndDoesNotThrow()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content, _, GameObject playerGo) = MakeLockedDrawer(item, consume: true);

            Assert.DoesNotThrow(() => drawer.Interact(new InteractionContext(playerGo)));
            Assert.IsFalse(drawer.IsUnlocked);
            Assert.IsFalse(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void Interact_LockedWithRequiredItemInInventory_StillDoesNotUnlock()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content, Inventory inventory, GameObject playerGo) = MakeLockedDrawer(item, consume: true);
            inventory.TryAddItem(item);

            drawer.Interact(new InteractionContext(playerGo));

            Assert.IsFalse(drawer.IsUnlocked, "Carrying the key should not be enough — it has to be used, not just held.");
            Assert.IsFalse(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void TryUnlockWithUsedItem_FocusedWithRequiredItemConsumed_UnlocksConsumesItemAndRevealsContent()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content, Inventory inventory, GameObject playerGo) = MakeLockedDrawer(item, consume: true);
            inventory.TryAddItem(item);
            drawer.SetFocused(true);

            bool unlocked = drawer.TryUnlockWithUsedItem("bronze_key");

            Assert.IsTrue(unlocked);
            Assert.IsTrue(drawer.IsUnlocked);
            Assert.IsFalse(inventory.HasItem("bronze_key"));
            Assert.IsTrue(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void TryUnlockWithUsedItem_FocusedWithRequiredItemNotConsumed_UnlocksAndKeepsItem()
        {
            InventoryItemDefinition item = MakeItem("id_card", "Office ID Card");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content, Inventory inventory, GameObject playerGo) = MakeLockedDrawer(item, consume: false);
            inventory.TryAddItem(item);
            drawer.SetFocused(true);

            bool unlocked = drawer.TryUnlockWithUsedItem("id_card");

            Assert.IsTrue(unlocked);
            Assert.IsTrue(drawer.IsUnlocked);
            Assert.IsTrue(inventory.HasItem("id_card"));
            Assert.IsTrue(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void TryUnlockWithUsedItem_NotFocused_StaysLocked()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content, Inventory inventory, GameObject playerGo) = MakeLockedDrawer(item, consume: true);
            inventory.TryAddItem(item);

            bool unlocked = drawer.TryUnlockWithUsedItem("bronze_key");

            Assert.IsFalse(unlocked, "Using the right key while looking at something else shouldn't unlock this drawer.");
            Assert.IsFalse(drawer.IsUnlocked);
            Assert.IsFalse(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void TryUnlockWithUsedItem_WrongItem_StaysLocked()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            var wrongItem = MakeItem("golden_key", "Golden Key");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content, Inventory inventory, GameObject playerGo) = MakeLockedDrawer(item, consume: true);
            inventory.TryAddItem(item);
            inventory.TryAddItem(wrongItem);
            drawer.SetFocused(true);

            bool unlocked = drawer.TryUnlockWithUsedItem("golden_key");

            Assert.IsFalse(unlocked);
            Assert.IsFalse(drawer.IsUnlocked);
            Assert.IsFalse(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void GetInteractionPrompt_LockedWithoutItem_IsGeneric()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, _, _, GameObject playerGo) = MakeLockedDrawer(item, consume: true);

            Assert.AreEqual("Locked Drawer", drawer.GetInteractionPrompt());

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void GetInteractionPrompt_LockedWithItemInInventory_NamesTheItem()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, _, Inventory inventory, GameObject playerGo) = MakeLockedDrawer(item, consume: true);
            inventory.TryAddItem(item);

            StringAssert.Contains("Bronze Key", drawer.GetInteractionPrompt());

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void Interact_NoRequiredItemConfigured_OpensImmediately()
        {
            var drawerGo = new GameObject("TestDrawer");
            var drawer = drawerGo.AddComponent<LockableDrawer>();
            var content = new GameObject("Content");
            content.transform.SetParent(drawerGo.transform);
            var playerGo = new GameObject("TestPlayer");
            var inventory = playerGo.AddComponent<Inventory>();
            drawer.Configure(null, false, content, inventory);

            drawer.Interact(new InteractionContext(playerGo));

            Assert.IsTrue(drawer.IsUnlocked);
            Assert.IsTrue(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void CanInteract_OnceUnlocked_ReturnsFalse()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, _, Inventory inventory, GameObject playerGo) = MakeLockedDrawer(item, consume: true);
            inventory.TryAddItem(item);
            drawer.SetFocused(true);
            drawer.TryUnlockWithUsedItem("bronze_key");

            Assert.IsFalse(drawer.CanInteract(new InteractionContext(playerGo)));

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }
    }
}
