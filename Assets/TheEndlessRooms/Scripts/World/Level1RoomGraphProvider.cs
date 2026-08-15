using System;
using System.Collections.Generic;
using EndlessRooms.Procedural;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Constructs the same synthetic <see cref="RoomGraph"/> every time Level 1 actually
    /// starts (Awake — before <see cref="AttendantController"/>'s OnEnable needs it) and
    /// hands it to <see cref="ProceduralLevelBuilder.UseExternalGraph"/>. Necessary
    /// because that builder's graph field isn't Unity-serialized, so baking a graph in
    /// once at edit time wouldn't survive a scene reload or a build — this has to run
    /// fresh on every actual play session. One node per corridor cell (main spine rows +
    /// the two cross-arm cells) and one per room, positions taken from
    /// <see cref="Level1Layout"/> so they can never drift out of sync with the actual
    /// geometry.
    /// </summary>
    public sealed class Level1RoomGraphProvider : MonoBehaviour
    {
        [SerializeField] private ProceduralLevelBuilder _levelBuilder;

        private void Awake()
        {
            if (_levelBuilder == null)
            {
                Debug.LogError($"{nameof(Level1RoomGraphProvider)} on '{name}' has no level builder assigned.", this);
                return;
            }

            _levelBuilder.UseExternalGraph(BuildGraph());
        }

        /// <summary>Public + static so the graph shape (node count, connectivity, entry/exit) is EditMode-testable without a scene.</summary>
        public static RoomGraph BuildGraph()
        {
            var graph = new RoomGraph();

            Vector2Int ToGrid(Vector3 world) => new(
                Mathf.RoundToInt(world.x / Level1Layout.GraphCellSize),
                Mathf.RoundToInt(world.z / Level1Layout.GraphCellSize));

            Guid AddNode(Vector3 worldPosition)
            {
                Guid id = Guid.NewGuid();
                graph.AddNode(new RoomNode(id, null, ToGrid(worldPosition)));
                return id;
            }

            var corridorNodeIds = new Dictionary<int, Guid>();
            for (int row = 1; row <= Level1Layout.TotalRows; row++)
            {
                corridorNodeIds[row] = AddNode(Level1Layout.CorridorCellCenter(row));
            }

            for (int row = 1; row < Level1Layout.TotalRows; row++)
            {
                graph.AddConnection(new RoomConnection(corridorNodeIds[row], corridorNodeIds[row + 1], Direction.North));
            }

            Guid westArmId = AddNode(Level1Layout.CrossArmCorridorCellCenter(Level1Layout.Side.West));
            Guid eastArmId = AddNode(Level1Layout.CrossArmCorridorCellCenter(Level1Layout.Side.East));
            graph.AddConnection(new RoomConnection(corridorNodeIds[Level1Layout.CrossCorridorRow], westArmId, Direction.West));
            graph.AddConnection(new RoomConnection(corridorNodeIds[Level1Layout.CrossCorridorRow], eastArmId, Direction.East));

            foreach (Level1Layout.OfficeSpec spec in Level1Layout.Offices)
            {
                Vector3 position = spec.IsCrossArm ? Level1Layout.CrossArmRoomCenter(spec.Side) : Level1Layout.RoomCenter(spec.Row, spec.Side);
                Guid officeId = AddNode(position);

                Guid corridorNeighbor = spec.IsCrossArm
                    ? (spec.Side == Level1Layout.Side.West ? westArmId : eastArmId)
                    : corridorNodeIds[spec.Row];
                Direction direction = spec.Side == Level1Layout.Side.West ? Direction.West : Direction.East;
                graph.AddConnection(new RoomConnection(corridorNeighbor, officeId, direction));
            }

            Guid womenId = AddNode(Level1Layout.RoomCenter(Level1Layout.BathroomRow, Level1Layout.Side.West));
            Guid menId = AddNode(Level1Layout.RoomCenter(Level1Layout.BathroomRow, Level1Layout.Side.East));
            graph.AddConnection(new RoomConnection(corridorNodeIds[Level1Layout.BathroomRow], womenId, Direction.West));
            graph.AddConnection(new RoomConnection(corridorNodeIds[Level1Layout.BathroomRow], menId, Direction.East));

            Guid courtyardWestId = AddNode(Level1Layout.RoomCenter(Level1Layout.CourtyardRow, Level1Layout.Side.West));
            Guid courtyardEastId = AddNode(Level1Layout.RoomCenter(Level1Layout.CourtyardRow, Level1Layout.Side.East));
            graph.AddConnection(new RoomConnection(corridorNodeIds[Level1Layout.CourtyardRow], courtyardWestId, Direction.West));
            graph.AddConnection(new RoomConnection(corridorNodeIds[Level1Layout.CourtyardRow], courtyardEastId, Direction.East));

            graph.SetEntry(corridorNodeIds[1]);
            graph.SetExit(corridorNodeIds[Level1Layout.TotalRows]);

            return graph;
        }
    }
}
