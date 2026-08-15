using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// A real, inventory-integrated pickup — the item <see cref="PickupTestItem"/>'s own
    /// doc comment anticipated ("replace with a real inventory-integrated item once the
    /// inventory framework exists"). Left <see cref="PickupTestItem"/> itself alone since
    /// Milestones 1/5/7's scene builders still reference it; this is additive, not a
    /// replacement. Adds <see cref="_item"/> to the instigator's <see cref="Inventory"/>
    /// on interact rather than just deactivating.
    /// </summary>
    public sealed class InventoryPickup : MonoBehaviour, IInteractable, ISaveable
    {
        [SerializeField] private InventoryItemDefinition _item;
        [Tooltip("Leave blank to use the GameObject name as the save identifier.")]
        [SerializeField] private string _saveId = "";

        private bool _isCollected;

        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;

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

        public string GetInteractionPrompt()
        {
            return _item != null ? $"Take {_item.DisplayName}" : "Take Item";
        }

        public bool CanInteract(InteractionContext context)
        {
            return !_isCollected && _item != null;
        }

        public void Interact(InteractionContext context)
        {
            if (_item == null || context.Instigator == null)
            {
                return;
            }

            var inventory = context.Instigator.GetComponentInParent<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning($"'{name}' interacted with an instigator that has no {nameof(Inventory)}.", this);
                return;
            }

            if (!inventory.TryAddItem(_item))
            {
                Debug.Log($"Inventory full — can't pick up '{_item.DisplayName}'.", this);
                return;
            }

            _isCollected = true;
            gameObject.SetActive(false);
        }

        public object CaptureState()
        {
            return new PickupState(_isCollected);
        }

        public void RestoreState(object state)
        {
            if (state is not PickupState pickupState)
            {
                return;
            }

            _isCollected = pickupState.IsCollected;
            gameObject.SetActive(!_isCollected);
        }

        [Serializable]
        public struct PickupState
        {
            public bool IsCollected;

            public PickupState(bool isCollected)
            {
                IsCollected = isCollected;
            }
        }
    }
}
