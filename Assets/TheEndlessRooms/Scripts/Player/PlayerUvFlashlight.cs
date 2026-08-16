using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.Player
{
    /// <summary>
    /// Same toggle pattern as <see cref="PlayerFlashlight"/>, but starts dead — using the
    /// Battery item while carrying this one "combines" them into a persistent
    /// <see cref="_isPowered"/> flag (a one-way transformation, not a per-toggle
    /// ownership check: once combined, the flashlight stays usable even if the battery
    /// item itself were ever removed). Also drives
    /// <see cref="GameEvents.UvLightToggled"/> so world props (a hidden bathroom clue)
    /// can react to whether UV light is currently on, without referencing this
    /// component directly.
    /// </summary>
    public sealed class PlayerUvFlashlight : MonoBehaviour
    {
        [SerializeField] private InventoryItemDefinition _uvFlashlightItem;
        [SerializeField] private InventoryItemDefinition _batteryItem;
        [SerializeField] private Inventory _inventory;
        [SerializeField] private Light _beam;

        private bool _isPowered;

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
            if (_batteryItem != null && itemId == _batteryItem.ItemId)
            {
                TryCombineBattery();
                return;
            }

            if (_uvFlashlightItem == null || itemId != _uvFlashlightItem.ItemId)
            {
                return;
            }

            if (_inventory == null || !_inventory.HasItem(_uvFlashlightItem.ItemId))
            {
                return;
            }

            if (!_isPowered)
            {
                Debug.Log("The UV flashlight needs batteries.");
                return;
            }

            bool turningOn = _beam == null || !_beam.enabled;
            SetOn(turningOn);
        }

        private void TryCombineBattery()
        {
            if (_isPowered || _inventory == null || _uvFlashlightItem == null || _batteryItem == null)
            {
                return;
            }

            if (!_inventory.HasItem(_uvFlashlightItem.ItemId) || !_inventory.HasItem(_batteryItem.ItemId))
            {
                return;
            }

            _isPowered = true;
            Debug.Log("The UV flashlight now has power.");
            GameEvents.RaiseUvFlashlightPowered();
        }

        private void SetOn(bool on)
        {
            if (_beam != null)
            {
                _beam.enabled = on;
            }

            GameEvents.RaiseUvLightToggled(on);
        }
    }
}
