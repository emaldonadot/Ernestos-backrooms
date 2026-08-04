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
        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;

        private void Reset()
        {
            _hinge = transform;
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

        public object CaptureState()
        {
            return new DoorState(IsOpen);
        }

        public void RestoreState(object state)
        {
            if (state is not DoorState doorState)
            {
                return;
            }

            IsOpen = doorState.IsOpen;
            _currentAngle = IsOpen ? _openAngle : 0f;
        }

        [Serializable]
        public struct DoorState
        {
            public bool IsOpen;

            public DoorState(bool isOpen)
            {
                IsOpen = isOpen;
            }
        }
    }
}
