using System;
using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// A safe that opens a keypad UI (via <see cref="GameEvents.KeypadOpened"/>, mirroring
    /// FieldNote's decoupling from FieldNoteUI) rather than checking an inventory item —
    /// the whole point of this puzzle beat is that the code has to be read elsewhere
    /// (the UV-revealed bathroom clue) and typed in, not carried as an item.
    /// </summary>
    public sealed class KeypadSafe : MonoBehaviour, IInteractable, IProgressionGate, ISaveable
    {
        [SerializeField] private string _code = "0000";
        [Tooltip("Activated once the correct code is entered — typically the InventoryPickup sitting inside the safe.")]
        [SerializeField] private GameObject _revealedContent;
        [Tooltip("Leave blank to use the GameObject name as the save identifier.")]
        [SerializeField] private string _saveId = "";

        private static KeypadSafe _activeSafe;

        public bool IsUnlocked { get; private set; }

        public event Action Changed;

        public string SaveId => string.IsNullOrEmpty(_saveId) ? name : _saveId;

        private void Awake()
        {
            if (_revealedContent != null)
            {
                _revealedContent.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameEvents.KeypadCodeSubmitted += HandleCodeSubmitted;
            if (GameServices.TryGet<SaveableRegistry>(out var registry))
            {
                registry.Register(this);
            }
        }

        private void OnDisable()
        {
            GameEvents.KeypadCodeSubmitted -= HandleCodeSubmitted;
            if (GameServices.TryGet<SaveableRegistry>(out var registry))
            {
                registry.Unregister(this);
            }
        }

        public string GetInteractionPrompt()
        {
            return IsUnlocked ? "Open Safe" : "Enter Code";
        }

        public bool CanInteract(InteractionContext context)
        {
            return !IsUnlocked;
        }

        public void Interact(InteractionContext context)
        {
            if (IsUnlocked)
            {
                return;
            }

            _activeSafe = this;
            GameEvents.RaiseKeypadOpened();
        }

        private void HandleCodeSubmitted(string code)
        {
            if (_activeSafe != this || IsUnlocked)
            {
                return;
            }

            if (code != _code)
            {
                Debug.Log($"'{name}' — wrong code.", this);
                return;
            }

            Unlock();
            GameEvents.RaiseKeypadUnlocked();
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
            return new SafeState(IsUnlocked);
        }

        public void RestoreState(object state)
        {
            if (state is not SafeState safeState || !safeState.IsUnlocked)
            {
                return;
            }

            Unlock();
        }

        [Serializable]
        public struct SafeState
        {
            public bool IsUnlocked;

            public SafeState(bool isUnlocked)
            {
                IsUnlocked = isUnlocked;
            }
        }
    }
}
