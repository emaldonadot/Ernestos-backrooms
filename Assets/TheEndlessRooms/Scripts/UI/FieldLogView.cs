using System.Collections.Generic;
using System.Linq;
using EndlessRooms.Core;
using EndlessRooms.Map;
using EndlessRooms.Procedural;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// Renders the player's Field Log: one icon per known room, a line per known
    /// connection, the current room highlighted, with keyboard pan/zoom while open.
    /// Reads <see cref="FieldLogService"/> purely through its public view types —
    /// never the underlying <see cref="RoomGraph"/> — so it can only ever show what
    /// the player has actually discovered.
    /// </summary>
    public sealed class FieldLogView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _mapRoot;
        [SerializeField] private RectTransform _content;

        [Header("Input")]
        [SerializeField] private InputActionReference _toggleMapAction;
        [SerializeField] private InputActionReference _panMapAction;
        [SerializeField] private InputActionReference _zoomMapAction;

        [Header("Layout")]
        [SerializeField] private float _cellPixelSize = 24f;
        [SerializeField] private float _iconSize = 16f;
        [SerializeField] private float _panSpeed = 200f;
        [SerializeField] private float _zoomSpeed = 1f;
        [SerializeField] private float _minZoom = 0.5f;
        [SerializeField] private float _maxZoom = 3f;

        private FieldLogService _service;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Start()
        {
            TryBindService();
            SetOpen(false);
        }

        private void OnEnable()
        {
            if (_toggleMapAction != null)
            {
                _toggleMapAction.action.Enable();
                _toggleMapAction.action.performed += OnToggleMap;
            }

            _panMapAction?.action.Enable();
            _zoomMapAction?.action.Enable();
        }

        private void OnDisable()
        {
            if (_toggleMapAction != null)
            {
                _toggleMapAction.action.performed -= OnToggleMap;
                _toggleMapAction.action.Disable();
            }

            _panMapAction?.action.Disable();
            _zoomMapAction?.action.Disable();
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.DiscoveryChanged -= Redraw;
            }
        }

        private void Update()
        {
            if (_service == null)
            {
                TryBindService();
            }

            if (!_isOpen || _content == null)
            {
                return;
            }

            Vector2 pan = _panMapAction != null ? _panMapAction.action.ReadValue<Vector2>() : Vector2.zero;
            _content.anchoredPosition += pan * _panSpeed * Time.deltaTime;

            float zoom = _zoomMapAction != null ? _zoomMapAction.action.ReadValue<float>() : 0f;
            if (Mathf.Abs(zoom) > 0.01f)
            {
                float newScale = Mathf.Clamp(_content.localScale.x + zoom * _zoomSpeed * Time.deltaTime, _minZoom, _maxZoom);
                _content.localScale = new Vector3(newScale, newScale, 1f);
            }
        }

        private void TryBindService()
        {
            if (GameServices.TryGet(out _service))
            {
                _service.DiscoveryChanged += Redraw;
                Redraw();
            }
        }

        private void OnToggleMap(InputAction.CallbackContext context)
        {
            SetOpen(!_isOpen);
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            if (_mapRoot != null)
            {
                _mapRoot.SetActive(open);
            }

            if (open)
            {
                Redraw();
            }
        }

        private void Redraw()
        {
            if (_content == null)
            {
                return;
            }

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }

            if (_service == null)
            {
                return;
            }

            List<FieldLogRoomView> rooms = _service.GetKnownRooms().ToList();
            Dictionary<System.Guid, Vector2> positionById = rooms.ToDictionary(r => r.RoomId, r => GridToLocal(r.GridPosition));

            foreach ((System.Guid fromId, System.Guid toId) in _service.GetKnownConnections())
            {
                if (positionById.TryGetValue(fromId, out Vector2 fromPos) && positionById.TryGetValue(toId, out Vector2 toPos))
                {
                    UILineFactory.CreateLine(_content, fromPos, toPos, 3f, Color.gray);
                }
            }

            foreach (FieldLogRoomView room in rooms)
            {
                CreateRoomIcon(room, positionById[room.RoomId]);
            }
        }

        private Vector2 GridToLocal(Vector2Int gridPosition)
        {
            return new Vector2(gridPosition.x * _cellPixelSize, gridPosition.y * _cellPixelSize);
        }

        private void CreateRoomIcon(FieldLogRoomView room, Vector2 localPosition)
        {
            var go = new GameObject($"Room_{room.GridPosition.x}_{room.GridPosition.y}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_content, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = localPosition;
            rect.sizeDelta = new Vector2(_iconSize, _iconSize);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;

            bool isCurrent = _service != null && room.RoomId == _service.CurrentRoomId;
            image.color = isCurrent ? Color.green : ColorForRoom(room);
        }

        private static Color ColorForRoom(FieldLogRoomView room)
        {
            if (room.State == RoomDiscoveryState.Glimpsed || room.Category == null)
            {
                return new Color(0.5f, 0.5f, 0.5f, 0.6f);
            }

            return room.Category switch
            {
                RoomCategory.Standard => Color.white,
                RoomCategory.Corridor => Color.cyan,
                RoomCategory.Junction => Color.yellow,
                RoomCategory.DeadEnd => Color.gray,
                RoomCategory.Exit => Color.magenta,
                _ => Color.blue,
            };
        }
    }
}
