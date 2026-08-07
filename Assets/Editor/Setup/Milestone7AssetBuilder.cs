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
            BuildInteractionPromptUi(interactionCaster);

            _ = cameraShake;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone7AssetBuilder] Built and saved '{ScenePath}'.");
        }

        private static AttendantConfig LoadOrCreateAttendantConfig()
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
            };
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

                var hidingSpot = spotGo.AddComponent<HidingSpot>();
                var so = new SerializedObject(hidingSpot);
                so.FindProperty("_saveId").stringValue = $"HidingSpot_{i}";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildAttendant(GameObject levelGo, AttendantConfig config, Transform playerTransform)
        {
            var attendantGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            attendantGo.name = "TheAttendant";
            attendantGo.transform.position = playerTransform.position + new Vector3(-4f, 0f, -4f);

            // The capsule primitive's own CapsuleCollider would double up with the
            // CharacterController's capsule; keep the mesh for visibility, remove the
            // primitive's collider so only the CharacterController drives movement/collision.
            Object.DestroyImmediate(attendantGo.GetComponent<CapsuleCollider>());

            var characterController = attendantGo.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.4f;
            characterController.center = new Vector3(0f, 1f, 0f);

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
