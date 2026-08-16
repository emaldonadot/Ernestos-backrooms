using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Simple hinged door: the Milestone 1 reference implementation of
    /// <see cref="IInteractable"/> + <see cref="ISaveable"/>, and the first consumer of
    /// <see cref="IWorldCommand"/>/<see cref="WorldCommandExecutor"/>.
    /// </summary>
    public sealed class Door : MonoBehaviour, IInteractable, ISaveable
    {
        [SerializeField] private Transform _hinge;
        [SerializeField] private float _openAngle = 90f;
        [SerializeField] private float _openSpeed = 120f;
        [Tooltip("Leave blank to use the GameObject name as the save identifier.")]
        [SerializeField] private string _saveId = "";
        [Tooltip("Leave blank to use the default 'Open Door'/'Close Door' prompts. Set for a disguised door (e.g. a secret door dressed as a bookcase) that shouldn't announce itself as a door.")]
        [SerializeField] private string _customOpenPrompt = "";
        [SerializeField] private string _customClosePrompt = "";

        [Tooltip("Milestone 9: if set, interacting with this door while locked checks the instigator's Inventory for this specific item — if present, the door unlocks. Leave blank for locks that unlock some other way (e.g. PuzzleGateController).")]
        [SerializeField] private InventoryItemDefinition _requiredItem;

        [Tooltip("Whether unlocking removes _requiredItem from the Inventory. False for a reusable key that fits more than one lock in the level.")]
        [SerializeField] private bool _consumeRequiredItem = true;

        [Tooltip("Seconds an open door stays open before swinging shut on its own. 0 or less disables auto-close.")]
        [SerializeField] private float _autoCloseSeconds = 30f;

        private float _currentAngle;
        private float _autoCloseTimer;

        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }
        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;

        /// <summary>
        /// Raised when a live interaction opens/closes this door — not when
        /// <see cref="RestoreState"/> reapplies saved state on load, so loading a save
        /// doesn't make every door in the level look like it was just opened to any
        /// creature perception listening for this. Milestone 7's Attendant uses this for
        /// its "investigates recently opened doors" archetype.
        /// </summary>
        public event Action<Door> DoorToggled;

        private void Reset()
        {
            _hinge = transform;
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

        /// <summary>Wires the hinge for doors added at runtime (e.g. by <see cref="ProceduralLevelBuilder"/>), where <see cref="Reset"/> never runs.</summary>
        public void Initialize(Transform hinge)
        {
            _hinge = hinge;
        }

        /// <summary>
        /// Overrides the default name-based <see cref="SaveId"/>. Every procedurally
        /// placed door shares the GameObject name "DoorHinge", so without this every
        /// door in a level would report the same SaveId and collide in save data.
        /// </summary>
        internal void SetSaveId(string saveId)
        {
            _saveId = saveId;
        }

        private void Update()
        {
            float targetAngle = IsOpen ? _openAngle : 0f;
            _currentAngle = Mathf.MoveTowards(_currentAngle, targetAngle, _openSpeed * Time.deltaTime);

            if (_hinge != null)
            {
                _hinge.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
            }

            if (IsOpen && _autoCloseSeconds > 0f)
            {
                _autoCloseTimer -= Time.deltaTime;
                if (_autoCloseTimer <= 0f)
                {
                    SetOpen(false);
                }
            }
        }

        public string GetInteractionPrompt()
        {
            if (IsOpen)
            {
                return string.IsNullOrEmpty(_customClosePrompt) ? "Close Door" : _customClosePrompt;
            }

            if (IsLocked && _requiredItem != null)
            {
                return $"Locked (Needs {_requiredItem.DisplayName})";
            }

            return string.IsNullOrEmpty(_customOpenPrompt) ? "Open Door" : _customOpenPrompt;
        }

        /// <summary>Placement-time override for a disguised door (e.g. a secret door dressed as a bookcase) — leaving either blank keeps the default "Open Door"/"Close Door" text.</summary>
        public void SetCustomPrompts(string openPrompt, string closePrompt)
        {
            _customOpenPrompt = openPrompt;
            _customClosePrompt = closePrompt;
        }

        /// <summary>Placement-time override wiring the item that unlocks this door. Leave unset for locks that unlock some other way (e.g. PuzzleGateController).</summary>
        public void SetRequiredItem(InventoryItemDefinition item, bool consume = true)
        {
            _requiredItem = item;
            _consumeRequiredItem = consume;
        }

        public bool CanInteract(InteractionContext context)
        {
            return true;
        }

        public void Interact(InteractionContext context)
        {
            if (IsLocked && !TryUnlockWithRequiredItem(context))
            {
                Debug.Log($"'{name}' won't budge — something is blocking the mechanism.", this);
                return;
            }

            var command = new ToggleDoorCommand(this);

            if (GameServices.TryGet<WorldCommandExecutor>(out var executor))
            {
                executor.Submit(command);
            }
            else
            {
                Debug.LogWarning($"No {nameof(WorldCommandExecutor)} registered; executing '{command.CommandId}' directly.", this);
                command.Execute();
            }
        }

        internal void SetOpen(bool isOpen)
        {
            IsOpen = isOpen;
            _autoCloseTimer = isOpen ? _autoCloseSeconds : 0f;
            DoorToggled?.Invoke(this);
        }

        internal void SetLocked(bool isLocked)
        {
            IsLocked = isLocked;
        }

        /// <summary>Consumes <see cref="_requiredItem"/> from the instigator's Inventory and unlocks, or leaves the door locked if it's absent (or there's no required item configured at all).</summary>
        private bool TryUnlockWithRequiredItem(InteractionContext context)
        {
            if (_requiredItem == null || context.Instigator == null)
            {
                return false;
            }

            var inventory = context.Instigator.GetComponentInParent<Inventory>();
            if (inventory == null || !inventory.HasItem(_requiredItem.ItemId))
            {
                return false;
            }

            if (_consumeRequiredItem)
            {
                inventory.TryRemoveItem(_requiredItem.ItemId);
            }

            SetLocked(false);
            return true;
        }

        public object CaptureState()
        {
            return new DoorState(IsOpen, IsLocked);
        }

        public void RestoreState(object state)
        {
            if (state is not DoorState doorState)
            {
                return;
            }

            IsOpen = doorState.IsOpen;
            IsLocked = doorState.IsLocked;
            _currentAngle = IsOpen ? _openAngle : 0f;
            _autoCloseTimer = IsOpen ? _autoCloseSeconds : 0f;
        }

        [Serializable]
        public struct DoorState
        {
            public bool IsOpen;
            public bool IsLocked;

            public DoorState(bool isOpen, bool isLocked)
            {
                IsOpen = isOpen;
                IsLocked = isLocked;
            }
        }
    }
}
