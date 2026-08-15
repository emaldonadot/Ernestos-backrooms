using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// A desk/cabinet drawer that's locked until the instigator's <see cref="Inventory"/>
    /// holds <see cref="_requiredItem"/> — the same item-lock idea <see cref="Door"/> uses
    /// for its single "hero" key, generalized into its own component (rather than reusing
    /// Door) because a drawer isn't a room boundary the Attendant should path/investigate
    /// through, and because <see cref="_consumeRequiredItem"/> defaults to false here: an
    /// ID Card is meant to reopen every lock it fits, not vanish after the first one.
    /// </summary>
    public sealed class LockableDrawer : MonoBehaviour, IInteractable, IProgressionGate, ISaveable
    {
        [SerializeField] private InventoryItemDefinition _requiredItem;
        [SerializeField] private bool _consumeRequiredItem = false;
        [Tooltip("Activated once the drawer unlocks — typically the InventoryPickup (or FieldNote) sitting inside it. Leave blank for a drawer that's locked but empty once opened.")]
        [SerializeField] private GameObject _revealedContent;
        [Tooltip("Leave blank to use the GameObject name as the save identifier.")]
        [SerializeField] private string _saveId = "";

        public bool IsUnlocked { get; private set; }

        public event Action Changed;

        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;

        /// <summary>Placement-time / test-time wiring, mirroring <see cref="Door.SetRequiredItem"/>.</summary>
        public void Configure(InventoryItemDefinition requiredItem, bool consumeRequiredItem, GameObject revealedContent)
        {
            _requiredItem = requiredItem;
            _consumeRequiredItem = consumeRequiredItem;
            _revealedContent = revealedContent;
            if (_revealedContent != null)
            {
                _revealedContent.SetActive(false);
            }
        }

        private void Awake()
        {
            if (_revealedContent != null)
            {
                _revealedContent.SetActive(false);
            }
        }

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
            if (IsUnlocked)
            {
                return "Search Drawer";
            }

            return _requiredItem != null ? $"Locked (Needs {_requiredItem.DisplayName})" : "Locked Drawer";
        }

        public bool CanInteract(InteractionContext context)
        {
            return !IsUnlocked;
        }

        public void Interact(InteractionContext context)
        {
            if (IsUnlocked || _requiredItem == null || context.Instigator == null)
            {
                return;
            }

            var inventory = context.Instigator.GetComponentInParent<Inventory>();
            if (inventory == null || !inventory.HasItem(_requiredItem.ItemId))
            {
                Debug.Log($"'{name}' is locked — needs {(_requiredItem != null ? _requiredItem.DisplayName : "something")}.", this);
                return;
            }

            if (_consumeRequiredItem)
            {
                inventory.TryRemoveItem(_requiredItem.ItemId);
            }

            Unlock();
        }

        private void Unlock()
        {
            IsUnlocked = true;
            if (_revealedContent != null)
            {
                _revealedContent.SetActive(true);
            }

            Changed?.Invoke();
        }

        public object CaptureState()
        {
            return new DrawerState(IsUnlocked);
        }

        public void RestoreState(object state)
        {
            if (state is not DrawerState drawerState || !drawerState.IsUnlocked)
            {
                return;
            }

            Unlock();
        }

        [Serializable]
        public struct DrawerState
        {
            public bool IsUnlocked;

            public DrawerState(bool isUnlocked)
            {
                IsUnlocked = isUnlocked;
            }
        }
    }
}
