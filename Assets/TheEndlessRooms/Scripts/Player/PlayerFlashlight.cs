using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.Player
{
    /// <summary>
    /// A real, toggleable light source — reacts to <see cref="GameEvents.ItemUseRequested"/>
    /// carrying its own <see cref="_flashlightItem"/>'s ItemId, so it only turns on/off
    /// when the flashlight is actually the selected inventory slot when Use is pressed.
    /// Requires the item to be in the player's <see cref="Inventory"/> at all — found on
    /// the ground before pickup does nothing.
    /// </summary>
    public sealed class PlayerFlashlight : MonoBehaviour
    {
        [SerializeField] private InventoryItemDefinition _flashlightItem;
        [SerializeField] private Inventory _inventory;
        [SerializeField] private Light _beam;

        private void OnEnable()
        {
            GameEvents.ItemUseRequested += HandleItemUseRequested;
            SetOn(false);
        }

        private void OnDisable()
        {
            GameEvents.ItemUseRequested -= HandleItemUseRequested;
        }

        private void HandleItemUseRequested(string itemId)
        {
            if (_flashlightItem == null || itemId != _flashlightItem.ItemId)
            {
                return;
            }

            if (_inventory != null && !_inventory.HasItem(_flashlightItem.ItemId))
            {
                return;
            }

            SetOn(_beam == null || !_beam.enabled);
        }

        private void SetOn(bool on)
        {
            if (_beam != null)
            {
                _beam.enabled = on;
            }
        }
    }
}
