using EndlessRooms.Core;
using EndlessRooms.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// Minimal HUD prompt ("Open Door", "Pick up Test Item") shown while an
    /// <see cref="IInteractable"/> is in focus. Reads only <see cref="InteractionCaster"/>'s
    /// public event, so it has no dependency on how interactables are found or executed.
    /// </summary>
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private InteractionCaster _interactionCaster;
        [SerializeField] private Text _promptText;
        [SerializeField] private GameObject _promptRoot;

        private IInteractable _focused;

        private void OnEnable()
        {
            if (_interactionCaster != null)
            {
                _interactionCaster.FocusChanged += HandleFocusChanged;
            }

            SetVisible(false);
        }

        private void OnDisable()
        {
            if (_interactionCaster != null)
            {
                _interactionCaster.FocusChanged -= HandleFocusChanged;
            }
        }

        private void Update()
        {
            // FocusChanged only fires when the focused *object reference* changes — a
            // HidingSpot's Hide/Come Out or a LockableDrawer's Locked/Use-X-To-Open text
            // changes while the player keeps looking at the same object, so the prompt
            // needs its own per-frame refresh instead of only reacting to that event.
            if (_focused != null)
            {
                RefreshText();
            }
        }

        private void HandleFocusChanged(IInteractable interactable)
        {
            _focused = interactable;

            if (interactable == null)
            {
                SetVisible(false);
                return;
            }

            RefreshText();
            SetVisible(true);
        }

        private void RefreshText()
        {
            if (_promptText != null && _focused != null)
            {
                _promptText.text = _focused.GetInteractionPrompt();
            }
        }

        private void SetVisible(bool visible)
        {
            if (_promptRoot != null)
            {
                _promptRoot.SetActive(visible);
            }
        }
    }
}
