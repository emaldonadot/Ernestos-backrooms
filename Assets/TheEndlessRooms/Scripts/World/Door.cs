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

        private float _currentAngle;

        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }
        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;

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
        internal void Initialize(Transform hinge)
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
        }

        public string GetInteractionPrompt()
        {
            return IsOpen ? "Close Door" : "Open Door";
        }

        public bool CanInteract(InteractionContext context)
        {
            return true;
        }

        public void Interact(InteractionContext context)
        {
            if (IsLocked)
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
        }

        internal void SetLocked(bool isLocked)
        {
            IsLocked = isLocked;
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
