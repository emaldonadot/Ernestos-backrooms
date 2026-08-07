using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Minimal pickup stub used only to validate the interaction + save-state wiring
    /// in the Milestone 1 test scene. Replace with a real inventory-integrated item
    /// once the inventory framework exists.
    /// </summary>
    public sealed class PickupTestItem : MonoBehaviour, IInteractable, ISaveable
    {
        [SerializeField] private string _itemName = "Test Item";
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
            return $"Pick up {_itemName}";
        }

        public bool CanInteract(InteractionContext context)
        {
            return !_isCollected;
        }

        public void Interact(InteractionContext context)
        {
            _isCollected = true;
            Debug.Log($"Picked up '{_itemName}' (instigator: {context.Instigator?.name}).", this);
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
