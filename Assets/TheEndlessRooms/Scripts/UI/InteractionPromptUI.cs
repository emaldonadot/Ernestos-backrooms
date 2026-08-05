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

        private void HandleFocusChanged(IInteractable interactable)
        {
            if (interactable == null)
            {
                SetVisible(false);
                return;
            }

            if (_promptText != null)
            {
                _promptText.text = interactable.GetInteractionPrompt();
            }

            SetVisible(true);
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
