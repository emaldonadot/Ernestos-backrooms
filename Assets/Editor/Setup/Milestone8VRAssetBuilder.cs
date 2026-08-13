using System.Linq;
using EndlessRooms.Core;
using EndlessRooms.Player;
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
    /// Quest-testable counterpart to <see cref="Milestone8AssetBuilder"/>'s secret-room
    /// scene: the exact same content (procedural level with Storage/OfficeCluster
    /// variety and the guaranteed Atrium landmark, the Attendant, the disguised secret
    /// room + field notes) but with Milestone 6's VR rig substituted for the PC player,
    /// same substitution Milestone 6 itself made for the puzzle test scene. Needed
    /// because none of the M7/M8 content previously existed in a VR-compatible scene —
    /// the Atrium/ramp fixes could only be verified on PC until this existed. Reuses
    /// the other builders' internal methods rather than duplicating their content.
    /// </summary>
    public static class Milestone8VRAssetBuilder
    {
        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone8_SecretRoomVRTestScene.unity";

        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            InputActionReference interactAction = Milestone6AssetBuilder.LoadInteractActionReference();

            new GameObject("GameBootstrap").AddComponent<Core.GameBootstrap>();

            GameObject levelGo = Milestone8AssetBuilder.BuildLevelBuilder();

            var attendantConfig = Milestone7AssetBuilder.LoadOrCreateAttendantConfig();

            Milestone6AssetBuilder.BuildVrRig(levelGo, interactAction, out InteractionCaster interactionCaster, out GameObject rigGo);
            if (rigGo == null)
            {
                Debug.LogError("[Milestone8VRAssetBuilder] VR rig failed to build — aborting.");
                return;
            }

            Milestone7AssetBuilder.BuildAttendant(levelGo, attendantConfig, rigGo.transform);

            var secretRoomRoot = new GameObject("SecretRoomRoot").transform;
            Milestone8AssetBuilder.BuildSecretRoom(rigGo.transform.position, secretRoomRoot);

            var placerGo = new GameObject("SecretRoomPlacer");
            var placer = placerGo.AddComponent<SecretRoomPlacer>();
            var placerSo = new SerializedObject(placer);
            placerSo.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            placerSo.FindProperty("_secretRoomRoot").objectReferenceValue = secretRoomRoot;
            placerSo.ApplyModifiedPropertiesWithoutUndo();

            Milestone6AssetBuilder.BuildWorldSpaceInteractionPromptUi(interactionCaster);
            BuildWorldSpaceFieldNoteUi(interactAction);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone8VRAssetBuilder] Built and saved '{ScenePath}'.");
        }

        /// <summary>
        /// Same FieldNoteUI script as PC, just parented to the head-tracked camera as a
        /// world-space canvas instead of a screen-space overlay — mirrors Milestone 6's
        /// BuildWorldSpaceInteractionPromptUi pattern exactly.
        /// </summary>
        private static void BuildWorldSpaceFieldNoteUi(InputActionReference dismissAction)
        {
            Camera headCamera = Object.FindAnyObjectByType<Camera>();

            var canvasGo = new GameObject("FieldNoteCanvas_WorldSpace");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(600f, 300f);

            if (headCamera != null)
            {
                canvasGo.transform.SetParent(headCamera.transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 0f, 1.2f);
                canvasGo.transform.localRotation = Quaternion.identity;
            }

            canvasGo.transform.localScale = Vector3.one * 0.001f;

            var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.92f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelRoot.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.fontSize = 18;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.sizeDelta = Vector2.zero;

            panelRoot.SetActive(false);

            var noteUi = canvasGo.AddComponent<FieldNoteUI>();
            var so = new SerializedObject(noteUi);
            so.FindProperty("_fragmentText").objectReferenceValue = text;
            so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("_dismissAction").objectReferenceValue = dismissAction;
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

        /// <summary>
        /// One-off standalone APK build of just this scene, for sideloading onto a Quest
        /// directly (outside the normal Build-and-Run-over-USB flow) — e.g. when USB
        /// debugging authorization is being finicky, a plain installable file is a
        /// useful fallback. Builds/ is gitignored (*.apk is too, but the folder itself
        /// isn't tracked either) since this is a generated artifact, not source.
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/Build M8 VR Test Scene APK")]
        public static void BuildApk()
        {
            const string outputPath = "Builds/TheEndlessRooms_M8_VR.apk";
            string outputDir = System.IO.Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !System.IO.Directory.Exists(outputDir))
            {
                System.IO.Directory.CreateDirectory(outputDir);
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[Milestone8VRAssetBuilder] Build succeeded: '{summary.outputPath}' ({summary.totalSize} bytes).");
            }
            else
            {
                Debug.LogError($"[Milestone8VRAssetBuilder] Build {summary.result}: {summary.totalErrors} error(s). See the full log above for details.");
            }
        }
    }
}
