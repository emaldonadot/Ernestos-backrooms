using System.Collections.Generic;
using EndlessRooms.Procedural;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Runs <see cref="RoomGraphGenerator"/> and instantiates the result: one modular
    /// room prefab per node on a grid, walls opened and a <see cref="Door"/> placed at
    /// every connection. Overlap is impossible by construction — the generator only
    /// ever places one node per grid cell.
    /// </summary>
    public sealed class ProceduralLevelBuilder : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private int _seed;
        [SerializeField] private int _roomCount = 10;
        [SerializeField] private RoomDefinition _entryDefinition;
        [SerializeField] private RoomDefinition _exitDefinition;
        [SerializeField] private RoomDefinition[] _fillerDefinitions;

        [Header("Spatial")]
        [SerializeField] private float _cellSize = 6f;
        [SerializeField] private float _wallHeight = 3f;
        [SerializeField] private float _wallThickness = 0.2f;
        [SerializeField] private float _doorWidth = 2f;

        [Header("Behavior")]
        [SerializeField] private bool _buildOnStart = true;

        private RoomGraph _lastGraph;
        private bool _lastGraphValid;
        private readonly Dictionary<System.Guid, RoomInstance> _instancesByNodeId = new();

        public RoomGraph LastGraph => _lastGraph;

        private void Start()
        {
            if (_buildOnStart)
            {
                BuildLevel();
            }
        }

        public void BuildLevel()
        {
            var settings = new RoomGraphGenerationSettings
            {
                Seed = _seed,
                RoomCount = _roomCount,
                EntryDefinition = _entryDefinition,
                ExitDefinition = _exitDefinition,
                FillerDefinitions = _fillerDefinitions,
            };

            _lastGraph = RoomGraphGenerator.GenerateValidated(settings);
            _lastGraphValid = RoomGraphValidator.Validate(_lastGraph).IsValid;

            ClearInstantiatedChildren();
            InstantiateRooms();
            OpenConnectionsAndPlaceDoors();
        }

        private void ClearInstantiatedChildren()
        {
            _instancesByNodeId.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private void InstantiateRooms()
        {
            foreach (RoomNode node in _lastGraph.Nodes)
            {
                if (node.Definition == null || node.Definition.RoomPrefab == null)
                {
                    Debug.LogError($"[ProceduralLevelBuilder] Room node at {node.GridPosition} has no RoomPrefab assigned on its RoomDefinition.", this);
                    continue;
                }

                Vector3 worldPosition = new(node.GridPosition.x * _cellSize, 0f, node.GridPosition.y * _cellSize);
                GameObject instanceGo = Instantiate(node.Definition.RoomPrefab, worldPosition, Quaternion.identity, transform);
                instanceGo.name = $"{node.Definition.Category}_{node.GridPosition.x}_{node.GridPosition.y}";

                var roomInstance = instanceGo.GetComponent<RoomInstance>();
                if (roomInstance == null)
                {
                    Debug.LogError($"[ProceduralLevelBuilder] RoomPrefab '{node.Definition.RoomPrefab.name}' has no {nameof(RoomInstance)} component.", instanceGo);
                    continue;
                }

                _instancesByNodeId[node.Id] = roomInstance;
            }
        }

        private void OpenConnectionsAndPlaceDoors()
        {
            foreach (RoomConnection connection in _lastGraph.Connections)
            {
                if (!_instancesByNodeId.TryGetValue(connection.FromId, out RoomInstance fromInstance)
                    || !_instancesByNodeId.TryGetValue(connection.ToId, out RoomInstance toInstance))
                {
                    continue;
                }

                Direction toDirection = connection.FromDirection.Opposite();
                fromInstance.OpenWall(connection.FromDirection);
                toInstance.OpenWall(toDirection);

                PlaceDoor(fromInstance.transform.position, toInstance.transform.position, connection.FromDirection);
            }
        }

        private void PlaceDoor(Vector3 fromRoomPosition, Vector3 toRoomPosition, Direction fromDirection)
        {
            Vector3 boundaryPoint = (fromRoomPosition + toRoomPosition) / 2f;

            bool runsAlongX = fromDirection is Direction.North or Direction.South;
            Vector3 panelScale = runsAlongX
                ? new Vector3(_doorWidth, _wallHeight, _wallThickness)
                : new Vector3(_wallThickness, _wallHeight, _doorWidth);
            Vector3 hingeToPanelOffset = runsAlongX
                ? new Vector3(_doorWidth / 2f, _wallHeight / 2f, 0f)
                : new Vector3(0f, _wallHeight / 2f, _doorWidth / 2f);

            var hinge = new GameObject("DoorHinge");
            hinge.transform.SetParent(transform, worldPositionStays: false);
            hinge.transform.position = boundaryPoint;

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            panel.transform.SetParent(hinge.transform, worldPositionStays: false);
            panel.transform.localPosition = hingeToPanelOffset;
            panel.transform.localScale = panelScale;

            var door = hinge.AddComponent<Door>();
            door.Initialize(hinge.transform);
        }

        private void OnDrawGizmos()
        {
            if (_lastGraph == null)
            {
                return;
            }

            foreach (RoomNode node in _lastGraph.Nodes)
            {
                Vector3 worldPosition = transform.TransformPoint(new Vector3(node.GridPosition.x * _cellSize, 1f, node.GridPosition.y * _cellSize));
                Gizmos.color = GizmoColorFor(node.Definition != null ? node.Definition.Category : RoomCategory.Standard);
                Gizmos.DrawSphere(worldPosition, 0.5f);
            }

            Gizmos.color = _lastGraphValid ? Color.green : Color.red;
            foreach (RoomConnection connection in _lastGraph.Connections)
            {
                if (!_lastGraph.TryGetNode(connection.FromId, out RoomNode from) || !_lastGraph.TryGetNode(connection.ToId, out RoomNode to))
                {
                    continue;
                }

                Vector3 fromWorld = transform.TransformPoint(new Vector3(from.GridPosition.x * _cellSize, 1f, from.GridPosition.y * _cellSize));
                Vector3 toWorld = transform.TransformPoint(new Vector3(to.GridPosition.x * _cellSize, 1f, to.GridPosition.y * _cellSize));
                Gizmos.DrawLine(fromWorld, toWorld);
            }
        }

        private static Color GizmoColorFor(RoomCategory category)
        {
            return category switch
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
