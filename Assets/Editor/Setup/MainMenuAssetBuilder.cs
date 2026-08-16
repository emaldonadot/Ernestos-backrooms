using System.Linq;
using EndlessRooms.Core;
using EndlessRooms.Player;
using EndlessRooms.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Builds the two main-menu scenes (PC screen-space, Quest world-space/VR rig) —
    /// both share the same small dark room and the same
    /// <see cref="LevelSelectEntry"/> props (see <see cref="BuildMenuRoomContent"/>),
    /// interacted with the exact same look-and-press-Interact flow as everything else in
    /// the game, so no new input plumbing or uGUI event system was needed to make this
    /// work on both platforms. Adding Level 2 later means adding one more entry to
    /// LevelCatalogBuilder — this file doesn't change.
    /// </summary>
    public static class MainMenuAssetBuilder
    {
        private const string PcScenePath = "Assets/TheEndlessRooms/Scenes/MainMenu_PC.unity";
        private const string QuestScenePath = "Assets/TheEndlessRooms/Scenes/MainMenu_Quest.unity";
        private const string MovementConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/PlayerMovementConfig.asset";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";

        private const float RoomSize = 6f;
        private const float WallHeight = 3f;
        private const float WallThickness = 0.2f;

        private struct ActionRefs
        {
            public InputActionReference Move;
            public InputActionReference Look;
            public InputActionReference Sprint;
            public InputActionReference Crouch;
            public InputActionReference Interact;
        }

        [MenuItem("Tools/The Endless Rooms/Main Menu/Build PC Scene")]
        public static void BuildPcScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            new GameObject("LevelProgressService").AddComponent<LevelProgressService>();
            LevelCatalog catalog = LevelCatalogBuilder.LoadOrCreateCatalog();

            var roomRoot = new GameObject("MenuRoom").transform;
            BuildRoomShell(roomRoot);
            BuildTitle(roomRoot);

            ActionRefs actionRefs = LoadInputActionReferences();
            GameObject playerGo = BuildPcPlayer(actionRefs, out InteractionCaster interactionCaster);
            playerGo.transform.position = new Vector3(0f, 1f, -RoomSize / 2f + 1.3f);

            BuildMenuRoomContent(roomRoot, catalog, isQuest: false);
            BuildInteractionPromptUi(interactionCaster);

            EditorSceneManager.SaveScene(scene, PcScenePath);
            InsertSceneAtFront(PcScenePath);

            Debug.Log($"[MainMenuAssetBuilder] Built and saved '{PcScenePath}' — {catalog.Levels.Length} level(s) listed.");
        }

        [MenuItem("Tools/The Endless Rooms/Main Menu/Build Quest Scene")]
        public static void BuildQuestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            new GameObject("LevelProgressService").AddComponent<LevelProgressService>();
            LevelCatalog catalog = LevelCatalogBuilder.LoadOrCreateCatalog();

            var roomRoot = new GameObject("MenuRoom").transform;
            BuildRoomShell(roomRoot);
            BuildTitle(roomRoot);

            ActionRefs actionRefs = LoadInputActionReferences();

            // BuildVrRig also wires a VRLevelPlayerSpawner tied to a ProceduralLevelBuilder,
            // which this static menu room doesn't have — safe (LevelPlayerSpawner just
            // logs and no-ops without one), but removed here since it's dead weight and
            // the log would be misleading.
            var throwawayLevelGo = new GameObject("ThrowawayLevelGo");
            Milestone6AssetBuilder.BuildVrRig(throwawayLevelGo, actionRefs.Interact, out InteractionCaster interactionCaster, out GameObject rigGo);
            Object.DestroyImmediate(throwawayLevelGo);
            GameObject spawner = GameObject.Find("VRLevelPlayerSpawner");
            if (spawner != null)
            {
                Object.DestroyImmediate(spawner);
            }

            if (rigGo != null)
            {
                rigGo.transform.position = new Vector3(0f, 0f, -RoomSize / 2f + 1.3f);
            }

            BuildMenuRoomContent(roomRoot, catalog, isQuest: true);
            Milestone6AssetBuilder.BuildWorldSpaceInteractionPromptUi(interactionCaster);

            EditorSceneManager.SaveScene(scene, QuestScenePath);
            InsertSceneAtFront(QuestScenePath);

            Debug.Log($"[MainMenuAssetBuilder] Built and saved '{QuestScenePath}' — {catalog.Levels.Length} level(s) listed.");
        }

        // ---------------------------------------------------------------- shared room

        private static void BuildRoomShell(Transform parent)
        {
            float floorY = WallThickness / 2f;
            Milestone9Level1AssetBuilder.CreateBlockWorld(parent, "Floor", new Vector3(0f, 0f, 0f), new Vector3(RoomSize, WallThickness, RoomSize));
            Milestone9Level1AssetBuilder.CreateBlockWorld(parent, "Ceiling", new Vector3(0f, WallHeight, 0f), new Vector3(RoomSize, WallThickness, RoomSize));
            Milestone9Level1AssetBuilder.CreateBlockWorld(parent, "Wall_Back", new Vector3(0f, WallHeight / 2f, RoomSize / 2f), new Vector3(RoomSize, WallHeight, WallThickness));
            Milestone9Level1AssetBuilder.CreateBlockWorld(parent, "Wall_Front", new Vector3(0f, WallHeight / 2f, -RoomSize / 2f), new Vector3(RoomSize, WallHeight, WallThickness));
            Milestone9Level1AssetBuilder.CreateBlockWorld(parent, "Wall_Left", new Vector3(-RoomSize / 2f, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, RoomSize));
            Milestone9Level1AssetBuilder.CreateBlockWorld(parent, "Wall_Right", new Vector3(RoomSize / 2f, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, RoomSize));

            var lightGo = new GameObject("RoomLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = new Vector3(0f, WallHeight - 0.2f, 0f);
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 1.2f;
            light.color = new Color(0.9f, 0.85f, 0.7f);
        }

        private static void BuildTitle(Transform parent)
        {
            var canvasGo = new GameObject("TitleCanvas");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.position = new Vector3(0f, 2.2f, RoomSize / 2f - 0.05f);
            canvasGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.01f;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500f, 100f);

            var textGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "THE ENDLESS ROOMS";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.85f, 0.8f, 0.65f);
            text.fontSize = 48;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        /// <summary>One raised, glowing pedestal per catalog entry, spaced along the room's width — shared by both scene builders so the level list is guaranteed identical on PC and Quest.</summary>
        private static void BuildMenuRoomContent(Transform parent, LevelCatalog catalog, bool isQuest)
        {
            LevelDefinition[] levels = catalog.Levels ?? System.Array.Empty<LevelDefinition>();
            float spacing = 1.8f;
            float startX = -(levels.Length - 1) * spacing / 2f;

            for (int i = 0; i < levels.Length; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null)
                {
                    continue;
                }

                float x = startX + i * spacing;
                Vector3 pedestalPos = new(x, 0f, 0.5f);
                BuildLevelPedestal(parent, level, catalog, pedestalPos, isQuest);
            }
        }

        private static void BuildLevelPedestal(Transform parent, LevelDefinition level, LevelCatalog catalog, Vector3 worldPosition, bool isQuest)
        {
            var pedestalGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestalGo.name = $"LevelEntry_{level.LevelId}";
            pedestalGo.transform.SetParent(parent, true);
            pedestalGo.transform.position = worldPosition + new Vector3(0f, 0.4f, 0f);
            pedestalGo.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
            pedestalGo.GetComponent<Renderer>().sharedMaterial = GetPedestalMaterial();

            var glowGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glowGo.name = "Glow";
            glowGo.transform.SetParent(pedestalGo.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            glowGo.transform.localScale = new Vector3(0.7f, 0.05f, 0.7f);
            Object.DestroyImmediate(glowGo.GetComponent<Collider>());
            glowGo.GetComponent<Renderer>().sharedMaterial = GetGlowMaterial();

            var entry = pedestalGo.AddComponent<LevelSelectEntry>();
            var entrySo = new SerializedObject(entry);
            entrySo.FindProperty("_level").objectReferenceValue = level;
            entrySo.FindProperty("_catalog").objectReferenceValue = catalog;
            entrySo.ApplyModifiedPropertiesWithoutUndo();

            var labelCanvasGo = new GameObject("StatusLabel");
            labelCanvasGo.transform.SetParent(pedestalGo.transform, false);
            labelCanvasGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            labelCanvasGo.transform.localRotation = isQuest ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            labelCanvasGo.transform.localScale = Vector3.one * 0.006f;

            var canvas = labelCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = labelCanvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 80f);

            var textGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(labelCanvasGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 28;
            text.color = Color.white;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var label = labelCanvasGo.AddComponent<LevelSelectLabel>();
            var labelSo = new SerializedObject(label);
            labelSo.FindProperty("_entry").objectReferenceValue = entry;
            labelSo.FindProperty("_statusText").objectReferenceValue = text;
            labelSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material _pedestalMaterial;
        private static Material _glowMaterial;

        private static Material GetPedestalMaterial()
        {
            const string path = "Assets/TheEndlessRooms/Art/Materials/MenuPedestal_Level1.mat";
            if (_pedestalMaterial != null)
            {
                return _pedestalMaterial;
            }

            _pedestalMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (_pedestalMaterial != null)
            {
                return _pedestalMaterial;
            }

            _pedestalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.1f, 0.1f, 0.11f) };
            _pedestalMaterial.SetFloat("_Smoothness", 0.4f);
            _pedestalMaterial.SetFloat("_Metallic", 0.3f);
            Milestone9Level1AssetBuilder.EnsureFolder("Assets/TheEndlessRooms/Art/Materials");
            AssetDatabase.CreateAsset(_pedestalMaterial, path);
            return _pedestalMaterial;
        }

        private static Material GetGlowMaterial()
        {
            const string path = "Assets/TheEndlessRooms/Art/Materials/MenuGlow_Level1.mat";
            if (_glowMaterial != null)
            {
                return _glowMaterial;
            }

            _glowMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (_glowMaterial != null)
            {
                return _glowMaterial;
            }

            _glowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.9f, 0.75f, 0.35f) };
            _glowMaterial.SetColor("_EmissionColor", new Color(0.9f, 0.75f, 0.35f) * 2f);
            _glowMaterial.EnableKeyword("_EMISSION");
            _glowMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Milestone9Level1AssetBuilder.EnsureFolder("Assets/TheEndlessRooms/Art/Materials");
            AssetDatabase.CreateAsset(_glowMaterial, path);
            return _glowMaterial;
        }

        // ---------------------------------------------------------------- PC player

        private static GameObject BuildPcPlayer(ActionRefs actionRefs, out InteractionCaster interactionCaster)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);

            var playerGo = new GameObject("Player") { tag = "Player" };
            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = config != null ? config.StandingHeight : 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, characterController.height / 2f, 0f);

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

            return playerGo;
        }

        private static void BuildInteractionPromptUi(InteractionCaster interactionCaster)
        {
            var canvasGo = new GameObject("PromptCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var promptRoot = new GameObject("PromptRoot", typeof(RectTransform));
            promptRoot.transform.SetParent(canvasGo.transform, false);
            var promptRootRect = promptRoot.GetComponent<RectTransform>();
            promptRootRect.anchorMin = new Vector2(0.5f, 0.15f);
            promptRootRect.anchorMax = new Vector2(0.5f, 0.15f);
            promptRootRect.sizeDelta = new Vector2(500f, 40f);

            var textGo = new GameObject("PromptText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(promptRoot.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 22;
            text.color = Color.white;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            promptRoot.SetActive(false);

            var promptUi = canvasGo.AddComponent<InteractionPromptUI>();
            var so = new SerializedObject(promptUi);
            so.FindProperty("_interactionCaster").objectReferenceValue = interactionCaster;
            so.FindProperty("_promptText").objectReferenceValue = text;
            so.FindProperty("_promptRoot").objectReferenceValue = promptRoot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- shared helpers

        private static ActionRefs LoadInputActionReferences()
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath).OfType<InputActionReference>().ToList();

            InputActionReference Find(string actionName)
            {
                var reference = subAssets.FirstOrDefault(r => r.action.name == actionName);
                if (reference == null)
                {
                    Debug.LogError($"[MainMenuAssetBuilder] Could not find action '{actionName}' in '{InputActionsPath}'.");
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

        private static void InsertSceneAtFront(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
