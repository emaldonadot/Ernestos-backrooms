using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// A designated hiding place (PRD Section 13). Toggling in/out raises
    /// <see cref="GameEvents.PlayerHiddenChanged"/> rather than referencing the player
    /// rig directly — every <see cref="IDetectable"/> implementation (PC and VR) mirrors
    /// the flag into its own <c>IsHidden</c>, and Milestone 7's Attendant perception
    /// reads it from there. Occupancy is tracked per spot so a second hiding spot can't
    /// be "entered" while already hidden in another one.
    /// </summary>
    public sealed class HidingSpot : MonoBehaviour, IInteractable, ISaveable
    {
        [Tooltip("Leave blank to use the GameObject name as the save identifier.")]
        [SerializeField] private string _saveId = "";

        [Tooltip("Leave unset to keep the old behavior (freeze movement in place, no teleport). When set, the player's CharacterController is moved here while hidden and restored to its pre-hide position/rotation on exit — e.g. actually inside a closet, or under a desk.")]
        [SerializeField] private Transform _hideAnchor;

        private bool _isOccupied;
        private Vector3 _preHidePosition;
        private Quaternion _preHideRotation;

        public bool IsOccupied => _isOccupied;
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
            return _isOccupied ? "Come Out" : "Hide";
        }

        public bool CanInteract(InteractionContext context)
        {
            return true;
        }

        public void Interact(InteractionContext context)
        {
            _isOccupied = !_isOccupied;

            if (_hideAnchor != null && context.Instigator != null)
            {
                Transform playerTransform = context.Instigator.transform;
                var characterController = context.Instigator.GetComponent<CharacterController>();

                if (_isOccupied)
                {
                    _preHidePosition = playerTransform.position;
                    _preHideRotation = playerTransform.rotation;
                    // Only yaw, not the hide anchor's full rotation — the player rig
                    // splits yaw (root transform) from pitch (a child camera pivot), so
                    // forcing a pitch onto the root here would just be silently ignored
                    // by the next mouse-look update anyway.
                    Teleport(characterController, playerTransform, _hideAnchor.position, Quaternion.Euler(0f, _hideAnchor.eulerAngles.y, 0f));
                }
                else
                {
                    Teleport(characterController, playerTransform, _preHidePosition, _preHideRotation);
                }
            }

            GameEvents.RaisePlayerHiddenChanged(_isOccupied);
        }

        private static void Teleport(CharacterController characterController, Transform playerTransform, Vector3 position, Quaternion rotation)
        {
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            playerTransform.SetPositionAndRotation(position, rotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        public object CaptureState()
        {
            return new HidingSpotState(_isOccupied);
        }

        public void RestoreState(object state)
        {
            if (state is not HidingSpotState hidingState)
            {
                return;
            }

            _isOccupied = hidingState.IsOccupied;
            GameEvents.RaisePlayerHiddenChanged(_isOccupied);
        }

        [Serializable]
        public struct HidingSpotState
        {
            public bool IsOccupied;

            public HidingSpotState(bool isOccupied)
            {
                IsOccupied = isOccupied;
            }
        }
    }
}
