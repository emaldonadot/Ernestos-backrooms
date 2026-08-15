using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// Displays a <see cref="EndlessRooms.World.FieldNote"/>'s text fragment on
    /// <see cref="GameEvents.FieldNoteOpened"/> — reads only the Core event, so it has
    /// no dependency on <c>EndlessRooms.World</c>. Dismissed by pressing the same
    /// Interact action again (read note, press E again to close).
    /// </summary>
    public sealed class FieldNoteUI : MonoBehaviour
    {
        [SerializeField] private Text _fragmentText;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private InputActionReference _dismissAction;

        // The same Interact press that opens the note (via InteractionCaster ->
        // FieldNote.Interact -> GameEvents.FieldNoteOpened) also fires this component's
        // own _dismissAction.performed subscription on the *same* underlying
        // InputAction, in the same synchronous dispatch — without this guard, opening a
        // note also immediately closed it again before the player ever saw it.
        private bool _justOpened;

        private void OnEnable()
        {
            GameEvents.FieldNoteOpened += HandleFieldNoteOpened;

            if (_dismissAction != null)
            {
                _dismissAction.action.Enable();
                _dismissAction.action.performed += HandleDismiss;
            }

            SetVisible(false);
        }

        private void OnDisable()
        {
            GameEvents.FieldNoteOpened -= HandleFieldNoteOpened;

            if (_dismissAction != null)
            {
                _dismissAction.action.performed -= HandleDismiss;
                _dismissAction.action.Disable();
            }
        }

        private void HandleFieldNoteOpened(string fragmentText)
        {
            if (_fragmentText != null)
            {
                _fragmentText.text = fragmentText;
            }

            SetVisible(true);
            _justOpened = true;
        }

        private void HandleDismiss(InputAction.CallbackContext context)
        {
            if (_justOpened)
            {
                _justOpened = false;
                return;
            }

            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }
        }
    }
}
