using System;
using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessRooms.Player
{
    /// <summary>
    /// Lets the player cycle which carried item is "selected" and trigger its use —
    /// pure input-to-event plumbing. Item-specific behavior (turning a light on, playing
    /// a message) lives entirely on whichever component reacts to
    /// <see cref="GameEvents.ItemUseRequested"/>, not here.
    /// </summary>
    public sealed class InventorySelectionController : MonoBehaviour
    {
        [SerializeField] private Inventory _inventory;
        [SerializeField] private InputActionReference _cycleNextAction;
        [SerializeField] private InputActionReference _cyclePreviousAction;
        [SerializeField] private InputActionReference _useItemAction;

        private int _selectedIndex;

        public InventoryItemDefinition SelectedItem
        {
            get
            {
                if (_inventory == null || _inventory.Items.Count == 0)
                {
                    return null;
                }

                int clampedIndex = Mathf.Clamp(_selectedIndex, 0, _inventory.Items.Count - 1);
                return _inventory.Items[clampedIndex];
            }
        }

        public int SelectedIndex => _selectedIndex;

        /// <summary>Raised whenever the selected slot changes (cycling, or the item list itself shrinking/growing) so a HUD can refresh without polling.</summary>
        public event Action SelectionChanged;

        private void OnEnable()
        {
            if (_cycleNextAction != null)
            {
                _cycleNextAction.action.Enable();
                _cycleNextAction.action.performed += HandleCycleNext;
            }

            if (_cyclePreviousAction != null)
            {
                _cyclePreviousAction.action.Enable();
                _cyclePreviousAction.action.performed += HandleCyclePrevious;
            }

            if (_useItemAction != null)
            {
                _useItemAction.action.Enable();
                _useItemAction.action.performed += HandleUseItem;
            }

            if (_inventory != null)
            {
                _inventory.Changed += HandleInventoryChanged;
            }
        }

        private void OnDisable()
        {
            if (_cycleNextAction != null)
            {
                _cycleNextAction.action.performed -= HandleCycleNext;
                _cycleNextAction.action.Disable();
            }

            if (_cyclePreviousAction != null)
            {
                _cyclePreviousAction.action.performed -= HandleCyclePrevious;
                _cyclePreviousAction.action.Disable();
            }

            if (_useItemAction != null)
            {
                _useItemAction.action.performed -= HandleUseItem;
                _useItemAction.action.Disable();
            }

            if (_inventory != null)
            {
                _inventory.Changed -= HandleInventoryChanged;
            }
        }

        private void HandleCycleNext(InputAction.CallbackContext context) => Cycle(1);

        private void HandleCyclePrevious(InputAction.CallbackContext context) => Cycle(-1);

        private void Cycle(int direction)
        {
            if (_inventory == null || _inventory.Items.Count == 0)
            {
                return;
            }

            int count = _inventory.Items.Count;
            _selectedIndex = ((_selectedIndex + direction) % count + count) % count;
            SelectionChanged?.Invoke();
            GameEvents.RaiseSelectedItemChanged(SelectedItem != null ? SelectedItem.ItemId : string.Empty);
        }

        private void HandleInventoryChanged()
        {
            _selectedIndex = _inventory.Items.Count > 0 ? Mathf.Clamp(_selectedIndex, 0, _inventory.Items.Count - 1) : 0;
            SelectionChanged?.Invoke();
            GameEvents.RaiseSelectedItemChanged(SelectedItem != null ? SelectedItem.ItemId : string.Empty);
        }

        private void HandleUseItem(InputAction.CallbackContext context)
        {
            InventoryItemDefinition selected = SelectedItem;
            if (selected != null)
            {
                GameEvents.RaiseItemUseRequested(selected.ItemId);
            }
        }
    }
}
