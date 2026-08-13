using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for Milestone 9's Level 1: a fixed, hand-authored cross-spine
    /// office building (not procedural — see docs/features/milestone-9-playable-office-levels.md).
    /// 14 offices at 5m(deep) x 6m(wide) x 3m(tall), a 6m-wide corridor, 2 paired
    /// bathrooms, 2 open-air courtyards. This pass builds the room/corridor shell,
    /// doors, and placeholder furniture collision only — content (clues/keys/locks/
    /// Attendant/jump scares) and the win/lose game flow are separate follow-up passes.
    /// </summary>
    public static class Milestone9Level1AssetBuilder
    {
        private const float RoomDepthX = 5f;
        private const float RoomWidthZ = 6f;
        private const float WallHeight = 3f;
        private const float WallThickness = 0.2f;
        private const float DoorWidth = 2f;
        private const float CorridorWidth = 6f;
        private const float RowSpacing = 6f;

        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone9_Level1TestScene.unity";

        private enum Side
        {
            West,
            East,
        }

        private struct OfficeSpec
        {
            public string Id;
            public int Row;
            public Side Side;
            public bool UseMeetingTable;
            public bool HasShelf;
            public bool IsCrossArm;
        }

        private static readonly OfficeSpec[] Offices =
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
            new() { Id = "R13", Row = 6, Side = Side.West, HasShelf = false, IsCrossArm = true },
            new() { Id = "R14", Row = 6, Side = Side.East, UseMeetingTable = true, HasShelf = false, IsCrossArm = true },
        };

        private const int CourtyardRow = 4;
        private const int BathroomRow = 8;
        private const int TotalRows = 9;

        [MenuItem("Tools/The Endless Rooms/M9 Level 1/Build Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            var levelRoot = new GameObject("Level1_HorrorOffice").transform;

            BuildMainCorridor(levelRoot);
            BuildCrossCorridorArms(levelRoot);

            foreach (OfficeSpec office in Offices)
            {
                BuildOffice(levelRoot, office);
            }

            BuildCourtyard(levelRoot, CourtyardRow, Side.West);
            BuildCourtyard(levelRoot, CourtyardRow, Side.East);
            BuildBathroom(levelRoot, BathroomRow, Side.West, "Bathroom_Women");
            BuildBathroom(levelRoot, BathroomRow, Side.East, "Bathroom_Men");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone9Level1AssetBuilder] Built and saved '{ScenePath}' — {Offices.Length} offices, 2 bathrooms, 2 courtyards.");
        }

        private static float RowCenterZ(int row) => row * RowSpacing - RowSpacing / 2f;

        /// <summary>Converts a room-local offset (local +X always points toward the door/corridor, regardless of which side the room is on) to world space.</summary>
        private static Vector3 LocalToWorld(Vector3 roomCenter, Side side, Vector3 local)
        {
            float xSign = side == Side.West ? 1f : -1f;
            return roomCenter + new Vector3(local.x * xSign, local.y, local.z);
        }

        private static Vector3 RoomCenter(int row, Side side)
        {
            float z = RowCenterZ(row);
            float x = (CorridorWidth / 2f + RoomDepthX / 2f) * (side == Side.West ? -1f : 1f);
            return new Vector3(x, 0f, z);
        }

        private static Vector3 CrossArmRoomCenter(Side side)
        {
            // One corridor-width further out than a normal room slot, since R13/R14 sit
            // at the far ends of the cross corridor's own east/west extension rather than
            // directly off the main north-south corridor.
            float z = RowCenterZ(6);
            float armInnerEdge = CorridorWidth / 2f + CorridorWidth; // main corridor edge + one cross-arm cell
            float x = (armInnerEdge + RoomDepthX / 2f) * (side == Side.West ? -1f : 1f);
            return new Vector3(x, 0f, z);
        }

        // ---------------------------------------------------------------- corridor shell

        private static void BuildMainCorridor(Transform parent)
        {
            var corridorGo = new GameObject("MainCorridor");
            corridorGo.transform.SetParent(parent, false);

            float totalLength = TotalRows * RowSpacing;
            float centerZ = totalLength / 2f;

            CreateBlock(corridorGo.transform, "Floor", new Vector3(0f, 0f, centerZ), new Vector3(CorridorWidth, WallThickness, totalLength));
            CreateBlock(corridorGo.transform, "Ceiling", new Vector3(0f, WallHeight, centerZ), new Vector3(CorridorWidth, WallThickness, totalLength));

            // End caps: solid wall at the very south (before START) and a doorway-height
            // opening marker at the north (EXIT) — the actual ExitPoint trigger is added
            // in the content-wiring pass, this just closes off the shell.
            CreateBlock(corridorGo.transform, "Wall_SouthEnd", new Vector3(0f, WallHeight / 2f, -WallThickness / 2f), new Vector3(CorridorWidth, WallHeight, WallThickness));
            CreateBlock(corridorGo.transform, "Wall_NorthEnd", new Vector3(0f, WallHeight / 2f, totalLength + WallThickness / 2f), new Vector3(CorridorWidth, WallHeight, WallThickness));
        }

        private static void BuildCrossCorridorArms(Transform parent)
        {
            var crossGo = new GameObject("CrossCorridorArms");
            crossGo.transform.SetParent(parent, false);

            float z = RowCenterZ(6);
            float mainEdge = CorridorWidth / 2f;
            float armLength = CorridorWidth;

            foreach (Side side in new[] { Side.West, Side.East })
            {
                float sign = side == Side.West ? -1f : 1f;
                float armCenterX = (mainEdge + armLength / 2f) * sign;

                CreateBlock(crossGo.transform, $"Floor_{side}", new Vector3(armCenterX, 0f, z), new Vector3(armLength, WallThickness, CorridorWidth));
                CreateBlock(crossGo.transform, $"Ceiling_{side}", new Vector3(armCenterX, WallHeight, z), new Vector3(armLength, WallThickness, CorridorWidth));

                // North/south edges of the arm need solid walls (nothing opens off them) —
                // the arm is only walkable along its own east-west axis, connecting the
                // main corridor to R13/R14.
                CreateBlock(crossGo.transform, $"Wall_{side}_North", new Vector3(armCenterX, WallHeight / 2f, z + CorridorWidth / 2f), new Vector3(armLength, WallHeight, WallThickness));
                CreateBlock(crossGo.transform, $"Wall_{side}_South", new Vector3(armCenterX, WallHeight / 2f, z - CorridorWidth / 2f), new Vector3(armLength, WallHeight, WallThickness));
            }
        }

        // ---------------------------------------------------------------- offices

        private static void BuildOffice(Transform parent, OfficeSpec spec)
        {
            Vector3 center = spec.IsCrossArm ? CrossArmRoomCenter(spec.Side) : RoomCenter(spec.Row, spec.Side);
            var roomGo = new GameObject(spec.Id);
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            BuildRoomShellWithDoor(roomGo.transform, spec.Side, RoomDepthX, RoomWidthZ);
            BuildOfficeFurniture(roomGo.transform, spec);
        }

        /// <summary>
        /// Floor, ceiling, back wall, two side walls, and a door-gap front wall (split
        /// into permanently-solid left/right pieces flanking a DoorWidth gap — same
        /// "split wall" convention Milestone 7 established for the procedural rooms, so
        /// a closed door actually blocks the whole opening instead of leaving it walkable).
        /// All positions are room-local (center of the room = origin) before LocalToWorld
        /// mirrors them for East-side rooms.
        /// </summary>
        private static void BuildRoomShellWithDoor(Transform room, Side side, float depthX, float widthZ)
        {
            Vector3 roomCenter = room.position;

            CreateBlockWorld(room, "Floor", roomCenter, new Vector3(depthX, WallThickness, widthZ));
            CreateBlockWorld(room, "Ceiling", roomCenter + Vector3.up * WallHeight, new Vector3(depthX, WallThickness, widthZ));

            // Back wall (opposite the door).
            Vector3 backWallLocal = new(-depthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(room, "Wall_Back", LocalToWorld(roomCenter, side, backWallLocal), new Vector3(WallThickness, WallHeight, widthZ));

            // Side walls (run the full depth of the room).
            Vector3 sideWallLocalNear = new(0f, WallHeight / 2f, widthZ / 2f);
            Vector3 sideWallLocalFar = new(0f, WallHeight / 2f, -widthZ / 2f);
            CreateBlockWorld(room, "Wall_SideA", LocalToWorld(roomCenter, side, sideWallLocalNear), new Vector3(depthX, WallHeight, WallThickness));
            CreateBlockWorld(room, "Wall_SideB", LocalToWorld(roomCenter, side, sideWallLocalFar), new Vector3(depthX, WallHeight, WallThickness));

            // Door-gap front wall: two solid pieces flanking a DoorWidth gap, centered on
            // the room's width.
            float sideWidth = (widthZ - DoorWidth) / 2f;
            float sideOffset = sideWidth / 2f + DoorWidth / 2f;
            Vector3 frontWallLocal = new(depthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(room, "Wall_Front_Left", LocalToWorld(roomCenter, side, frontWallLocal + new Vector3(0f, 0f, -sideOffset)), new Vector3(WallThickness, WallHeight, sideWidth));
            CreateBlockWorld(room, "Wall_Front_Right", LocalToWorld(roomCenter, side, frontWallLocal + new Vector3(0f, 0f, sideOffset)), new Vector3(WallThickness, WallHeight, sideWidth));

            Vector3 doorBoundaryWorld = LocalToWorld(roomCenter, side, new Vector3(depthX / 2f, 0f, 0f));
            PlaceDoor(room, doorBoundaryWorld, $"{room.name}_Door");
        }

        /// <summary>
        /// Same hinge+panel construction as <see cref="EndlessRooms.World.ProceduralLevelBuilder.PlaceDoor"/>'s
        /// East/West case (the connecting wall runs along Z here, same as it does between
        /// rooms placed side-by-side along a procedural corridor) — reused by hand since
        /// this level isn't built by that system.
        /// </summary>
        private static void PlaceDoor(Transform parent, Vector3 boundaryWorldPos, string doorName)
        {
            Vector3 hingePosition = boundaryWorldPos;
            hingePosition.z -= DoorWidth / 2f;

            var hinge = new GameObject(doorName);
            hinge.transform.SetParent(parent.parent, true);
            hinge.transform.position = hingePosition;

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            panel.transform.SetParent(hinge.transform, false);
            panel.transform.localPosition = new Vector3(0f, WallHeight / 2f, DoorWidth / 2f);
            panel.transform.localScale = new Vector3(WallThickness, WallHeight, DoorWidth);
            DebugColor.Apply(panel, DebugColor.Door);

            var door = hinge.AddComponent<Door>();
            door.Initialize(hinge.transform);
            // No explicit SaveId: hinge.name (doorName, e.g. "R01_Door") is already
            // unique per door, so the default name-based SaveId works as-is — same
            // reasoning as the secret room's door in Milestone8AssetBuilder.
        }

        private static void BuildOfficeFurniture(Transform room, OfficeSpec spec)
        {
            Vector3 roomCenter = room.position;

            if (spec.UseMeetingTable)
            {
                AddFurniture(room, "MeetingTable", LocalToWorld(roomCenter, spec.Side, new Vector3(-1.5f, 0.375f, 0f)), new Vector3(1.0f, 0.75f, 2.2f), hideable: false);
            }
            else
            {
                AddFurniture(room, "Desk", LocalToWorld(roomCenter, spec.Side, new Vector3(-1.7f, 0.375f, 0f)), new Vector3(1.0f, 0.75f, 1.4f), hideable: true);
            }

            AddFurniture(room, "Closet", LocalToWorld(roomCenter, spec.Side, new Vector3(-1.7f, 1.0f, -2.3f)), new Vector3(0.6f, 2.0f, 0.9f), hideable: true);

            if (spec.HasShelf)
            {
                AddFurniture(room, "Shelf", LocalToWorld(roomCenter, spec.Side, new Vector3(-2.15f, 0.65f, 2.3f)), new Vector3(0.4f, 1.3f, 1.2f), hideable: false);
            }
        }

        // ---------------------------------------------------------------- bathrooms

        private static void BuildBathroom(Transform parent, int row, Side side, string name)
        {
            Vector3 center = RoomCenter(row, side);
            var roomGo = new GameObject(name);
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            BuildRoomShellWithDoor(roomGo.transform, side, RoomDepthX, RoomWidthZ);

            AddFurniture(roomGo.transform, "Toilet", LocalToWorld(center, side, new Vector3(-2.0f, 0.4f, -2.0f)), new Vector3(0.6f, 0.8f, 0.6f), hideable: false);
            AddFurniture(roomGo.transform, "Sink", LocalToWorld(center, side, new Vector3(-2.0f, 0.45f, 2.0f)), new Vector3(0.6f, 0.9f, 0.6f), hideable: false);
        }

        // ---------------------------------------------------------------- courtyards

        private static void BuildCourtyard(Transform parent, int row, Side side)
        {
            Vector3 center = RoomCenter(row, side);
            var roomGo = new GameObject($"Courtyard_{side}");
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            // Open to sky: floor + walls, no ceiling, no door gap needed on the far side
            // (the whole corridor-facing wall is open — the courtyard is basically an
            // alcove off the corridor, not a separately-doored room).
            CreateBlockWorld(roomGo.transform, "Floor", center, new Vector3(RoomDepthX, WallThickness, RoomWidthZ));

            Vector3 backWallLocal = new(-RoomDepthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(roomGo.transform, "Wall_Back", LocalToWorld(center, side, backWallLocal), new Vector3(WallThickness, WallHeight, RoomWidthZ));
            CreateBlockWorld(roomGo.transform, "Wall_SideA", LocalToWorld(center, side, new Vector3(0f, WallHeight / 2f, RoomWidthZ / 2f)), new Vector3(RoomDepthX, WallHeight, WallThickness));
            CreateBlockWorld(roomGo.transform, "Wall_SideB", LocalToWorld(center, side, new Vector3(0f, WallHeight / 2f, -RoomWidthZ / 2f)), new Vector3(RoomDepthX, WallHeight, WallThickness));

            AddFurniture(roomGo.transform, "PlanterPlaceholder", LocalToWorld(center, side, new Vector3(-1.8f, 0.3f, 0f)), new Vector3(0.8f, 0.6f, 0.8f), hideable: false);
        }

        // ---------------------------------------------------------------- shared helpers

        private static void AddFurniture(Transform parent, string name, Vector3 worldPosition, Vector3 size, bool hideable)
        {
            GameObject furniture = CreateBlockWorld(parent, name, worldPosition, size);
            if (hideable)
            {
                furniture.AddComponent<HidingSpot>();
            }
        }

        private static GameObject CreateBlockWorld(Transform parent, string name, Vector3 worldPosition, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, true);
            block.transform.position = worldPosition;
            block.transform.localScale = scale;
            return block;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = scale;
            return block;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = System.Linq.Enumerable.ToList(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
