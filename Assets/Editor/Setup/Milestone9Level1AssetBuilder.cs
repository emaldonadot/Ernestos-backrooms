using System.Linq;
using EndlessRooms.AI;
using EndlessRooms.Core;
using EndlessRooms.Player;
using EndlessRooms.UI;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Side = EndlessRooms.World.Level1Layout.Side;
using OfficeSpec = EndlessRooms.World.Level1Layout.OfficeSpec;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for Milestone 9's Level 1: a fixed, hand-authored cross-spine
    /// office building (not procedural — see docs/features/milestone-9-playable-office-levels.md
    /// and <see cref="Level1Layout"/> for the shared position math). 14 offices at
    /// 5m(deep) x 6m(wide) x 3m(tall), a 6m-wide corridor, 2 paired bathrooms, 2 open-air
    /// courtyards, a PC player, the Attendant (unchanged from Milestone 7, pathing via
    /// <see cref="Level1RoomGraphProvider"/>'s synthetic graph), the exit, and a first
    /// investigate-clue-key-locked door chain proving the mechanic end to end (more
    /// content is a follow-up, not a blocker for the first playable pass).
    /// </summary>
    public static class Milestone9Level1AssetBuilder
    {
        private const float WallHeight = 3f;
        private const float WallThickness = 0.2f;
        private const float DoorWidth = 2f;

        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone9_Level1TestScene.unity";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";
        private const string MovementConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/PlayerMovementConfig.asset";
        private const string ItemsFolder = "Assets/TheEndlessRooms/ScriptableObjects/Items";

        [MenuItem("Tools/The Endless Rooms/M9 Level 1/Build Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            new GameObject("GameBootstrap").AddComponent<Core.GameBootstrap>();

            var levelRoot = new GameObject("Level1_HorrorOffice").transform;

            BuildMainCorridor(levelRoot);
            BuildCrossCorridorArms(levelRoot);

            foreach (OfficeSpec office in Level1Layout.Offices)
            {
                BuildOffice(levelRoot, office);
            }

            BuildCourtyard(levelRoot, Level1Layout.CourtyardRow, Side.West);
            BuildCourtyard(levelRoot, Level1Layout.CourtyardRow, Side.East);
            BuildBathroom(levelRoot, Level1Layout.BathroomRow, Side.West, "Bathroom_Women");
            BuildBathroom(levelRoot, Level1Layout.BathroomRow, Side.East, "Bathroom_Men");

            GameObject levelGraphGo = BuildLevelGraph();
            FlickeringLight[] warningLights = BuildCorridorWarningLights(levelRoot);

            ActionRefs actionRefs = LoadInputActionReferences();
            var movementConfig = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
            GameObject playerGo = BuildPlayer(movementConfig, actionRefs, out InteractionCaster interactionCaster, out CameraShakeEffect cameraShake);
            playerGo.transform.position = new Vector3(0f, 1f, 1.5f);
            _ = cameraShake;

            var attendantConfig = Milestone7AssetBuilder.LoadOrCreateAttendantConfig();
            Milestone7AssetBuilder.BuildAttendant(levelGraphGo, attendantConfig, playerGo.transform);
            BuildAttendantAppearanceCycle(warningLights);

            BuildExitPoint();
            BuildFirstKeyLockChain(playerGo.transform);
            BuildJumpScares();

            BuildInteractionPromptUi(interactionCaster);
            BuildLevelCompleteUi();
            BuildGameOverUi();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone9Level1AssetBuilder] Built and saved '{ScenePath}' — {Level1Layout.Offices.Length} offices, 2 bathrooms, 2 courtyards, player, Attendant, exit, one key/lock chain, jump scares.");
        }

        // ---------------------------------------------------------------- corridor shell

        private static void BuildMainCorridor(Transform parent)
        {
            var corridorGo = new GameObject("MainCorridor");
            corridorGo.transform.SetParent(parent, false);

            float totalLength = Level1Layout.TotalRows * Level1Layout.RowSpacing;
            float centerZ = totalLength / 2f;

            CreateBlock(corridorGo.transform, "Floor", new Vector3(0f, 0f, centerZ), new Vector3(Level1Layout.CorridorWidth, WallThickness, totalLength));
            CreateBlock(corridorGo.transform, "Ceiling", new Vector3(0f, WallHeight, centerZ), new Vector3(Level1Layout.CorridorWidth, WallThickness, totalLength));

            CreateBlock(corridorGo.transform, "Wall_SouthEnd", new Vector3(0f, WallHeight / 2f, -WallThickness / 2f), new Vector3(Level1Layout.CorridorWidth, WallHeight, WallThickness));
            CreateBlock(corridorGo.transform, "Wall_NorthEnd", new Vector3(0f, WallHeight / 2f, totalLength + WallThickness / 2f), new Vector3(Level1Layout.CorridorWidth, WallHeight, WallThickness));
        }

        private static void BuildCrossCorridorArms(Transform parent)
        {
            var crossGo = new GameObject("CrossCorridorArms");
            crossGo.transform.SetParent(parent, false);

            float z = Level1Layout.RowCenterZ(Level1Layout.CrossCorridorRow);
            float mainEdge = Level1Layout.CorridorWidth / 2f;
            float armLength = Level1Layout.CorridorWidth;

            foreach (Side side in new[] { Side.West, Side.East })
            {
                float sign = side == Side.West ? -1f : 1f;
                float armCenterX = (mainEdge + armLength / 2f) * sign;

                CreateBlock(crossGo.transform, $"Floor_{side}", new Vector3(armCenterX, 0f, z), new Vector3(armLength, WallThickness, Level1Layout.CorridorWidth));
                CreateBlock(crossGo.transform, $"Ceiling_{side}", new Vector3(armCenterX, WallHeight, z), new Vector3(armLength, WallThickness, Level1Layout.CorridorWidth));

                CreateBlock(crossGo.transform, $"Wall_{side}_North", new Vector3(armCenterX, WallHeight / 2f, z + Level1Layout.CorridorWidth / 2f), new Vector3(armLength, WallHeight, WallThickness));
                CreateBlock(crossGo.transform, $"Wall_{side}_South", new Vector3(armCenterX, WallHeight / 2f, z - Level1Layout.CorridorWidth / 2f), new Vector3(armLength, WallHeight, WallThickness));
            }
        }

        // ---------------------------------------------------------------- offices

        private static void BuildOffice(Transform parent, OfficeSpec spec)
        {
            Vector3 center = spec.IsCrossArm ? Level1Layout.CrossArmRoomCenter(spec.Side) : Level1Layout.RoomCenter(spec.Row, spec.Side);
            var roomGo = new GameObject(spec.Id);
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            BuildRoomShellWithDoor(roomGo.transform, spec.Side, Level1Layout.RoomDepthX, Level1Layout.RoomWidthZ, out Door door);
            BuildOfficeFurniture(roomGo.transform, spec);

            if (spec.Id == "R11")
            {
                _pendingLockedDoor = door;
            }
        }

        private static Door _pendingLockedDoor;

        /// <summary>
        /// Floor, ceiling, back wall, two side walls, and a door-gap front wall (split
        /// into permanently-solid left/right pieces flanking a DoorWidth gap — same
        /// "split wall" convention Milestone 7 established for the procedural rooms, so
        /// a closed door actually blocks the whole opening instead of leaving it walkable).
        /// All positions are room-local (center of the room = origin) before LocalToWorld
        /// mirrors them for East-side rooms.
        /// </summary>
        private static void BuildRoomShellWithDoor(Transform room, Side side, float depthX, float widthZ, out Door door)
        {
            Vector3 roomCenter = room.position;

            CreateBlockWorld(room, "Floor", roomCenter, new Vector3(depthX, WallThickness, widthZ));
            CreateBlockWorld(room, "Ceiling", roomCenter + Vector3.up * WallHeight, new Vector3(depthX, WallThickness, widthZ));

            Vector3 backWallLocal = new(-depthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(room, "Wall_Back", Level1Layout.LocalToWorld(roomCenter, side, backWallLocal), new Vector3(WallThickness, WallHeight, widthZ));

            Vector3 sideWallLocalNear = new(0f, WallHeight / 2f, widthZ / 2f);
            Vector3 sideWallLocalFar = new(0f, WallHeight / 2f, -widthZ / 2f);
            CreateBlockWorld(room, "Wall_SideA", Level1Layout.LocalToWorld(roomCenter, side, sideWallLocalNear), new Vector3(depthX, WallHeight, WallThickness));
            CreateBlockWorld(room, "Wall_SideB", Level1Layout.LocalToWorld(roomCenter, side, sideWallLocalFar), new Vector3(depthX, WallHeight, WallThickness));

            float sideWidth = (widthZ - DoorWidth) / 2f;
            float sideOffset = sideWidth / 2f + DoorWidth / 2f;
            Vector3 frontWallLocal = new(depthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(room, "Wall_Front_Left", Level1Layout.LocalToWorld(roomCenter, side, frontWallLocal + new Vector3(0f, 0f, -sideOffset)), new Vector3(WallThickness, WallHeight, sideWidth));
            CreateBlockWorld(room, "Wall_Front_Right", Level1Layout.LocalToWorld(roomCenter, side, frontWallLocal + new Vector3(0f, 0f, sideOffset)), new Vector3(WallThickness, WallHeight, sideWidth));

            Vector3 doorBoundaryWorld = Level1Layout.LocalToWorld(roomCenter, side, new Vector3(depthX / 2f, 0f, 0f));
            door = PlaceDoor(room, doorBoundaryWorld, $"{room.name}_Door");
        }

        /// <summary>
        /// Same hinge+panel construction as <see cref="ProceduralLevelBuilder.PlaceDoor"/>'s
        /// East/West case (the connecting wall runs along Z here, same as it does between
        /// rooms placed side-by-side along a procedural corridor) — reused by hand since
        /// this level isn't built by that system.
        /// </summary>
        private static Door PlaceDoor(Transform parent, Vector3 boundaryWorldPos, string doorName)
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
            return door;
        }

        private static void BuildOfficeFurniture(Transform room, OfficeSpec spec)
        {
            Vector3 roomCenter = room.position;

            if (spec.UseMeetingTable)
            {
                AddFurniture(room, "MeetingTable", Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(-1.5f, 0.375f, 0f)), new Vector3(1.0f, 0.75f, 2.2f), hideable: false);
            }
            else
            {
                AddFurniture(room, "Desk", Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(-1.7f, 0.375f, 0f)), new Vector3(1.0f, 0.75f, 1.4f), hideable: true);
            }

            AddFurniture(room, "Closet", Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(-1.7f, 1.0f, -2.3f)), new Vector3(0.6f, 2.0f, 0.9f), hideable: true);

            if (spec.HasShelf)
            {
                AddFurniture(room, "Shelf", Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(-2.15f, 0.65f, 2.3f)), new Vector3(0.4f, 1.3f, 1.2f), hideable: false);
            }
        }

        // ---------------------------------------------------------------- bathrooms

        private static void BuildBathroom(Transform parent, int row, Side side, string name)
        {
            Vector3 center = Level1Layout.RoomCenter(row, side);
            var roomGo = new GameObject(name);
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            BuildRoomShellWithDoor(roomGo.transform, side, Level1Layout.RoomDepthX, Level1Layout.RoomWidthZ, out _);

            AddFurniture(roomGo.transform, "Toilet", Level1Layout.LocalToWorld(center, side, new Vector3(-2.0f, 0.4f, -2.0f)), new Vector3(0.6f, 0.8f, 0.6f), hideable: false);
            AddFurniture(roomGo.transform, "Sink", Level1Layout.LocalToWorld(center, side, new Vector3(-2.0f, 0.45f, 2.0f)), new Vector3(0.6f, 0.9f, 0.6f), hideable: false);
        }

        // ---------------------------------------------------------------- courtyards

        private static void BuildCourtyard(Transform parent, int row, Side side)
        {
            Vector3 center = Level1Layout.RoomCenter(row, side);
            var roomGo = new GameObject($"Courtyard_{side}");
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            CreateBlockWorld(roomGo.transform, "Floor", center, new Vector3(Level1Layout.RoomDepthX, WallThickness, Level1Layout.RoomWidthZ));

            Vector3 backWallLocal = new(-Level1Layout.RoomDepthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(roomGo.transform, "Wall_Back", Level1Layout.LocalToWorld(center, side, backWallLocal), new Vector3(WallThickness, WallHeight, Level1Layout.RoomWidthZ));
            CreateBlockWorld(roomGo.transform, "Wall_SideA", Level1Layout.LocalToWorld(center, side, new Vector3(0f, WallHeight / 2f, Level1Layout.RoomWidthZ / 2f)), new Vector3(Level1Layout.RoomDepthX, WallHeight, WallThickness));
            CreateBlockWorld(roomGo.transform, "Wall_SideB", Level1Layout.LocalToWorld(center, side, new Vector3(0f, WallHeight / 2f, -Level1Layout.RoomWidthZ / 2f)), new Vector3(Level1Layout.RoomDepthX, WallHeight, WallThickness));

            AddFurniture(roomGo.transform, "PlanterPlaceholder", Level1Layout.LocalToWorld(center, side, new Vector3(-1.8f, 0.3f, 0f)), new Vector3(0.8f, 0.6f, 0.8f), hideable: false);
        }

        // ---------------------------------------------------------------- level graph / Attendant

        private static GameObject BuildLevelGraph()
        {
            var levelGraphGo = new GameObject("Level1Graph");
            var builder = levelGraphGo.AddComponent<ProceduralLevelBuilder>();
            var so = new SerializedObject(builder);
            so.FindProperty("_cellSize").floatValue = Level1Layout.GraphCellSize;
            so.FindProperty("_buildOnStart").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var providerGo = new GameObject("Level1RoomGraphProvider");
            var provider = providerGo.AddComponent<Level1RoomGraphProvider>();
            var providerSo = new SerializedObject(provider);
            providerSo.FindProperty("_levelBuilder").objectReferenceValue = builder;
            providerSo.ApplyModifiedPropertiesWithoutUndo();

            return levelGraphGo;
        }

        /// <summary>
        /// One ceiling light per main-corridor row, disabled by default (steady, normal
        /// light) — FlickeringLight only turns on as AttendantAppearanceController's
        /// warning cue, not as an always-on ambient effect the way Milestone 8's rooms
        /// use it.
        /// </summary>
        private static FlickeringLight[] BuildCorridorWarningLights(Transform parent)
        {
            var lightsGo = new GameObject("CorridorWarningLights");
            lightsGo.transform.SetParent(parent, false);

            var lights = new FlickeringLight[Level1Layout.TotalRows];
            for (int row = 1; row <= Level1Layout.TotalRows; row++)
            {
                var lightGo = new GameObject($"WarningLight_Row{row}");
                lightGo.transform.SetParent(lightsGo.transform, false);
                lightGo.transform.position = Level1Layout.CorridorCellCenter(row) + new Vector3(0f, WallHeight - 0.15f, 0f);
                lightGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 8f;
                light.intensity = 1.3f;
                light.color = new Color(0.85f, 0.9f, 1f);

                // Always enabled: FlickeringLight itself flickers gently as ambient
                // baseline atmosphere all the time now, and AttendantAppearanceController
                // only intensifies (not toggles) it during Warning/Hunting via SetIntensified.
                lightGo.AddComponent<FlickeringLight>();
                lights[row - 1] = lightGo.GetComponent<FlickeringLight>();
            }

            return lights;
        }

        /// <summary>
        /// Wires AttendantAppearanceController onto a dedicated manager object — the
        /// Attendant GameObject itself (built by Milestone7AssetBuilder.BuildAttendant,
        /// found by name here since that method doesn't hand back a reference) starts
        /// inactive; the appearance controller decides when it's actually present.
        /// </summary>
        private static void BuildAttendantAppearanceCycle(FlickeringLight[] warningLights)
        {
            GameObject attendantGo = GameObject.Find("TheAttendant");
            if (attendantGo == null)
            {
                Debug.LogError("[Milestone9Level1AssetBuilder] Could not find 'TheAttendant' to wire the appearance cycle onto.");
                return;
            }

            var attendantController = attendantGo.GetComponent<AttendantController>();
            attendantGo.SetActive(false);

            var managerGo = new GameObject("AttendantAppearanceManager");
            var audioSource = managerGo.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;

            var appearance = managerGo.AddComponent<AttendantAppearanceController>();
            var so = new SerializedObject(appearance);
            so.FindProperty("_attendantGo").objectReferenceValue = attendantGo;
            so.FindProperty("_attendant").objectReferenceValue = attendantController;
            so.FindProperty("_warningAudioSource").objectReferenceValue = audioSource;

            SerializedProperty lightsProp = so.FindProperty("_warningLights");
            lightsProp.arraySize = warningLights.Length;
            for (int i = 0; i < warningLights.Length; i++)
            {
                lightsProp.GetArrayElementAtIndex(i).objectReferenceValue = warningLights[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- player

        private struct ActionRefs
        {
            public InputActionReference Move;
            public InputActionReference Look;
            public InputActionReference Sprint;
            public InputActionReference Crouch;
            public InputActionReference Interact;
        }

        private static ActionRefs LoadInputActionReferences()
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath).OfType<InputActionReference>().ToList();

            InputActionReference Find(string actionName)
            {
                var reference = subAssets.FirstOrDefault(r => r.action.name == actionName);
                if (reference == null)
                {
                    Debug.LogError($"[Milestone9Level1AssetBuilder] Could not find action '{actionName}' in '{InputActionsPath}'.");
                }

                return reference;
            }

            return new ActionRefs
            {
                Move = Find("Move"),
                Look = Find("Look"),
                Sprint = Find("Sprint"),
                Crouch = Find("Crouch"),
                Interact = Find("Interact"),
            };
        }

        private static GameObject BuildPlayer(PlayerMovementConfig config, ActionRefs actionRefs, out InteractionCaster interactionCaster, out CameraShakeEffect cameraShake)
        {
            var playerGo = new GameObject("Player") { tag = "Player" };

            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = config.StandingHeight;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, config.StandingHeight / 2f, 0f);

            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerGo.transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);

            // Milestone 9 fix: the earlier pass omitted this entirely (same gap found
            // and fixed in Milestone8AssetBuilder's PC rig) — without a CameraShakeEffect
            // component somewhere under the player, AttendantController.HandleCapture's
            // shake intensity has nothing to apply to, so getting caught feels like
            // nothing happened even once a real capture consequence exists.
            var shakeAnchor = new GameObject("CameraShakeAnchor").transform;
            shakeAnchor.SetParent(cameraPivot, false);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(shakeAnchor, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();

            cameraShake = shakeAnchor.gameObject.AddComponent<CameraShakeEffect>();
            var shakeSo = new SerializedObject(cameraShake);
            shakeSo.FindProperty("_shakeTarget").objectReferenceValue = cameraGo.transform;
            shakeSo.ApplyModifiedPropertiesWithoutUndo();

            var playerController = playerGo.AddComponent<PlayerController>();
            var controllerSo = new SerializedObject(playerController);
            controllerSo.FindProperty("_config").objectReferenceValue = config;
            controllerSo.FindProperty("_moveAction").objectReferenceValue = actionRefs.Move;
            controllerSo.FindProperty("_lookAction").objectReferenceValue = actionRefs.Look;
            controllerSo.FindProperty("_sprintAction").objectReferenceValue = actionRefs.Sprint;
            controllerSo.FindProperty("_crouchAction").objectReferenceValue = actionRefs.Crouch;
            controllerSo.FindProperty("_cameraPivot").objectReferenceValue = cameraPivot;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            interactionCaster = playerGo.AddComponent<InteractionCaster>();
            var casterSo = new SerializedObject(interactionCaster);
            casterSo.FindProperty("_viewCamera").objectReferenceValue = camera;
            casterSo.FindProperty("_interactAction").objectReferenceValue = actionRefs.Interact;
            casterSo.ApplyModifiedPropertiesWithoutUndo();

            playerGo.AddComponent<Inventory>();

            return playerGo;
        }

        // ---------------------------------------------------------------- exit

        private static void BuildExitPoint()
        {
            float z = Level1Layout.RowCenterZ(Level1Layout.TotalRows) + Level1Layout.RowSpacing / 2f - 1f;
            var exitGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            exitGo.name = "ExitPoint";
            exitGo.transform.position = new Vector3(0f, 1f, z);
            exitGo.transform.localScale = Vector3.one * 0.6f;
            exitGo.AddComponent<ExitPoint>();
        }

        // ---------------------------------------------------------------- first key/lock chain

        /// <summary>
        /// Proves the investigate-clue-key-locked-door mechanic end to end: a clue in R01
        /// mentions a master key, the key itself sits in R01, and R11's door (near the
        /// exit) requires it. Only one chain for this first pass — more rooms getting
        /// clues/keys/locks is straightforward follow-up content once this is confirmed
        /// to work, not a blocker.
        /// </summary>
        private static void BuildFirstKeyLockChain(Transform player)
        {
            GameObject r01 = GameObject.Find("R01");
            if (r01 == null)
            {
                Debug.LogError("[Milestone9Level1AssetBuilder] Could not find R01 to place the first key/clue in.");
                return;
            }

            EnsureFolder(ItemsFolder);
            string itemPath = $"{ItemsFolder}/MasterKey.asset";
            var masterKey = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(itemPath);
            if (masterKey == null)
            {
                masterKey = ScriptableObject.CreateInstance<InventoryItemDefinition>();
                AssetDatabase.CreateAsset(masterKey, itemPath);
            }

            masterKey.ItemId = "master_key";
            masterKey.DisplayName = "Master Key";
            masterKey.Description = "A worn brass key. Someone kept it in their desk.";
            EditorUtility.SetDirty(masterKey);
            AssetDatabase.SaveAssets();

            Vector3 keyPosition = r01.transform.position + new Vector3(-1.7f, 0.9f, 0f);
            var keyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyGo.name = "MasterKeyPickup";
            keyGo.transform.position = keyPosition;
            keyGo.transform.localScale = new Vector3(0.08f, 0.02f, 0.15f);
            DebugColor.Apply(keyGo, DebugColor.Pickup);
            var pickup = keyGo.AddComponent<InventoryPickup>();
            var pickupSo = new SerializedObject(pickup);
            pickupSo.FindProperty("_item").objectReferenceValue = masterKey;
            pickupSo.ApplyModifiedPropertiesWithoutUndo();

            var clueGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clueGo.name = "ClueNote_R01";
            clueGo.transform.position = r01.transform.position + new Vector3(-1.7f, 0.78f, -0.35f);
            clueGo.transform.localScale = new Vector3(0.22f, 0.03f, 0.28f);
            DebugColor.Apply(clueGo, DebugColor.Note);
            var note = clueGo.AddComponent<FieldNote>();
            var noteSo = new SerializedObject(note);
            noteSo.FindProperty("_promptLabel").stringValue = "Read Note";
            noteSo.FindProperty("_fragmentText").stringValue =
                "\"Left the master key in my desk again — third time this month. If R11's " +
                "stuck shut, that's where it'll be.\"";
            noteSo.ApplyModifiedPropertiesWithoutUndo();

            if (_pendingLockedDoor != null)
            {
                _pendingLockedDoor.SetRequiredItem(masterKey);
                _pendingLockedDoor.RestoreState(new Door.DoorState(isOpen: false, isLocked: true));
            }
            else
            {
                Debug.LogError("[Milestone9Level1AssetBuilder] R11's door wasn't captured — the key/lock chain has nothing to unlock.");
            }

            _ = player;
        }

        // ---------------------------------------------------------------- jump scares

        private static void BuildJumpScares()
        {
            AddJumpScare("R07");
            AddJumpScare("R09");
        }

        private static void AddJumpScare(string roomId)
        {
            GameObject room = GameObject.Find(roomId);
            if (room == null)
            {
                return;
            }

            var triggerGo = new GameObject($"JumpScare_{roomId}");
            triggerGo.transform.SetParent(room.transform, false);
            triggerGo.transform.localPosition = Vector3.zero;
            var collider = triggerGo.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(Level1Layout.RoomDepthX, WallHeight, Level1Layout.RoomWidthZ);
            triggerGo.AddComponent<AudioSource>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "ScareVisual";
            visual.transform.SetParent(triggerGo.transform, false);
            visual.transform.localPosition = new Vector3(1.5f, 1f, 0f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            DebugColor.Apply(visual, DebugColor.Attendant);

            var jumpScare = triggerGo.AddComponent<JumpScareTrigger>();
            var so = new SerializedObject(jumpScare);
            so.FindProperty("_scareVisual").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- UI

        private static void BuildInteractionPromptUi(InteractionCaster interactionCaster)
        {
            var canvasGo = new GameObject("PromptCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var promptRoot = new GameObject("PromptRoot");
            promptRoot.transform.SetParent(canvasGo.transform, false);
            var promptRootRect = promptRoot.AddComponent<RectTransform>();
            promptRootRect.anchorMin = new Vector2(0.5f, 0.15f);
            promptRootRect.anchorMax = new Vector2(0.5f, 0.15f);
            promptRootRect.sizeDelta = new Vector2(400f, 40f);

            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(promptRoot.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 24;
            text.color = Color.white;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            promptRoot.SetActive(false);

            var promptUi = canvasGo.AddComponent<InteractionPromptUI>();
            var promptSo = new SerializedObject(promptUi);
            promptSo.FindProperty("_interactionCaster").objectReferenceValue = interactionCaster;
            promptSo.FindProperty("_promptText").objectReferenceValue = text;
            promptSo.FindProperty("_promptRoot").objectReferenceValue = promptRoot;
            promptSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Same functional (not styled) screen-space panel Milestones 4-6 already established for GameEvents.LevelCompleted — reused as-is.</summary>
        private static void BuildLevelCompleteUi()
        {
            var canvasGo = new GameObject("LevelCompleteCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.4f);
            panelRect.anchorMax = new Vector2(0.7f, 0.6f);
            panelRect.sizeDelta = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelRoot.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "You Escaped The Building";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 28;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            panelRoot.SetActive(false);

            var levelCompleteUi = canvasGo.AddComponent<LevelCompleteUI>();
            var so = new SerializedObject(levelCompleteUi);
            so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Same functional panel shape as BuildLevelCompleteUi, driven by GameEvents.PlayerCaptured instead — the "lose" consequence that was previously missing entirely.</summary>
        private static void BuildGameOverUi()
        {
            var canvasGo = new GameObject("GameOverCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100; // always above PromptCanvas/LevelCompleteCanvas regardless of hierarchy order
            canvasGo.AddComponent<CanvasScaler>();

            var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.4f);
            panelRect.anchorMax = new Vector2(0.7f, 0.6f);
            panelRect.sizeDelta = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0.4f, 0f, 0f, 0.9f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelRoot.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "You Were Caught";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 28;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            panelRoot.SetActive(false);

            var gameOverUi = canvasGo.AddComponent<GameOverController>();
            var so2 = new SerializedObject(gameOverUi);
            so2.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            so2.ApplyModifiedPropertiesWithoutUndo();
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
