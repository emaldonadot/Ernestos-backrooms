using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// One-time headless project bootstrap for Milestone 1: installs the packages the
    /// committed scripts/asmdefs depend on. Safe to re-run (Package Manager no-ops if a
    /// package is already present). Invoked via:
    /// `-executeMethod EndlessRooms.EditorSetup.Milestone1Bootstrap.InstallPackages`.
    /// This class is a setup utility, not gameplay code — kept under Assets/Editor so it
    /// never ships in a build.
    /// </summary>
    public static class Milestone1Bootstrap
    {
        private static readonly string[] RequiredPackages =
        {
            "com.unity.inputsystem",
            "com.unity.render-pipelines.universal",
            "com.unity.test-framework",
        };

        public static void InstallPackages()
        {
            foreach (string packageId in RequiredPackages)
            {
                AddPackageAndWait(packageId);
            }

            Debug.Log("[Milestone1Bootstrap] Package installation complete.");
        }

        /// <summary>
        /// Creates (or reuses) a Universal Render Pipeline asset via the same "Create"
        /// menu path a user would click, so its default renderer data is wired up
        /// correctly, then assigns it as the project's default pipeline.
        /// </summary>
        public static void ConfigureRenderPipeline()
        {
            const string settingsFolder = "Assets/TheEndlessRooms/Settings";
            string assetPath = settingsFolder + "/TheEndlessRoomsURP.asset";

            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (existing == null)
            {
                var before = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset").ToHashSet();

                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(settingsFolder);
                EditorApplication.ExecuteMenuItem("Assets/Create/Rendering/URP Asset (with Universal Renderer)");
                AssetDatabase.Refresh();

                var after = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
                string newGuid = after.FirstOrDefault(guid => !before.Contains(guid));

                if (newGuid == null)
                {
                    Debug.LogError("[Milestone1Bootstrap] URP asset creation menu item produced no new asset. Check the menu path is still valid for the installed URP version.");
                    return;
                }

                string createdPath = AssetDatabase.GUIDToAssetPath(newGuid);
                string error = AssetDatabase.MoveAsset(createdPath, assetPath);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"[Milestone1Bootstrap] Could not move generated URP asset from '{createdPath}' to '{assetPath}': {error}");
                    assetPath = createdPath;
                }

                existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            }

            if (existing == null)
            {
                Debug.LogError("[Milestone1Bootstrap] No Universal Render Pipeline asset available to assign.");
                return;
            }

            GraphicsSettings.defaultRenderPipeline = existing;
            QualitySettings.renderPipeline = existing;
            AssetDatabase.SaveAssets();
            Debug.Log($"[Milestone1Bootstrap] Assigned '{assetPath}' as the default render pipeline.");
        }

        private static void AddPackageAndWait(string packageId)
        {
            Debug.Log($"[Milestone1Bootstrap] Adding package '{packageId}'...");
            AddRequest request = Client.Add(packageId);

            while (!request.IsCompleted)
            {
                Thread.Sleep(500);
            }

            if (request.Status == StatusCode.Success)
            {
                Debug.Log($"[Milestone1Bootstrap] Installed '{request.Result.packageId}'.");
            }
            else if (request.Status >= StatusCode.Failure)
            {
                Debug.LogError($"[Milestone1Bootstrap] Failed to add '{packageId}': {request.Error?.message}");
            }
        }
    }
}
