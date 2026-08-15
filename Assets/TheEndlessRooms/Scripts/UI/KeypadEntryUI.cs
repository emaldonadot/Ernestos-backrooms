using System.Text;
using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// The 4-digit code panel a <see cref="EndlessRooms.World.KeypadSafe"/> opens via
    /// <see cref="GameEvents.KeypadOpened"/> — reads raw number keys directly rather than
    /// adding ten more named Input Actions for one panel. Digits fill up to 4, submit on
    /// the 4th, Backspace to correct, the same dismiss action FieldNoteUI uses (Interact)
    /// to cancel out without submitting.
    /// </summary>
    public sealed class KeypadEntryUI : MonoBehaviour
    {
        private const int CodeLength = 4;

        [SerializeField] private Text _digitsText;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private InputActionReference _dismissAction;

        private readonly StringBuilder _digits = new();
        private bool _isOpen;

        private void OnEnable()
        {
            GameEvents.KeypadOpened += HandleKeypadOpened;
            GameEvents.KeypadUnlocked += HandleKeypadUnlocked;

            if (_dismissAction != null)
            {
                _dismissAction.action.Enable();
                _dismissAction.action.performed += HandleDismiss;
            }

            SetVisible(false);
        }

        private void OnDisable()
        {
            GameEvents.KeypadOpened -= HandleKeypadOpened;
            GameEvents.KeypadUnlocked -= HandleKeypadUnlocked;

            if (_dismissAction != null)
            {
                _dismissAction.action.performed -= HandleDismiss;
                _dismissAction.action.Disable();
            }
        }

        private void Update()
        {
            if (!_isOpen || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.backspaceKey.wasPressedThisFrame && _digits.Length > 0)
            {
                _digits.Remove(_digits.Length - 1, 1);
                Refresh();
                return;
            }

            for (int digit = 0; digit <= 9 && _digits.Length < CodeLength; digit++)
            {
                if (DigitKeyPressedThisFrame(digit))
                {
                    _digits.Append(digit);
                    Refresh();
                    break;
                }
            }

            if (_digits.Length == CodeLength)
            {
                GameEvents.RaiseKeypadCodeSubmitted(_digits.ToString());
                _digits.Clear();
                Refresh();
            }
        }

        private static bool DigitKeyPressedThisFrame(int digit)
        {
            Key numberRowKey = Key.Digit0 + digit;
            Key numpadKey = Key.Numpad0 + digit;
            return Keyboard.current[numberRowKey].wasPressedThisFrame || Keyboard.current[numpadKey].wasPressedThisFrame;
        }

        private void HandleKeypadOpened()
        {
            _digits.Clear();
            Refresh();
            SetVisible(true);
        }

        private void HandleKeypadUnlocked()
        {
            SetVisible(false);
        }

        private void HandleDismiss(InputAction.CallbackContext context)
        {
            SetVisible(false);
        }

        private void Refresh()
        {
            if (_digitsText != null)
            {
                _digitsText.text = _digits.ToString().PadRight(CodeLength, '_');
            }
        }

        private void SetVisible(bool visible)
        {
            _isOpen = visible;
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }
        }
    }
}
