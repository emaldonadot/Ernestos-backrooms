using System.Linq;
using EndlessRooms.Map;
using EndlessRooms.Player;
using EndlessRooms.UI;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for Milestone 3: adds a <see cref="RoomTrigger"/> to the shared
    /// modular room prefab, and builds the Map-enabled test scene (procedural level,
    /// player, spawner, Field Log UI). One-time setup utility, kept under Assets/Editor
    /// so it never ships in a build.
    /// </summary>
    public static class Milestone3AssetBuilder
    {
        private const float CellSize = 6f;
        private const float WallHeight = 3f;
        private const string RoomPrefabPath = "Assets/TheEndlessRooms/Prefabs/ModularRoomBase.prefab";
        private const string DefinitionsFolder = "Assets/TheEndlessRooms/ScriptableObjects/RoomDefinitions";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";
        private const string MovementConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/PlayerMovementConfig.asset";
        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone3_MapTestScene.unity";

        public static void AddRoomTriggerToPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(RoomPrefabPath);

            if (prefabRoot.GetComponent<RoomTrigger>() == null)
            {
                var collider = prefabRoot.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.center = new Vector3(0f, WallHeight / 2f, 0f);
                collider.size = new Vector3(CellSize * 0.9f, WallHeight * 0.9f, CellSize * 0.9f);

                prefabRoot.AddComponent<RoomTrigger>();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, RoomPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log($"[Milestone3AssetBuilder] Added RoomTrigger to '{RoomPrefabPath}'.");
        }

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

            new GameObject("GameBootstrap").AddComponent<Core.GameBootstrap>();

            GameObject levelGo = BuildLevelBuilder();
            BuildPlayerAndSpawner(levelGo, movementConfig, actionRefs, out InteractionCaster interactionCaster, out GameObject playerGo);

            var mapBootstrapGo = new GameObject("MapBootstrap");
            var mapBootstrap = mapBootstrapGo.AddComponent<MapBootstrap>();
            new SerializedObject(mapBootstrap).FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            new SerializedObject(mapBootstrap).ApplyModifiedPropertiesWithoutUndo();

            BuildInteractionPromptUi(interactionCaster);
            BuildMapUi(actionRefs);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone3AssetBuilder] Built and saved '{ScenePath}'.");
        }

        private struct ActionRefs
        {
            public InputActionReference Move;
            public InputActionReference Look;
            public InputActionReference Sprint;
            public InputActionReference Crouch;
            public InputActionReference Interact;
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
                    Debug.LogError($"[Milestone3AssetBuilder] Could not find action '{actionName}' in '{InputActionsPath}'.");
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
                ToggleMap = Find("ToggleMap"),
                PanMap = Find("PanMap"),
                ZoomMap = Find("ZoomMap"),
            };
        }

        private static GameObject BuildLevelBuilder()
        {
            var entry = AssetDatabase.LoadAssetAtPath<Procedural.RoomDefinition>($"{DefinitionsFolder}/Standard.asset");
            var exit = AssetDatabase.LoadAssetAtPath<Procedural.RoomDefinition>($"{DefinitionsFolder}/Exit.asset");
            var corridor = AssetDatabase.LoadAssetAtPath<Procedural.RoomDefinition>($"{DefinitionsFolder}/Corridor.asset");
            var junction = AssetDatabase.LoadAssetAtPath<Procedural.RoomDefinition>($"{DefinitionsFolder}/Junction.asset");
            var deadEnd = AssetDatabase.LoadAssetAtPath<Procedural.RoomDefinition>($"{DefinitionsFolder}/DeadEnd.asset");

            var levelGo = new GameObject("ProceduralLevel");
            var builder = levelGo.AddComponent<ProceduralLevelBuilder>();
            var so = new SerializedObject(builder);
            so.FindProperty("_seed").intValue = 2026;
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

            // LevelPlayerSpawner drives the build explicitly so it can position the
            // player at the entry room afterward — Start() order between it and this
            // component isn't guaranteed otherwise.
            so.FindProperty("_buildOnStart").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            return levelGo;
        }

        private static void BuildPlayerAndSpawner(GameObject levelGo, PlayerMovementConfig config, ActionRefs actionRefs, out InteractionCaster interactionCaster, out GameObject playerGo)
        {
            playerGo = new GameObject("Player") { tag = "Player" };

            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = config.StandingHeight;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, config.StandingHeight / 2f, 0f);

            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerGo.transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(cameraPivot, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();

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

        private static void BuildMapUi(ActionRefs actionRefs)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

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

            Button removeButton = CreateButton(row.transform, "RemoveButton", "X");

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
