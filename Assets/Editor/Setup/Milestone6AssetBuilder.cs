using System.Linq;
using EndlessRooms.Core;
using EndlessRooms.Player;
using EndlessRooms.Procedural;
using EndlessRooms.UI;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for Milestone 6's Quest test scene: the same
    /// ProceduralLevelBuilder/PuzzleGateController setup as the PC scenes, with the XR
    /// Interaction Toolkit's "Starter Assets" rig (imported by
    /// <see cref="Milestone6Bootstrap.ImportXriStarterAssets"/>) substituted for the PC
    /// player, pruned to only the locomotion the user asked for (continuous move +
    /// smooth turn — see docs/features/milestone-6-vr-platform-support.md). Every
    /// SerializedObject write below uses exactly one instance per component.
    /// </summary>
    public static class Milestone6AssetBuilder
    {
        private const string DefinitionsFolder = "Assets/TheEndlessRooms/ScriptableObjects/RoomDefinitions";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";
        private const string RigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone6_QuestTestScene.unity";
        private const int SwitchCount = 3;

        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            InputActionReference interactAction = LoadInteractActionReference();

            new GameObject("GameBootstrap").AddComponent<Core.GameBootstrap>();

            GameObject levelGo = BuildLevelBuilder();
            PuzzleGateController puzzleGate = BuildPuzzleGate(levelGo);

            BuildVrRig(levelGo, interactAction, out InteractionCaster interactionCaster, out GameObject _);
            BuildWorldSpaceInteractionPromptUi(interactionCaster);
            BuildWorldSpaceLevelCompleteUi();

            _ = puzzleGate;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone6AssetBuilder] Built and saved '{ScenePath}'.");
        }

        internal static InputActionReference LoadInteractActionReference()
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath).OfType<InputActionReference>().ToList();
            InputActionReference reference = subAssets.FirstOrDefault(r => r.action.name == "Interact");
            if (reference == null)
            {
                Debug.LogError($"[Milestone6AssetBuilder] Could not find action 'Interact' in '{InputActionsPath}'.");
            }

            return reference;
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
            so.FindProperty("_seed").intValue = 6060;
            so.FindProperty("_roomCount").intValue = 10;
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

        private static PuzzleGateController BuildPuzzleGate(GameObject levelGo)
        {
            var gateGo = new GameObject("PuzzleGate");
            var switches = new PuzzleSwitch[SwitchCount];

            for (int i = 0; i < SwitchCount; i++)
            {
                var switchGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                switchGo.name = $"PuzzleSwitch_{i}";
                switchGo.transform.SetParent(gateGo.transform, false);
                switchGo.transform.localPosition = new Vector3(i * 1.2f - 1.2f, 1f, 3f);
                switchGo.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

                var puzzleSwitch = switchGo.AddComponent<PuzzleSwitch>();
                var switchSo = new SerializedObject(puzzleSwitch);
                switchSo.FindProperty("_switchIndex").intValue = i;
                switchSo.ApplyModifiedPropertiesWithoutUndo();

                switches[i] = puzzleSwitch;
            }

            var controller = gateGo.AddComponent<PuzzleGateController>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            SerializedProperty switchesProp = controllerSo.FindProperty("_switches");
            switchesProp.arraySize = switches.Length;
            for (int i = 0; i < switches.Length; i++)
            {
                switchesProp.GetArrayElementAtIndex(i).objectReferenceValue = switches[i];
            }

            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        /// <summary>
        /// Instantiates XRI's Starter Assets rig and prunes it to exactly the locomotion
        /// scope this milestone asked for: continuous move + smooth turn (the user's
        /// explicit choice), gravity so the CharacterController still respects floors,
        /// and nothing else — teleport, climb, grab-move, and jump are all out of scope
        /// (see the design doc's Scope section) and are disabled rather than removed, so
        /// re-enabling any of them later for a comfort-options menu is a checkbox, not
        /// new code. Interaction reuses <see cref="InteractionCaster"/> unchanged: the
        /// right controller's own transform becomes its ray origin override, and the
        /// existing "Interact" input action (which just gained a controller-trigger
        /// binding, see TheEndlessRooms.inputactions) drives it exactly like PC's E key.
        /// </summary>
        internal static void BuildVrRig(GameObject levelGo, InputActionReference interactAction, out InteractionCaster interactionCaster, out GameObject rigGo)
        {
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (rigPrefab == null)
            {
                Debug.LogError($"[Milestone6AssetBuilder] Could not load rig prefab at '{RigPrefabPath}'. Was Milestone6Bootstrap.ImportXriStarterAssets run?");
                interactionCaster = null;
                rigGo = null;
                return;
            }

            rigGo = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rigGo.name = "VRPlayer";

            Transform locomotion = rigGo.transform.Find("Locomotion");
            DisableComponents(locomotion.Find("Turn").GetComponent<SnapTurnProvider>());
            DisableComponents(locomotion.Find("Teleportation").GetComponent<TeleportationProvider>());
            DisableComponents(locomotion.Find("Climb").GetComponentsInChildren<Component>().OfType<ClimbProvider>().ToArray());
            DisableComponents(locomotion.Find("Grab Move").GetComponents<GrabMoveProvider>());
            DisableComponents(locomotion.Find("Grab Move").GetComponent<TwoHandedGrabMoveProvider>());
            DisableComponents(locomotion.Find("Jump").GetComponents<Component>().OfType<Behaviour>().ToArray());
            // ContinuousTurnProvider (on "Turn") and DynamicMoveProvider (on "Move") stay
            // enabled — they're the sample's default and match continuous move + smooth
            // turn already. Gravity stays enabled so the rig doesn't float off the floor.

            Transform rightController = rigGo.transform.Find("Camera Offset/Right Controller");
            DisableGameObject(rightController.Find("Poke Interactor"));
            DisableGameObject(rightController.Find("Near-Far Interactor"));
            DisableGameObject(rightController.Find("Teleport Interactor"));
            Transform leftController = rigGo.transform.Find("Camera Offset/Left Controller");
            DisableGameObject(leftController.Find("Poke Interactor"));
            DisableGameObject(leftController.Find("Near-Far Interactor"));
            DisableGameObject(leftController.Find("Teleport Interactor"));

            interactionCaster = rightController.gameObject.AddComponent<InteractionCaster>();
            var casterSo = new SerializedObject(interactionCaster);
            casterSo.FindProperty("_rayOriginOverride").objectReferenceValue = rightController;
            casterSo.FindProperty("_interactAction").objectReferenceValue = interactAction;
            casterSo.ApplyModifiedPropertiesWithoutUndo();

            var spawnerGo = new GameObject("VRLevelPlayerSpawner");
            var spawner = spawnerGo.AddComponent<LevelPlayerSpawner>();
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            spawnerSo.FindProperty("_player").objectReferenceValue = rigGo.transform;
            spawnerSo.FindProperty("_playerCharacterController").objectReferenceValue = rigGo.GetComponent<CharacterController>();
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DisableComponents(params Behaviour[] behaviours)
        {
            foreach (Behaviour behaviour in behaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private static void DisableGameObject(Transform t)
        {
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// World-space UI parented to the head-tracked camera at a fixed local offset, so
        /// it's always in view regardless of where the player is standing/looking — the
        /// same role PC's screen-space overlay canvas plays. The underlying
        /// InteractionPromptUI/LevelCompleteUI scripts are unchanged from PC; only the
        /// canvas render mode, scale, and parent differ.
        /// </summary>
        internal static void BuildWorldSpaceInteractionPromptUi(InteractionCaster interactionCaster)
        {
            Camera headCamera = Object.FindAnyObjectByType<Camera>();

            var canvasGo = new GameObject("PromptCanvas_WorldSpace");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 40f);

            if (headCamera != null)
            {
                canvasGo.transform.SetParent(headCamera.transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, -0.2f, 1.2f);
                canvasGo.transform.localRotation = Quaternion.identity;
            }

            canvasGo.transform.localScale = Vector3.one * 0.001f;

            var promptRoot = new GameObject("PromptRoot");
            promptRoot.transform.SetParent(canvasGo.transform, false);
            var promptRootRect = promptRoot.AddComponent<RectTransform>();
            promptRootRect.anchorMin = Vector2.zero;
            promptRootRect.anchorMax = Vector2.one;
            promptRootRect.sizeDelta = Vector2.zero;

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

        private static void BuildWorldSpaceLevelCompleteUi()
        {
            Camera headCamera = Object.FindAnyObjectByType<Camera>();

            var canvasGo = new GameObject("LevelCompleteCanvas_WorldSpace");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(600f, 300f);

            if (headCamera != null)
            {
                canvasGo.transform.SetParent(headCamera.transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 0f, 1.5f);
                canvasGo.transform.localRotation = Quaternion.identity;
            }

            canvasGo.transform.localScale = Vector3.one * 0.001f;

            var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelRoot.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "You Found The Exit";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 36;
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
