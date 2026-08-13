using EndlessRooms.Core;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class InventoryStateTests
    {
        private static InventoryItemDefinition MakeItem(string id)
        {
            var item = ScriptableObject.CreateInstance<InventoryItemDefinition>();
            item.ItemId = id;
            item.DisplayName = id;
            return item;
        }

        [Test]
        public void TryAddItem_BelowCap_Succeeds()
        {
            var state = new InventoryState();
            bool added = state.TryAddItem(MakeItem("key_brass"));

            Assert.IsTrue(added);
            Assert.AreEqual(1, state.Items.Count);
        }

        [Test]
        public void TryAddItem_AtCap_Fails()
        {
            var state = new InventoryState();
            for (int i = 0; i < InventoryState.MaxItems; i++)
            {
                Assert.IsTrue(state.TryAddItem(MakeItem($"item_{i}")));
            }

            bool added = state.TryAddItem(MakeItem("one_too_many"));

            Assert.IsFalse(added);
            Assert.AreEqual(InventoryState.MaxItems, state.Items.Count);
        }

        [Test]
        public void TryAddItem_Null_Fails()
        {
            var state = new InventoryState();
            bool added = state.TryAddItem(null);

            Assert.IsFalse(added);
            Assert.AreEqual(0, state.Items.Count);
        }

        [Test]
        public void HasItem_AfterAdding_ReturnsTrueForThatIdOnly()
        {
            var state = new InventoryState();
            state.TryAddItem(MakeItem("key_brass"));

            Assert.IsTrue(state.HasItem("key_brass"));
            Assert.IsFalse(state.HasItem("chain_cutter"));
        }

        [Test]
        public void TryRemoveItem_Present_RemovesAndReturnsTrue()
        {
            var state = new InventoryState();
            state.TryAddItem(MakeItem("key_brass"));

            bool removed = state.TryRemoveItem("key_brass");

            Assert.IsTrue(removed);
            Assert.IsFalse(state.HasItem("key_brass"));
            Assert.AreEqual(0, state.Items.Count);
        }

        [Test]
        public void TryRemoveItem_NotPresent_ReturnsFalse()
        {
            var state = new InventoryState();
            bool removed = state.TryRemoveItem("nonexistent");

            Assert.IsFalse(removed);
        }

        [Test]
        public void TryRemoveItem_AfterRemoving_FreesCapForAnotherAdd()
        {
            var state = new InventoryState();
            for (int i = 0; i < InventoryState.MaxItems; i++)
            {
                state.TryAddItem(MakeItem($"item_{i}"));
            }

            state.TryRemoveItem("item_0");
            bool added = state.TryAddItem(MakeItem("new_item"));

            Assert.IsTrue(added);
            Assert.AreEqual(InventoryState.MaxItems, state.Items.Count);
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            var state = new InventoryState();
            state.TryAddItem(MakeItem("key_brass"));
            state.TryAddItem(MakeItem("chain_cutter"));

            state.Clear();

            Assert.AreEqual(0, state.Items.Count);
        }
    }
}
