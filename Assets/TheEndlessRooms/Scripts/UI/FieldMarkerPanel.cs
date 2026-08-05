using EndlessRooms.Core;
using EndlessRooms.Map;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// Lets the player add a <see cref="FieldMark"/> of a chosen type + note at their
    /// current room, and remove existing marks in that room. The type buttons, note
    /// field, and mark-row template are wired once via the Inspector (built by the
    /// scene setup tooling) — this script only ever reacts to clicks.
    /// </summary>
    public sealed class FieldMarkerPanel : MonoBehaviour
    {
        [SerializeField] private Button[] _typeButtons;
        [SerializeField] private InputField _noteInput;
        [SerializeField] private Button _addButton;
        [SerializeField] private Transform _marksListRoot;
        [SerializeField] private GameObject _markRowTemplate;

        private FieldLogService _service;
        private FieldMarkType _selectedType = FieldMarkType.Danger;

        private void Start()
        {
            for (int i = 0; i < _typeButtons.Length; i++)
            {
                FieldMarkType type = (FieldMarkType)i;
                _typeButtons[i].onClick.AddListener(() => _selectedType = type);
            }

            if (_addButton != null)
            {
                _addButton.onClick.AddListener(AddMarkerAtCurrentRoom);
            }

            if (_markRowTemplate != null)
            {
                _markRowTemplate.SetActive(false);
            }

            TryBindService();
        }

        private void Update()
        {
            if (_service == null)
            {
                TryBindService();
            }
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.MarksChanged -= RefreshMarksList;
            }
        }

        private void TryBindService()
        {
            if (GameServices.TryGet(out _service))
            {
                _service.MarksChanged += RefreshMarksList;
                RefreshMarksList();
            }
        }

        private void AddMarkerAtCurrentRoom()
        {
            if (_service == null || _service.CurrentRoomId == System.Guid.Empty)
            {
                return;
            }

            string note = _noteInput != null ? _noteInput.text : string.Empty;
            _service.AddMark(_service.CurrentRoomId, Vector2.zero, _selectedType, note);

            if (_noteInput != null)
            {
                _noteInput.text = string.Empty;
            }
        }

        private void RefreshMarksList()
        {
            if (_marksListRoot == null || _service == null)
            {
                return;
            }

            for (int i = _marksListRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _marksListRoot.GetChild(i);
                if (child.gameObject != _markRowTemplate)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (FieldMark mark in _service.Marks)
            {
                if (mark.RoomId != _service.CurrentRoomId)
                {
                    continue;
                }

                CreateMarkRow(mark);
            }
        }

        private void CreateMarkRow(FieldMark mark)
        {
            if (_markRowTemplate == null)
            {
                return;
            }

            GameObject row = Instantiate(_markRowTemplate, _marksListRoot);
            row.SetActive(true);

            Text label = row.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = string.IsNullOrEmpty(mark.Note) ? mark.Type.ToString() : $"{mark.Type}: {mark.Note}";
            }

            Button removeButton = row.GetComponentInChildren<Button>();
            if (removeButton != null)
            {
                removeButton.onClick.AddListener(() => _service.RemoveMark(mark.Id));
            }
        }
    }
}
