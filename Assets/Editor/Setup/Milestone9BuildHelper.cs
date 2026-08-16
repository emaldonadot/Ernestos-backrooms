using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// One-off standalone builds of the real playable game (menu -> Level 1), for
    /// testing outside the Editor — same pattern as Milestone8AssetBuilder.BuildPcStandalone
    /// and Milestone8VRAssetBuilder.BuildApk, just pointed at the current menu+level
    /// scenes instead of the M8 secret-room scene. Builds/ is gitignored (a generated
    /// artifact, not source) — these get attached to GitHub releases instead.
    /// </summary>
    public static class Milestone9BuildHelper
    {
        private const string PcMenuScenePath = "Assets/TheEndlessRooms/Scenes/MainMenu_PC.unity";
        private const string QuestMenuScenePath = "Assets/TheEndlessRooms/Scenes/MainMenu_Quest.unity";
        private const string Level1ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone9_Level1TestScene.unity";

        [MenuItem("Tools/The Endless Rooms/Build/PC Standalone (Menu + Level 1)")]
        public static void BuildPcStandalone()
        {
            const string outputPath = "Builds/TheEndlessRooms_L1_PC/TheEndlessRooms.x86_64";
            Build(new BuildPlayerOptions
            {
                scenes = new[] { PcMenuScenePath, Level1ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None,
            }, "PC");
        }

        [MenuItem("Tools/The Endless Rooms/Build/Quest APK (Menu + Level 1)")]
        public static void BuildQuestApk()
        {
            const string outputPath = "Builds/TheEndlessRooms_L1_VR.apk";
            Build(new BuildPlayerOptions
            {
                scenes = new[] { QuestMenuScenePath, Level1ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            }, "Quest");
        }

        private static void Build(BuildPlayerOptions options, string label)
        {
            string outputDir = Path.GetDirectoryName(options.locationPathName);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Milestone9BuildHelper] {label} build succeeded: '{summary.outputPath}' ({summary.totalSize} bytes).");
            }
            else
            {
                Debug.LogError($"[Milestone9BuildHelper] {label} build {summary.result}: {summary.totalErrors} error(s). See the full log above for details.");
            }
        }
    }
}
