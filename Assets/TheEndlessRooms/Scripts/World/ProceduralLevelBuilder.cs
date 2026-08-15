using System;
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
        [Tooltip("Y value used for entry/room/waypoint world positions (GetEntryWorldPosition, TryGetRoomWorldPosition) — NOT the room prefab's own placement Y (always 0). Defaults to 1 to match the shared ModularRoomBase prefab's floor height; a hand-built level with a different actual floor height (e.g. UseExternalGraph) should set this to match, since characters without gravity (The Attendant) never self-correct a mismatched Y.")]
        [SerializeField] private float _roomAnchorHeight = 1f;

        [Header("Materials")]
        [Tooltip("Leave unset to keep the flat DebugColor.Door placeholder. Doors are built fresh at runtime (unlike walls, which share one prefab), so a real material has to be assigned here rather than fixed once on a shared asset.")]
        [SerializeField] private Material _doorMaterial;

        [Header("Landmark")]
        [Tooltip("Leave unset to skip landmark placement entirely. When set, the most-connected eligible room (never entry/exit, never closer than _landmarkMinHopsFromEntry) is instantiated from this prefab instead of its normal RoomDefinition's — see LandmarkNodeSelector.")]
        [SerializeField] private GameObject _landmarkRoomPrefab;
        [SerializeField] [Min(1)] private int _landmarkMinHopsFromEntry = 2;

        [Header("Behavior")]
        [SerializeField] private bool _buildOnStart = true;

        private RoomGraph _lastGraph;
        private bool _lastGraphValid;
        private readonly Dictionary<System.Guid, RoomInstance> _instancesByNodeId = new();

        public RoomGraph LastGraph => _lastGraph;
        public int Seed => _seed;
        public float CellSize => _cellSize;

        /// <summary>The door on the connection leading to the exit room, always set once <see cref="BuildLevel"/> completes — <see cref="RoomGraphValidator"/> guarantees the exit is reachable, so a connection touching it always exists.</summary>
        public Door ExitDoor { get; private set; }

        /// <summary>Raised once <see cref="BuildLevel"/> finishes instantiating, so consumers (e.g. the Map system) never see a partially-built graph.</summary>
        public event Action<RoomGraph> LevelBuilt;

        private void Start()
        {
            if (_buildOnStart)
            {
                BuildLevel();
            }
        }

        /// <summary>Rebuilds using an explicit seed instead of the Inspector-configured one — how <c>SaveService.Load</c> regenerates a saved world.</summary>
        public void BuildLevel(int seedOverride)
        {
            _seed = seedOverride;
            BuildLevel();
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

            LevelBuilt?.Invoke(_lastGraph);
        }

        /// <summary>
        /// Milestone 9: adopts an already-built graph instead of running
        /// <see cref="RoomGraphGenerator"/> — for a hand-authored fixed level (e.g.
        /// Level 1's office building) whose rooms already exist, built by a different
        /// Editor script, not <see cref="InstantiateRooms"/>. This is the seam that lets
        /// <see cref="AttendantController"/>'s graph-based patrol/pathing work completely
        /// unchanged against a fixed layout: it only ever reads <see cref="LastGraph"/>
        /// and calls <see cref="TryGetRoomWorldPosition"/>, neither of which cares
        /// whether the graph came from generation or was constructed by hand, as long as
        /// each <see cref="RoomNode.GridPosition"/> maps to the room's real world
        /// position via <c>GridPosition * _cellSize</c> — callers should pick a _cellSize
        /// (e.g. a small common unit like 0.5) that evenly divides their hand-placed
        /// positions. Skips <see cref="ClearInstantiatedChildren"/>/<see cref="InstantiateRooms"/>/
        /// <see cref="OpenConnectionsAndPlaceDoors"/> entirely — the caller already built
        /// (and is responsible for) the actual scene geometry.
        /// </summary>
        public void UseExternalGraph(RoomGraph graph)
        {
            _lastGraph = graph;
            _lastGraphValid = RoomGraphValidator.Validate(graph).IsValid;
            LevelBuilt?.Invoke(_lastGraph);
        }

        /// <summary>World-space position of the entry room, for spawning the player there.</summary>
        public Vector3 GetEntryWorldPosition()
        {
            RoomNode entryNode = _lastGraph.GetNode(_lastGraph.EntryNodeId);
            return transform.TransformPoint(new Vector3(entryNode.GridPosition.x * _cellSize, _roomAnchorHeight, entryNode.GridPosition.y * _cellSize));
        }

        /// <summary>
        /// World-space position of any room by its graph node id, for anything that
        /// needs to move toward a room without owning grid-to-world math itself —
        /// Milestone 7's <c>AttendantController</c> uses this to convert a BFS path
        /// over <see cref="RoomGraph.GetNeighborIds"/> into movement waypoints.
        /// </summary>
        public bool TryGetRoomWorldPosition(Guid nodeId, out Vector3 position)
        {
            if (_lastGraph == null || !_lastGraph.TryGetNode(nodeId, out RoomNode node))
            {
                position = default;
                return false;
            }

            position = transform.TransformPoint(new Vector3(node.GridPosition.x * _cellSize, _roomAnchorHeight, node.GridPosition.y * _cellSize));
            return true;
        }

        private void ClearInstantiatedChildren()
        {
            _instancesByNodeId.Clear();
            ExitDoor = null;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;

                // Destroy() is Play-mode only; editor tooling (headless verification,
                // an eventual "regenerate" button) calls BuildLevel in Edit mode too.
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void InstantiateRooms()
        {
            Guid? landmarkNodeId = _landmarkRoomPrefab != null
                ? LandmarkNodeSelector.SelectLandmarkNode(_lastGraph, _landmarkMinHopsFromEntry)
                : null;

            foreach (RoomNode node in _lastGraph.Nodes)
            {
                bool isLandmark = landmarkNodeId.HasValue && node.Id == landmarkNodeId.Value;
                GameObject roomPrefab = isLandmark ? _landmarkRoomPrefab : node.Definition?.RoomPrefab;

                if (roomPrefab == null)
                {
                    Debug.LogError($"[ProceduralLevelBuilder] Room node at {node.GridPosition} has no RoomPrefab assigned on its RoomDefinition.", this);
                    continue;
                }

                Vector3 worldPosition = new(node.GridPosition.x * _cellSize, 0f, node.GridPosition.y * _cellSize);
                GameObject instanceGo = Instantiate(roomPrefab, worldPosition, Quaternion.identity, transform);
                instanceGo.name = isLandmark ? $"Landmark_{node.GridPosition.x}_{node.GridPosition.y}" : $"{node.Definition.Category}_{node.GridPosition.x}_{node.GridPosition.y}";

                var roomInstance = instanceGo.GetComponent<RoomInstance>();
                if (roomInstance == null)
                {
                    Debug.LogError($"[ProceduralLevelBuilder] RoomPrefab '{node.Definition.RoomPrefab.name}' has no {nameof(RoomInstance)} component.", instanceGo);
                    continue;
                }

                _instancesByNodeId[node.Id] = roomInstance;

                var roomTrigger = instanceGo.GetComponent<RoomTrigger>();
                roomTrigger?.Initialize(node.Id);

                if (node.Id == _lastGraph.ExitNodeId)
                {
                    SpawnExitPoint(instanceGo.transform);
                }
            }
        }

        private static void SpawnExitPoint(Transform roomTransform)
        {
            GameObject exitGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            exitGo.name = "ExitPoint";
            exitGo.transform.SetParent(roomTransform, false);
            exitGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            exitGo.transform.localScale = Vector3.one * 0.6f;
            exitGo.AddComponent<ExitPoint>();
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

                Door door = PlaceDoor(fromInstance.transform.position, toInstance.transform.position, connection.FromDirection);
                door.SetSaveId($"Door_{connection.FromId}_{connection.ToId}");

                bool touchesExit = connection.FromId == _lastGraph.ExitNodeId || connection.ToId == _lastGraph.ExitNodeId;
                if (touchesExit && ExitDoor == null)
                {
                    ExitDoor = door;
                }
            }
        }

        private Door PlaceDoor(Vector3 fromRoomPosition, Vector3 toRoomPosition, Direction fromDirection)
        {
            Vector3 boundaryPoint = (fromRoomPosition + toRoomPosition) / 2f;

            bool runsAlongX = fromDirection is Direction.North or Direction.South;
            Vector3 panelScale = runsAlongX
                ? new Vector3(_doorWidth, _wallHeight, _wallThickness)
                : new Vector3(_wallThickness, _wallHeight, _doorWidth);
            Vector3 hingeToPanelOffset = runsAlongX
                ? new Vector3(_doorWidth / 2f, _wallHeight / 2f, 0f)
                : new Vector3(0f, _wallHeight / 2f, _doorWidth / 2f);

            // The panel (below) spans from the hinge outward by a full _doorWidth in
            // one direction — it doesn't straddle the hinge. Since the wall's door-sized
            // gap is centered on boundaryPoint (see RoomInstance's split wall pieces),
            // the hinge itself needs to sit at the gap's edge, not its center, or the
            // closed panel only covers half the gap and leaves the other half walkable.
            Vector3 hingePosition = boundaryPoint;
            if (runsAlongX)
            {
                hingePosition.x -= _doorWidth / 2f;
            }
            else
            {
                hingePosition.z -= _doorWidth / 2f;
            }

            var hinge = new GameObject("DoorHinge");
            hinge.transform.SetParent(transform, worldPositionStays: false);
            hinge.transform.position = hingePosition;

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            panel.transform.SetParent(hinge.transform, worldPositionStays: false);
            panel.transform.localPosition = hingeToPanelOffset;
            panel.transform.localScale = panelScale;

            if (_doorMaterial != null)
            {
                panel.GetComponent<Renderer>().sharedMaterial = _doorMaterial;
            }
            else
            {
                DebugColor.Apply(panel, DebugColor.Door);
            }

            var door = hinge.AddComponent<Door>();
            door.Initialize(hinge.transform);
            return door;
        }

        private void OnDrawGizmos()
        {
            if (_lastGraph == null)
            {
                return;
            }

            foreach (RoomNode node in _lastGraph.Nodes)
            {
                Vector3 worldPosition = transform.TransformPoint(new Vector3(node.GridPosition.x * _cellSize, _roomAnchorHeight, node.GridPosition.y * _cellSize));
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

                Vector3 fromWorld = transform.TransformPoint(new Vector3(from.GridPosition.x * _cellSize, _roomAnchorHeight, from.GridPosition.y * _cellSize));
                Vector3 toWorld = transform.TransformPoint(new Vector3(to.GridPosition.x * _cellSize, _roomAnchorHeight, to.GridPosition.y * _cellSize));
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
