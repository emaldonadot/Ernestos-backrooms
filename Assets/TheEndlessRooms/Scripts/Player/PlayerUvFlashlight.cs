using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.Player
{
    /// <summary>
    /// Same toggle pattern as <see cref="PlayerFlashlight"/>, but additionally requires
    /// <see cref="_batteryItem"/> in the player's <see cref="Inventory"/> to turn on —
    /// the "combine the UV flashlight with batteries" requirement, implemented as a
    /// simple ownership check rather than a dedicated combine UI/interaction (both
    /// items just need to be carried at the same time). Also drives
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
            if (_uvFlashlightItem == null || itemId != _uvFlashlightItem.ItemId)
            {
                return;
            }

            if (_inventory == null || !_inventory.HasItem(_uvFlashlightItem.ItemId))
            {
                return;
            }

            bool turningOn = _beam == null || !_beam.enabled;
            if (turningOn && (_batteryItem == null || !_inventory.HasItem(_batteryItem.ItemId)))
            {
                Debug.Log("The UV flashlight needs batteries.");
                return;
            }

            SetOn(turningOn);
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
