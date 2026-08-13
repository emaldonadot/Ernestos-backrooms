using System.Collections.Generic;
using System.Linq;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Pure add/remove/cap logic for a carried-items list — no <see cref="UnityEngine.MonoBehaviour"/>
    /// dependency, so it's EditMode-testable directly, the same way <c>AttendantPerception</c>/
    /// <c>AttendantStateMachine</c> separate pure logic from their MonoBehaviour wrapper
    /// (<see cref="Inventory"/>).
    /// </summary>
    public sealed class InventoryState
    {
        public const int MaxItems = 10;

        private readonly List<InventoryItemDefinition> _items = new();

        public IReadOnlyList<InventoryItemDefinition> Items => _items;

        public bool HasItem(string itemId)
        {
            return _items.Any(item => item.ItemId == itemId);
        }

        public bool TryAddItem(InventoryItemDefinition item)
        {
            if (item == null || _items.Count >= MaxItems)
            {
                return false;
            }

            _items.Add(item);
            return true;
        }

        public bool TryRemoveItem(string itemId)
        {
            int index = _items.FindIndex(item => item.ItemId == itemId);
            if (index < 0)
            {
                return false;
            }

            _items.RemoveAt(index);
            return true;
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
