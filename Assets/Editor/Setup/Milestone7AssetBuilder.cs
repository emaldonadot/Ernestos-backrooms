using System.Linq;
using EndlessRooms.AI;
using EndlessRooms.Core;
using EndlessRooms.Map;
using EndlessRooms.Persistence;
using EndlessRooms.Player;
using EndlessRooms.Procedural;
using EndlessRooms.UI;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for Milestone 7's horror test scene: the same PC rig and
    /// procedural/puzzle/persistence setup as prior milestones, plus one Attendant and
    /// two hiding spots. Every SerializedObject write below uses exactly one instance
    /// per component.
    /// </summary>
    public static class Milestone7AssetBuilder
    {
        private const string DefinitionsFolder = "Assets/TheEndlessRooms/ScriptableObjects/RoomDefinitions";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";
        private const string MovementConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/PlayerMovementConfig.asset";
        private const string AttendantConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/AttendantConfig.asset";
        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone7_HorrorTestScene.unity";

        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            var actionRefs = LoadInputActionReferences();
            var movementConfig = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
            var attendantConfig = LoadOrCreateAttendantConfig();

            new GameObject("GameBootstrap").AddComponent<Core.GameBootstrap>();

            GameObject levelGo = BuildLevelBuilder();
            BuildPlayerAndSpawner(levelGo, movementConfig, actionRefs, out InteractionCaster interactionCaster, out GameObject playerGo, out CameraShakeEffect cameraShake);

            var mapBootstrapGo = new GameObject("MapBootstrap");
            var mapBootstrap = mapBootstrapGo.AddComponent<MapBootstrap>();
            var mapBootstrapSo = new SerializedObject(mapBootstrap);
            mapBootstrapSo.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            mapBootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            BuildSaveLoadAndRespawn(levelGo, playerGo, actionRefs);
            BuildHidingSpots(playerGo.transform);
            BuildAttendant(levelGo, attendantConfig, playerGo.transform);
            BuildPickupItem(playerGo.transform);
            BuildInteractionPromptUi(interactionCaster);
            BuildMapUi(actionRefs);

            _ = cameraShake;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone7AssetBuilder] Built and saved '{ScenePath}'.");
        }

        internal static AttendantConfig LoadOrCreateAttendantConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttendantConfig>(AttendantConfigPath);
            if (existing != null)
            {
                return existing;
            }

            var config = ScriptableObject.CreateInstance<AttendantConfig>();
            AssetDatabase.CreateAsset(config, AttendantConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private struct ActionRefs
        {
            public InputActionReference Move;
            public InputActionReference Look;
            public InputActionReference Sprint;
            public InputActionReference Crouch;
            public InputActionReference Interact;
            public InputActionReference QuickSave;
            public InputActionReference QuickLoad;
            public InputActionReference ToggleMap;
            public InputActionReference PanMap;
            public InputActionReference ZoomMap;
        }

        private static ActionRefs LoadInputActionReferences()
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath).OfType<InputActionReference>().ToList();

            InputActionReference Find(string actionName)
            {
                var reference = subAssets.FirstOrDefault(r => r.action.name == actionName);
                if (reference == null)
                {
                    Debug.LogError($"[Milestone7AssetBuilder] Could not find action '{actionName}' in '{InputActionsPath}'.");
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
                QuickSave = Find("QuickSave"),
                QuickLoad = Find("QuickLoad"),
                ToggleMap = Find("ToggleMap"),
                PanMap = Find("PanMap"),
                ZoomMap = Find("ZoomMap"),
            };
        }

        /// <summary>
        /// Adds the Field Log map UI (missed when this scene was first built — the
        /// data layer, MapBootstrap, was wired up, but the actual viewable canvas
        /// wasn't) to whichever scene is currently open in the Editor. A menu command
        /// rather than part of the headless BuildScene path specifically so it can be
        /// run live, from inside an already-open Editor session, without a competing
        /// batch process fighting over the same project.
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/Add Map UI To Current Scene")]
        public static void AddMapUiToCurrentScene()
        {
            var actionRefs = LoadInputActionReferences();
            BuildMapUi(actionRefs);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Milestone7AssetBuilder] Added Field Log map UI to the current scene. Save the scene (Ctrl+S) to keep it.");
        }

        /// <summary>
        /// Recolors the shared ModularRoomBase prefab's four walls yellow — a one-time,
        /// persistent change (unlike the per-scene menu commands above) since every
        /// room in every scene instantiates this same prefab. Requested during
        /// Milestone 7 testing: doors were visually indistinguishable from walls in
        /// the grey-box, making door behavior hard to evaluate. See
        /// <c>EndlessRooms.World.DebugColor</c> for the rest of the color scheme
        /// (doors brown, Attendant red, hiding spots blue, pickups green).
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/Recolor Shared Wall Prefab (One-Time)")]
        public static void RecolorSharedWallPrefab()
        {
            const string prefabPath = "Assets/TheEndlessRooms/Prefabs/ModularRoomBase.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

            foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
            {
                GameObject wall = contents.GetComponent<RoomInstance>().GetWall(direction);
                if (wall != null)
                {
                    DebugColor.Apply(wall, DebugColor.Wall);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log("[Milestone7AssetBuilder] Recolored ModularRoomBase's walls yellow.");
        }

        /// <summary>
        /// Splits each of ModularRoomBase's four full-width wall panels into a
        /// door-width center "gap" segment (the one <see cref="RoomInstance.OpenWall"/>
        /// still toggles — same <see cref="RoomInstance"/> serialized reference, just
        /// resized, so no C# API change needed) plus two permanently-solid side
        /// segments that are never referenced by <see cref="RoomInstance"/> and so
        /// never get deactivated. Fixes a real gameplay bug reported during Milestone 7
        /// playtesting: <see cref="RoomInstance.OpenWall"/> was deactivating the
        /// *entire* 6m wall for any connection, not just a 2m door-sized gap, so every
        /// connected room pair had no interior wall at all — you could walk straight
        /// past a closed door anywhere along that wall, bypassing it (and The
        /// Attendant, and hiding, and any tension the level design was supposed to
        /// create) entirely. A one-time, persistent prefab change, like the recolor
        /// above — every room in every scene instantiates this same prefab.
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/Split Walls Into Door-Sized Gaps (One-Time)")]
        public static void SplitWallsIntoDoorSizedGaps()
        {
            const string prefabPath = "Assets/TheEndlessRooms/Prefabs/ModularRoomBase.prefab";
            const float doorWidth = 2f;
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            var roomInstance = contents.GetComponent<RoomInstance>();

            foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
            {
                GameObject gapWall = roomInstance.GetWall(direction);
                if (gapWall == null)
                {
                    continue;
                }

                Transform gapTransform = gapWall.transform;
                Vector3 originalScale = gapTransform.localScale;
                Vector3 originalPosition = gapTransform.localPosition;
                bool runsAlongX = direction is Direction.North or Direction.South;
                float span = runsAlongX ? originalScale.x : originalScale.z;
                float sideWidth = (span - doorWidth) / 2f;

                gapTransform.localScale = runsAlongX
                    ? new Vector3(doorWidth, originalScale.y, originalScale.z)
                    : new Vector3(originalScale.x, originalScale.y, doorWidth);
                gapWall.name = $"Wall_{direction}_Gap";

                float sideOffset = sideWidth / 2f + doorWidth / 2f;
                CreateWallSidePiece(gapWall, $"Wall_{direction}_Left", originalPosition, originalScale, runsAlongX, sideWidth, -sideOffset);
                CreateWallSidePiece(gapWall, $"Wall_{direction}_Right", originalPosition, originalScale, runsAlongX, sideWidth, sideOffset);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log("[Milestone7AssetBuilder] Split all four walls into door-sized gap + permanently-solid side pieces.");
        }

        private static void CreateWallSidePiece(GameObject referenceWall, string name, Vector3 originalPosition, Vector3 originalScale, bool runsAlongX, float sideWidth, float offset)
        {
            GameObject piece = Object.Instantiate(referenceWall, referenceWall.transform.parent);
            piece.name = name;

            Vector3 position = originalPosition;
            Vector3 scale;
            if (runsAlongX)
            {
                position.x += offset;
                scale = new Vector3(sideWidth, originalScale.y, originalScale.z);
            }
            else
            {
                position.z += offset;
                scale = new Vector3(originalScale.x, originalScale.y, sideWidth);
            }

            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
        }

        [MenuItem("Tools/The Endless Rooms/Fix Attendant CharacterController In Current Scene")]
        public static void FixAttendantCharacterControllerInCurrentScene()
        {
            GameObject attendantGo = GameObject.Find("TheAttendant");
            if (attendantGo == null)
            {
                Debug.LogError("[Milestone7AssetBuilder] Could not find 'TheAttendant' in the current scene.");
                return;
            }

            var characterController = attendantGo.GetComponent<CharacterController>();
            if (characterController == null)
            {
                Debug.LogError("[Milestone7AssetBuilder] 'TheAttendant' has no CharacterController.");
                return;
            }

            characterController.height = 1.8f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Milestone7AssetBuilder] Fixed TheAttendant's CharacterController (height 1.8, center Y 0.9). Save the scene (Ctrl+S) to keep it.");
        }

        private static void BuildMapUi(ActionRefs actionRefs)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var canvasGo = new GameObject("MapCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var mapRoot = new GameObject("MapRoot");
            mapRoot.transform.SetParent(canvasGo.transform, false);
            var mapRootRect = mapRoot.AddComponent<RectTransform>();
            mapRootRect.anchorMin = new Vector2(0.1f, 0.1f);
            mapRootRect.anchorMax = new Vector2(0.9f, 0.9f);
            mapRootRect.sizeDelta = Vector2.zero;

            var background = mapRoot.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.75f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(mapRoot.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = new Vector2(0.7f, 1f);
            viewportRect.sizeDelta = Vector2.zero;

            var content = new GameObject("MapContent", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;

            var fieldLogViewGo = new GameObject("FieldLogView");
            fieldLogViewGo.transform.SetParent(canvasGo.transform, false);
            var fieldLogView = fieldLogViewGo.AddComponent<FieldLogView>();
            var viewSo = new SerializedObject(fieldLogView);
            viewSo.FindProperty("_mapRoot").objectReferenceValue = mapRoot;
            viewSo.FindProperty("_content").objectReferenceValue = contentRect;
            viewSo.FindProperty("_toggleMapAction").objectReferenceValue = actionRefs.ToggleMap;
            viewSo.FindProperty("_panMapAction").objectReferenceValue = actionRefs.PanMap;
            viewSo.FindProperty("_zoomMapAction").objectReferenceValue = actionRefs.ZoomMap;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            BuildMarkerPanel(mapRoot);

            mapRoot.SetActive(false);
        }

        private static void BuildMarkerPanel(GameObject mapRoot)
        {
            var panelGo = new GameObject("MarkerPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(mapRoot.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.72f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.sizeDelta = Vector2.zero;

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childForceExpandHeight = false;

            var typeButtons = new Button[System.Enum.GetValues(typeof(FieldMarkType)).Length];
            for (int i = 0; i < typeButtons.Length; i++)
            {
                var typeName = ((FieldMarkType)i).ToString();
                typeButtons[i] = CreateButton(panelGo.transform, $"Type_{typeName}", typeName);
            }

            var noteInputGo = new GameObject("NoteInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            noteInputGo.transform.SetParent(panelGo.transform, false);
            noteInputGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 24f);
            var noteInput = noteInputGo.GetComponent<InputField>();
            var noteTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            noteTextGo.transform.SetParent(noteInputGo.transform, false);
            var noteText = noteTextGo.GetComponent<Text>();
            noteText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            noteText.color = Color.black;
            StretchFull(noteTextGo.GetComponent<RectTransform>());
            noteInput.textComponent = noteText;

            Button addButton = CreateButton(panelGo.transform, "AddButton", "Add Marker Here");

            var marksListRoot = new GameObject("MarksList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            marksListRoot.transform.SetParent(panelGo.transform, false);

            GameObject rowTemplate = CreateMarkRowTemplate(marksListRoot.transform);

            var panelComponent = panelGo.AddComponent<FieldMarkerPanel>();
            var panelSo = new SerializedObject(panelComponent);
            var typeButtonsProp = panelSo.FindProperty("_typeButtons");
            typeButtonsProp.arraySize = typeButtons.Length;
            for (int i = 0; i < typeButtons.Length; i++)
            {
                typeButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = typeButtons[i];
            }

            panelSo.FindProperty("_noteInput").objectReferenceValue = noteInput;
            panelSo.FindProperty("_addButton").objectReferenceValue = addButton;
            panelSo.FindProperty("_marksListRoot").objectReferenceValue = marksListRoot.transform;
            panelSo.FindProperty("_markRowTemplate").objectReferenceValue = rowTemplate;
            panelSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateMarkRowTemplate(Transform parent)
        {
            var row = new GameObject("MarkRowTemplate", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(row.transform, false);
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.color = Color.white;
            label.fontSize = 12;

            CreateButton(row.transform, "RemoveButton", "X");

            return row;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            buttonGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 22f);
            buttonGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 12;
            StretchFull(textGo.GetComponent<RectTransform>());

            return buttonGo.GetComponent<Button>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        private static GameObject BuildLevelBuilder()
        {
            var entry = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Standard.asset");
            var exit = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Exit.asset");
            var corridor = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Corridor.asset");
            var junction = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Junction.asset");
            var deadEnd = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/DeadEnd.asset");

            var levelGo = new GameObject("ProceduralLevel");
            var builder = levelGo.AddComponent<ProceduralLevelBuilder>();
            var so = new SerializedObject(builder);
            so.FindProperty("_seed").intValue = 7070;
            so.FindProperty("_roomCount").intValue = 12;
            so.FindProperty("_entryDefinition").objectReferenceValue = entry;
            so.FindProperty("_exitDefinition").objectReferenceValue = exit;

            SerializedProperty fillers = so.FindProperty("_fillerDefinitions");
            var fillerDefinitions = new[] { entry, corridor, junction, deadEnd };
            fillers.arraySize = fillerDefinitions.Length;
            for (int i = 0; i < fillerDefinitions.Length; i++)
            {
                fillers.GetArrayElementAtIndex(i).objectReferenceValue = fillerDefinitions[i];
            }

            so.FindProperty("_buildOnStart").boolValue = false;

            var doorMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TheEndlessRooms/Art/Materials/Door_Office.mat");
            if (doorMaterial != null)
            {
                so.FindProperty("_doorMaterial").objectReferenceValue = doorMaterial;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return levelGo;
        }

        private static void BuildPlayerAndSpawner(GameObject levelGo, PlayerMovementConfig config, ActionRefs actionRefs, out InteractionCaster interactionCaster, out GameObject playerGo, out CameraShakeEffect cameraShake)
        {
            playerGo = new GameObject("Player") { tag = "Player" };

            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = config.StandingHeight;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, config.StandingHeight / 2f, 0f);

            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerGo.transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);

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

            var spawnerGo = new GameObject("LevelPlayerSpawner");
            var spawner = spawnerGo.AddComponent<LevelPlayerSpawner>();
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            spawnerSo.FindProperty("_player").objectReferenceValue = playerGo.transform;
            spawnerSo.FindProperty("_playerCharacterController").objectReferenceValue = characterController;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSaveLoadAndRespawn(GameObject levelGo, GameObject playerGo, ActionRefs actionRefs)
        {
            var saveGo = new GameObject("SaveService");
            var saveService = saveGo.AddComponent<SaveService>();
            var saveSo = new SerializedObject(saveService);
            saveSo.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            saveSo.FindProperty("_player").objectReferenceValue = playerGo.transform;
            saveSo.FindProperty("_playerCharacterController").objectReferenceValue = playerGo.GetComponent<CharacterController>();
            saveSo.ApplyModifiedPropertiesWithoutUndo();

            var controller = saveGo.AddComponent<SaveLoadController>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_saveService").objectReferenceValue = saveService;
            controllerSo.FindProperty("_quickSaveAction").objectReferenceValue = actionRefs.QuickSave;
            controllerSo.FindProperty("_quickLoadAction").objectReferenceValue = actionRefs.QuickLoad;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            var respawnGo = new GameObject("RespawnController");
            var respawn = respawnGo.AddComponent<RespawnController>();
            var respawnSo = new SerializedObject(respawn);
            respawnSo.FindProperty("_saveService").objectReferenceValue = saveService;
            respawnSo.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            respawnSo.FindProperty("_player").objectReferenceValue = playerGo.transform;
            respawnSo.FindProperty("_playerCharacterController").objectReferenceValue = playerGo.GetComponent<CharacterController>();
            respawnSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHidingSpots(Transform playerTransform)
        {
            for (int i = 0; i < 2; i++)
            {
                var spotGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spotGo.name = $"HidingSpot_{i}";
                spotGo.transform.position = playerTransform.position + new Vector3(2f + i * 1.5f, 0.5f, -2f);
                spotGo.transform.localScale = new Vector3(1f, 1f, 1f);

                DebugColor.Apply(spotGo, DebugColor.HidingSpot);

                var hidingSpot = spotGo.AddComponent<HidingSpot>();
                var so = new SerializedObject(hidingSpot);
                so.FindProperty("_saveId").stringValue = $"HidingSpot_{i}";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildPickupItem(Transform playerTransform)
        {
            var pickupGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickupGo.name = "TestPickup";
            pickupGo.transform.position = playerTransform.position + new Vector3(1f, 0.5f, 1f);
            pickupGo.transform.localScale = Vector3.one * 0.4f;
            pickupGo.GetComponent<Collider>().isTrigger = true;
            DebugColor.Apply(pickupGo, DebugColor.Pickup);

            var pickup = pickupGo.AddComponent<PickupTestItem>();
            var so = new SerializedObject(pickup);
            so.FindProperty("_itemName").stringValue = "Rusted Key";
            so.FindProperty("_saveId").stringValue = "TestPickup_Rusted_Key";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void BuildAttendant(GameObject levelGo, AttendantConfig config, Transform playerTransform)
        {
            var attendantGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            attendantGo.name = "TheAttendant";
            attendantGo.transform.position = playerTransform.position + new Vector3(-4f, 0f, -4f);
            DebugColor.Apply(attendantGo, DebugColor.Attendant);

            // The capsule primitive's own CapsuleCollider would double up with the
            // CharacterController's capsule; keep the mesh for visibility, remove the
            // primitive's collider so only the CharacterController drives movement/collision.
            Object.DestroyImmediate(attendantGo.GetComponent<CapsuleCollider>());

            var characterController = attendantGo.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.4f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            var eyes = new GameObject("Eyes").transform;
            eyes.SetParent(attendantGo.transform, false);
            eyes.localPosition = new Vector3(0f, 1.6f, 0f);

            var audioSource = attendantGo.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;

            var controller = attendantGo.AddComponent<AttendantController>();
            var so = new SerializedObject(controller);
            so.FindProperty("_config").objectReferenceValue = config;
            so.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            so.FindProperty("_eyes").objectReferenceValue = eyes;
            so.FindProperty("_stateAudioSource").objectReferenceValue = audioSource;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

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
