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

        private bool _isOccupied;

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
            GameEvents.RaisePlayerHiddenChanged(_isOccupied);
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
