using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// The player's carried-items list (Milestone 9: up to <see cref="InventoryState.MaxItems"/>
    /// items — keys, tools). Thin MonoBehaviour/<see cref="ISaveable"/> wrapper around the
    /// pure <see cref="InventoryState"/>, same split as <c>AttendantController</c>/
    /// <c>AttendantPerception</c>.
    /// </summary>
    public sealed class Inventory : MonoBehaviour, ISaveable
    {
        [Tooltip("Leave blank to use the GameObject name as the save identifier.")]
        [SerializeField] private string _saveId = "";

        [Tooltip("Every item that could exist in this level — used to resolve saved item IDs back to their definitions when a save is loaded.")]
        [SerializeField] private InventoryItemDefinition[] _itemCatalog = Array.Empty<InventoryItemDefinition>();

        private readonly InventoryState _state = new();

        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;
        public IReadOnlyList<InventoryItemDefinition> Items => _state.Items;

        /// <summary>Raised whenever an item is added or removed, so a HUD can refresh without polling.</summary>
        public event Action Changed;

        private void OnEnable()
        {
            if (GameServices.TryGet<SaveableRegistry>(out var registry))
            {
                registry.Register(this);
            }
        }

        private void OnDisable()
        {
            if (GameServices.TryGet<SaveableRegistry>(out var registry))
            {
                registry.Unregister(this);
            }
        }

        public bool HasItem(string itemId) => _state.HasItem(itemId);

        public bool TryAddItem(InventoryItemDefinition item)
        {
            bool added = _state.TryAddItem(item);
            if (added)
            {
                Changed?.Invoke();
            }

            return added;
        }

        public bool TryRemoveItem(string itemId)
        {
            bool removed = _state.TryRemoveItem(itemId);
            if (removed)
            {
                Changed?.Invoke();
            }

            return removed;
        }

        public object CaptureState()
        {
            return new InventorySaveState(_state.Items.Select(item => item.ItemId).ToArray());
        }

        public void RestoreState(object state)
        {
            if (state is not InventorySaveState saveState)
            {
                return;
            }

            _state.Clear();
            foreach (string itemId in saveState.ItemIds)
            {
                InventoryItemDefinition definition = _itemCatalog.FirstOrDefault(item => item != null && item.ItemId == itemId);
                if (definition != null)
                {
                    _state.TryAddItem(definition);
                }
            }

            Changed?.Invoke();
        }

        [Serializable]
        public struct InventorySaveState
        {
            public string[] ItemIds;

            public InventorySaveState(string[] itemIds)
            {
                ItemIds = itemIds;
            }
        }
    }
}
