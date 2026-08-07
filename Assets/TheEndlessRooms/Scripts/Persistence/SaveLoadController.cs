using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessRooms.Persistence
{
    /// <summary>
    /// Manual save/load trigger. Feedback is a Console log line, not a dedicated toast
    /// widget — Section 17's full save/load UI is a later-milestone concern; this
    /// milestone is about the underlying system being correct.
    /// </summary>
    public sealed class SaveLoadController : MonoBehaviour
    {
        [SerializeField] private SaveService _saveService;
        [SerializeField] private InputActionReference _quickSaveAction;
        [SerializeField] private InputActionReference _quickLoadAction;

        private void OnEnable()
        {
            if (_quickSaveAction != null)
            {
                _quickSaveAction.action.Enable();
                _quickSaveAction.action.performed += OnQuickSave;
            }

            if (_quickLoadAction != null)
            {
                _quickLoadAction.action.Enable();
                _quickLoadAction.action.performed += OnQuickLoad;
            }
        }

        private void OnDisable()
        {
            if (_quickSaveAction != null)
            {
                _quickSaveAction.action.performed -= OnQuickSave;
                _quickSaveAction.action.Disable();
            }

            if (_quickLoadAction != null)
            {
                _quickLoadAction.action.performed -= OnQuickLoad;
                _quickLoadAction.action.Disable();
            }
        }

        private void OnQuickSave(InputAction.CallbackContext context)
        {
            _saveService.Save();
        }

        private void OnQuickLoad(InputAction.CallbackContext context)
        {
            _saveService.Load();
        }
    }
}
