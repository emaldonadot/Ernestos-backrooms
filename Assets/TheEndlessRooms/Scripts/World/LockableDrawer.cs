using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// A desk/cabinet drawer. With no <see cref="_requiredItem"/> configured it just
    /// opens on interact (every desk gets one of these so every drawer in the level is
    /// genuinely searchable). With one configured, interacting never unlocks it
    /// directly — it only ever reports whether a key is needed: pressing Interact
    /// always just gives feedback (a generic "needs a key" if the player doesn't have
    /// it yet, or the specific item name once they do), and the real unlock happens by
    /// selecting that item in the inventory and pressing UseItem while this drawer is
    /// the focused interactable. That two-step matches how the UV Flashlight/Battery
    /// and Cassette/Recorder combos already work — a key is "used on" a lock, not
    /// auto-consumed just by being carried near it.
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
        [Tooltip("Needed to check/consume the required item without an InteractionContext, since UseItem-while-focused doesn't carry an instigator the way Interact does.")]
        [SerializeField] private Inventory _inventory;
        [Tooltip("Optional — the drawer tray transform to slide open on unlock (its own local +Z is the open direction). Leave blank for a drawer with no open/close animation.")]
        [SerializeField] private Transform _slidingPart;
        [SerializeField] private Vector3 _openLocalOffset = new(0f, 0f, 0.22f);
        [SerializeField] private float _slideSpeed = 0.5f;
        [Tooltip("Leave blank to use the GameObject name as the save identifier.")]
        [SerializeField] private string _saveId = "";

        private Vector3 _closedLocalPosition;
        private bool _isFocused;

        public bool IsUnlocked { get; private set; }

        public event Action Changed;

        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;

        /// <summary>Placement-time / test-time wiring, mirroring <see cref="Door.SetRequiredItem"/>.</summary>
        public void Configure(InventoryItemDefinition requiredItem, bool consumeRequiredItem, GameObject revealedContent, Inventory inventory, Transform slidingPart = null, Vector3? openLocalOffset = null)
        {
            _requiredItem = requiredItem;
            _consumeRequiredItem = consumeRequiredItem;
            _revealedContent = revealedContent;
            if (_revealedContent != null)
            {
                _revealedContent.SetActive(false);
            }

            _inventory = inventory;
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
            GameEvents.InteractableFocusChanged += HandleFocusChanged;
            GameEvents.ItemUseRequested += HandleItemUseRequested;

            if (GameServices.TryGet<SaveableRegistry>(out var registry))
            {
                registry.Register(this);
            }
        }

        private void OnDisable()
        {
            GameEvents.InteractableFocusChanged -= HandleFocusChanged;
            GameEvents.ItemUseRequested -= HandleItemUseRequested;

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

            if (_requiredItem == null)
            {
                return "Open Drawer";
            }

            bool hasItem = _inventory != null && _inventory.HasItem(_requiredItem.ItemId);
            return hasItem ? $"Use {_requiredItem.DisplayName} To Open" : "Locked Drawer";
        }

        public bool CanInteract(InteractionContext context)
        {
            return !IsUnlocked;
        }

        /// <summary>A free (no key needed) drawer opens immediately; a locked one only ever reports what's needed here — see the class doc comment for why unlocking itself goes through UseItem instead.</summary>
        public void Interact(InteractionContext context)
        {
            if (IsUnlocked)
            {
                return;
            }

            if (_requiredItem == null)
            {
                Unlock();
                return;
            }

            var inventory = context.Instigator != null ? context.Instigator.GetComponentInParent<Inventory>() : _inventory;
            bool hasItem = inventory != null && inventory.HasItem(_requiredItem.ItemId);
            Debug.Log(hasItem
                ? $"'{name}' is locked — select the {_requiredItem.DisplayName} and use it to open this."
                : $"'{name}' is locked. You need a key.", this);
        }

        private void HandleFocusChanged(IInteractable focused)
        {
            _isFocused = ReferenceEquals(focused, this);
        }

        private void HandleItemUseRequested(string itemId)
        {
            TryUnlockWithUsedItem(itemId);
        }

        /// <summary>The actual unlock path — public so it's directly testable without needing GameEvents.ItemUseRequested subscriptions to have fired (OnEnable never runs in headless Editor scripting or EditMode tests).</summary>
        public bool TryUnlockWithUsedItem(string itemId)
        {
            if (IsUnlocked || !_isFocused || _requiredItem == null || itemId != _requiredItem.ItemId)
            {
                return false;
            }

            if (_inventory == null || !_inventory.HasItem(_requiredItem.ItemId))
            {
                return false;
            }

            if (_consumeRequiredItem)
            {
                _inventory.TryRemoveItem(_requiredItem.ItemId);
            }

            Unlock();
            return true;
        }

        /// <summary>Test/placement-time hook for focus, mirroring how InteractionCaster tracks it in real play.</summary>
        public void SetFocused(bool isFocused)
        {
            _isFocused = isFocused;
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
