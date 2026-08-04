using System.Linq;
using EndlessRooms.Core;
using EndlessRooms.Player;
using EndlessRooms.UI;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Builds the Milestone 1 grey-box test scene end to end (player, room, door,
    /// pickup, interaction prompt) and wires every serialized reference, so the
    /// scene described in docs/UNITY_SETUP.md exists without manual click-through.
    /// One-time setup utility, kept under Assets/Editor so it never ships in a build.
    /// Invoke via `-executeMethod EndlessRooms.EditorSetup.Milestone1SceneBuilder.BuildScene`.
    /// </summary>
    public static class Milestone1SceneBuilder
    {
        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone1_TestScene.unity";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";
        private const string MovementConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/PlayerMovementConfig.asset";

        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            EnsureFolder("Assets/TheEndlessRooms/ScriptableObjects");
            EnsureFolder("Assets/TheEndlessRooms/Scenes");

            PlayerMovementConfig movementConfig = LoadOrCreateMovementConfig();
            var actionRefs = LoadInputActionReferences();

            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();

            BuildPlayer(movementConfig, actionRefs, out InteractionCaster interactionCaster);
            BuildGreyBoxRoom(out Door door, out PickupTestItem pickup);
            BuildInteractionPromptUi(interactionCaster);

            _ = door;
            _ = pickup;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone1SceneBuilder] Built and saved '{ScenePath}'.");
        }

        private static PlayerMovementConfig LoadOrCreateMovementConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
                AssetDatabase.CreateAsset(config, MovementConfigPath);
                AssetDatabase.SaveAssets();
            }

            return config;
        }

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
                    Debug.LogError($"[Milestone1SceneBuilder] Could not find action '{actionName}' in '{InputActionsPath}'.");
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

        private static void BuildPlayer(PlayerMovementConfig config, ActionRefs actionRefs, out InteractionCaster interactionCaster)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, -2f);

            var characterController = player.AddComponent<CharacterController>();
            characterController.height = config.StandingHeight;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, config.StandingHeight / 2f, 0f);

            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(player.transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(cameraPivot, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();

            var playerController = player.AddComponent<PlayerController>();
            var controllerSo = new SerializedObject(playerController);
            controllerSo.FindProperty("_config").objectReferenceValue = config;
            controllerSo.FindProperty("_moveAction").objectReferenceValue = actionRefs.Move;
            controllerSo.FindProperty("_lookAction").objectReferenceValue = actionRefs.Look;
            controllerSo.FindProperty("_sprintAction").objectReferenceValue = actionRefs.Sprint;
            controllerSo.FindProperty("_crouchAction").objectReferenceValue = actionRefs.Crouch;
            controllerSo.FindProperty("_cameraPivot").objectReferenceValue = cameraPivot;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            interactionCaster = player.AddComponent<InteractionCaster>();
            var casterSo = new SerializedObject(interactionCaster);
            casterSo.FindProperty("_viewCamera").objectReferenceValue = camera;
            casterSo.FindProperty("_interactAction").objectReferenceValue = actionRefs.Interact;
            casterSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildGreyBoxRoom(out Door door, out PickupTestItem pickup)
        {
            const float roomSize = 6f;
            const float wallHeight = 3f;
            const float wallThickness = 0.2f;

            var room = new GameObject("GreyBoxRoom");

            CreateBlock(room.transform, "Floor", new Vector3(0f, 0f, 0f), new Vector3(roomSize, wallThickness, roomSize));
            CreateBlock(room.transform, "Ceiling", new Vector3(0f, wallHeight, 0f), new Vector3(roomSize, wallThickness, roomSize));
            CreateBlock(room.transform, "Wall_North", new Vector3(0f, wallHeight / 2f, roomSize / 2f), new Vector3(roomSize, wallHeight, wallThickness));
            CreateBlock(room.transform, "Wall_West", new Vector3(-roomSize / 2f, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, roomSize));
            CreateBlock(room.transform, "Wall_East", new Vector3(roomSize / 2f, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, roomSize));

            // South wall has a doorway gap in the middle for the Door prefab-equivalent.
            CreateBlock(room.transform, "Wall_South_Left", new Vector3(-roomSize / 4f - 0.5f, wallHeight / 2f, -roomSize / 2f), new Vector3(roomSize / 2f - 1f, wallHeight, wallThickness));
            CreateBlock(room.transform, "Wall_South_Right", new Vector3(roomSize / 4f + 0.5f, wallHeight / 2f, -roomSize / 2f), new Vector3(roomSize / 2f - 1f, wallHeight, wallThickness));

            var hinge = new GameObject("DoorHinge");
            hinge.transform.SetParent(room.transform, false);
            hinge.transform.position = new Vector3(-1f, 0f, -roomSize / 2f);

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            panel.transform.SetParent(hinge.transform, false);
            panel.transform.localPosition = new Vector3(1f, wallHeight / 2f, 0f);
            panel.transform.localScale = new Vector3(2f, wallHeight, wallThickness);

            door = hinge.AddComponent<Door>();
            var doorSo = new SerializedObject(door);
            doorSo.FindProperty("_hinge").objectReferenceValue = hinge.transform;
            doorSo.ApplyModifiedPropertiesWithoutUndo();

            var pickupGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickupGo.name = "TestPickup";
            pickupGo.transform.SetParent(room.transform, false);
            pickupGo.transform.position = new Vector3(1.5f, 0.5f, 1.5f);
            pickupGo.transform.localScale = Vector3.one * 0.4f;
            var pickupCollider = pickupGo.GetComponent<Collider>();
            pickupCollider.isTrigger = true;

            pickup = pickupGo.AddComponent<PickupTestItem>();
        }

        private static void CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = scale;
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
