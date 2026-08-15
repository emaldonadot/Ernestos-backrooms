using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Shared layout math/data for Milestone 9's Level 1 (the fixed, hand-authored
    /// cross-spine office building) — the one source of truth for both the Editor scene
    /// builder (geometry placement) and <see cref="Level1RoomGraphProvider"/> (the
    /// runtime graph reconstruction the Attendant's pathing needs every time the scene
    /// actually starts, since <see cref="ProceduralLevelBuilder"/>'s graph field isn't
    /// Unity-serialized — see <see cref="ProceduralLevelBuilder.UseExternalGraph"/>'s
    /// doc comment). Pure data/math, no MonoBehaviour, so it's usable from both an
    /// Editor-only script and a runtime one without any assembly issues.
    /// </summary>
    public static class Level1Layout
    {
        public const float RoomDepthX = 5f;
        public const float RoomWidthZ = 6f;
        public const float CorridorWidth = 6f;
        public const float RowSpacing = 6f;
        public const int TotalRows = 9;
        public const int CourtyardRow = 4;
        public const int BathroomRow = 8;
        public const int CrossCorridorRow = 6;

        /// <summary>
        /// A small common unit that evenly divides every hand-placed position below
        /// (all multiples of 0.5m) — used only for ProceduralLevelBuilder's
        /// GridPosition * cellSize world-position math, unrelated to the real geometry
        /// scale (which is built directly in world space, not on this grid at all).
        /// </summary>
        public const float GraphCellSize = 0.5f;

        public enum Side
        {
            West,
            East,
        }

        public struct OfficeSpec
        {
            public string Id;
            public int Row;
            public Side Side;
            public bool UseMeetingTable;
            public bool HasShelf;
            public bool IsCrossArm;
        }

        public static readonly OfficeSpec[] Offices =
        {
            new() { Id = "R01", Row = 1, Side = Side.West, HasShelf = false },
            new() { Id = "R02", Row = 1, Side = Side.East, HasShelf = true },
            new() { Id = "R03", Row = 2, Side = Side.West, HasShelf = true },
            new() { Id = "R04", Row = 2, Side = Side.East, UseMeetingTable = true, HasShelf = true },
            new() { Id = "R05", Row = 3, Side = Side.West, HasShelf = true },
            new() { Id = "R06", Row = 3, Side = Side.East, HasShelf = true },
            new() { Id = "R07", Row = 5, Side = Side.West, HasShelf = false },
            new() { Id = "R08", Row = 5, Side = Side.East, UseMeetingTable = true, HasShelf = true },
            new() { Id = "R09", Row = 7, Side = Side.West, HasShelf = true },
            new() { Id = "R10", Row = 7, Side = Side.East, HasShelf = true },
            new() { Id = "R11", Row = 9, Side = Side.West, HasShelf = true },
            new() { Id = "R12", Row = 9, Side = Side.East, HasShelf = true },
            new() { Id = "R13", Row = CrossCorridorRow, Side = Side.West, HasShelf = false, IsCrossArm = true },
            new() { Id = "R14", Row = CrossCorridorRow, Side = Side.East, UseMeetingTable = true, HasShelf = false, IsCrossArm = true },
        };

        public static float RowCenterZ(int row) => row * RowSpacing - RowSpacing / 2f;

        /// <summary>Converts a room-local offset (local +X always points toward the door/corridor, regardless of which side the room is on) to world space.</summary>
        public static Vector3 LocalToWorld(Vector3 roomCenter, Side side, Vector3 local)
        {
            float xSign = side == Side.West ? 1f : -1f;
            return roomCenter + new Vector3(local.x * xSign, local.y, local.z);
        }

        public static Vector3 RoomCenter(int row, Side side)
        {
            float z = RowCenterZ(row);
            float x = (CorridorWidth / 2f + RoomDepthX / 2f) * (side == Side.West ? -1f : 1f);
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// R13/R14 sit at the far ends of the cross corridor's own east/west extension,
        /// one corridor-width further out than a normal room slot — not directly off the
        /// main north-south corridor.
        /// </summary>
        public static Vector3 CrossArmRoomCenter(Side side)
        {
            float z = RowCenterZ(CrossCorridorRow);
            float armInnerEdge = CorridorWidth / 2f + CorridorWidth;
            float x = (armInnerEdge + RoomDepthX / 2f) * (side == Side.West ? -1f : 1f);
            return new Vector3(x, 0f, z);
        }

        public static Vector3 CorridorCellCenter(int row) => new(0f, 0f, RowCenterZ(row));

        public static Vector3 CrossArmCorridorCellCenter(Side side)
        {
            float z = RowCenterZ(CrossCorridorRow);
            float mainEdge = CorridorWidth / 2f;
            float x = (mainEdge + CorridorWidth / 2f) * (side == Side.West ? -1f : 1f);
            return new Vector3(x, 0f, z);
        }
    }
}
