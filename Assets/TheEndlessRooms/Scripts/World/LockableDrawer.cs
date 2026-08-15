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
    /// Optionally slides <see cref="_slidingPart"/> open along its own local +Z (matching
    /// <c>Level1FurnitureBuilder.BuildDeskDrawerTray</c>'s convention) so the unlock is
    /// visible, not just a state flag — <see cref="_revealedContent"/> is expected to be
    /// a child of that same sliding part so the item rides out with the tray.
    /// </summary>
    public sealed class LockableDrawer : MonoBehaviour, IInteractable, IProgressionGate, ISaveable
    {
        [SerializeField] private InventoryItemDefinition _requiredItem;
        [SerializeField] private bool _consumeRequiredItem = false;
        [Tooltip("Activated once the drawer unlocks — typically the InventoryPickup (or FieldNote) sitting inside it. Leave blank for a drawer that's locked but empty once opened.")]
        [SerializeField] private GameObject _revealedContent;
        [Tooltip("Optional — the drawer tray transform to slide open on unlock (its own local +Z is the open direction). Leave blank for a drawer with no open/close animation.")]
        [SerializeField] private Transform _slidingPart;
        [SerializeField] private Vector3 _openLocalOffset = new(0f, 0f, 0.22f);
        [SerializeField] private float _slideSpeed = 0.5f;
        [Tooltip("Leave blank to use the GameObject name as the save identifier.")]
        [SerializeField] private string _saveId = "";

        private Vector3 _closedLocalPosition;

        public bool IsUnlocked { get; private set; }

        public event Action Changed;

        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;

        /// <summary>Placement-time / test-time wiring, mirroring <see cref="Door.SetRequiredItem"/>.</summary>
        public void Configure(InventoryItemDefinition requiredItem, bool consumeRequiredItem, GameObject revealedContent, Transform slidingPart = null, Vector3? openLocalOffset = null)
        {
            _requiredItem = requiredItem;
            _consumeRequiredItem = consumeRequiredItem;
            _revealedContent = revealedContent;
            if (_revealedContent != null)
            {
                _revealedContent.SetActive(false);
            }

            _slidingPart = slidingPart;
            if (openLocalOffset.HasValue)
            {
                _openLocalOffset = openLocalOffset.Value;
            }

            _closedLocalPosition = _slidingPart != null ? _slidingPart.localPosition : Vector3.zero;
        }

        private void Awake()
        {
            if (_revealedContent != null)
            {
                _revealedContent.SetActive(false);
            }

            if (_slidingPart != null && _closedLocalPosition == Vector3.zero)
            {
                _closedLocalPosition = _slidingPart.localPosition;
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

        private void Update()
        {
            if (_slidingPart == null)
            {
                return;
            }

            Vector3 target = _closedLocalPosition + (IsUnlocked ? _openLocalOffset : Vector3.zero);
            _slidingPart.localPosition = Vector3.MoveTowards(_slidingPart.localPosition, target, _slideSpeed * Time.deltaTime);
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
