using EndlessRooms.Core;
using EndlessRooms.World;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    /// <summary>
    /// Covers <see cref="LockableDrawer"/>'s item-lock (Milestone 9's progression
    /// redesign) — same shape as <see cref="DoorItemLockTests"/>, but also checks the
    /// non-consuming case (an ID Card has to keep working after unlocking a drawer),
    /// which Door's default-consuming lock never needed to prove.
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

        private static (GameObject drawerGo, LockableDrawer drawer, GameObject content) MakeLockedDrawer(InventoryItemDefinition requiredItem, bool consume)
        {
            var drawerGo = new GameObject("TestDrawer");
            var drawer = drawerGo.AddComponent<LockableDrawer>();
            var content = new GameObject("Content");
            content.transform.SetParent(drawerGo.transform);

            drawer.Configure(requiredItem, consume, content);

            return (drawerGo, drawer, content);
        }

        [Test]
        public void Interact_WithRequiredItemConsumed_UnlocksConsumesItemAndRevealsContent()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content) = MakeLockedDrawer(item, consume: true);

            var playerGo = new GameObject("TestPlayer");
            var inventory = playerGo.AddComponent<Inventory>();
            inventory.TryAddItem(item);

            drawer.Interact(new InteractionContext(playerGo));

            Assert.IsTrue(drawer.IsUnlocked);
            Assert.IsFalse(inventory.HasItem("bronze_key"));
            Assert.IsTrue(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void Interact_WithRequiredItemNotConsumed_UnlocksAndKeepsItem()
        {
            InventoryItemDefinition item = MakeItem("id_card", "Office ID Card");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content) = MakeLockedDrawer(item, consume: false);

            var playerGo = new GameObject("TestPlayer");
            var inventory = playerGo.AddComponent<Inventory>();
            inventory.TryAddItem(item);

            drawer.Interact(new InteractionContext(playerGo));

            Assert.IsTrue(drawer.IsUnlocked);
            Assert.IsTrue(inventory.HasItem("id_card"));
            Assert.IsTrue(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void Interact_WithoutRequiredItem_StaysLockedAndContentStaysHidden()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, GameObject content) = MakeLockedDrawer(item, consume: true);

            var playerGo = new GameObject("TestPlayer");
            playerGo.AddComponent<Inventory>();

            drawer.Interact(new InteractionContext(playerGo));

            Assert.IsFalse(drawer.IsUnlocked);
            Assert.IsFalse(content.activeSelf);

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void GetInteractionPrompt_LockedWithRequiredItem_MentionsTheItemName()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, _) = MakeLockedDrawer(item, consume: true);

            StringAssert.Contains("Bronze Key", drawer.GetInteractionPrompt());

            Object.DestroyImmediate(drawerGo);
        }

        [Test]
        public void CanInteract_OnceUnlocked_ReturnsFalse()
        {
            InventoryItemDefinition item = MakeItem("bronze_key", "Bronze Key");
            (GameObject drawerGo, LockableDrawer drawer, _) = MakeLockedDrawer(item, consume: true);

            var playerGo = new GameObject("TestPlayer");
            var inventory = playerGo.AddComponent<Inventory>();
            inventory.TryAddItem(item);

            drawer.Interact(new InteractionContext(playerGo));

            Assert.IsFalse(drawer.CanInteract(new InteractionContext(playerGo)));

            Object.DestroyImmediate(drawerGo);
            Object.DestroyImmediate(playerGo);
        }
    }
}
