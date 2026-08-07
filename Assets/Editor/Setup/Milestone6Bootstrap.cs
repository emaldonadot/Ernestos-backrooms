using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless package installer for Milestone 6 (VR platform support). Same pattern
    /// as <see cref="Milestone1Bootstrap"/>: let the real Editor resolve compatible
    /// versions rather than guessing them. One-time setup utility, kept under
    /// Assets/Editor so it never ships in a build.
    /// </summary>
    public static class Milestone6Bootstrap
    {
        private static readonly string[] RequiredPackages =
        {
            "com.unity.xr.management",
            "com.unity.xr.openxr",
            "com.unity.xr.interaction.toolkit",
        };

        public static void InstallPackages()
        {
            foreach (string packageId in RequiredPackages)
            {
                AddPackageAndWait(packageId);
            }

            Debug.Log("[Milestone6Bootstrap] Package installation complete.");
        }

        private static void AddPackageAndWait(string packageId)
        {
            Debug.Log($"[Milestone6Bootstrap] Adding package '{packageId}'...");
            AddRequest request = Client.Add(packageId);

            while (!request.IsCompleted)
            {
                Thread.Sleep(500);
            }

            if (request.Status == StatusCode.Success)
            {
                Debug.Log($"[Milestone6Bootstrap] Installed '{request.Result.packageId}'.");
            }
            else if (request.Status >= StatusCode.Failure)
            {
                Debug.LogError($"[Milestone6Bootstrap] Failed to add '{packageId}': {request.Error?.message}");
            }
        }

        /// <summary>
        /// Player Settings + active build target for Android/Quest. Safe, well-documented
        /// scripting API only. XR Plug-in Management's OpenXR loader + Meta Quest Support
        /// feature group are a separate, much smaller manual step documented in
        /// docs/QUEST_TESTING.md — those settings are edited through internal-ish APIs that
        /// vary between OpenXR package versions and can't be visually confirmed from this
        /// headless environment, so getting them silently wrong would be worse than a
        /// one-time checkbox the user flips once in Project Settings.
        /// </summary>
        public static void ConfigurePlatform()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            Debug.Log("[Milestone6Bootstrap] Player Settings: IL2CPP, ARM64, min API 29.");

            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Debug.Log(switched
                ? "[Milestone6Bootstrap] Active build target switched to Android."
                : "[Milestone6Bootstrap] Active build target was already Android (or switch was a no-op).");

            Debug.Log("[Milestone6Bootstrap] Platform configuration complete.");
        }

        /// <summary>
        /// Imports XR Interaction Toolkit's "Starter Assets" sample (default input actions +
        /// a vetted XR Origin rig prefab with locomotion already wired). Reusing Unity's own
        /// sample rig is safer than hand-wiring LocomotionMediator/XRBodyTransformer/input
        /// reader references blind in a headless script with no way to visually confirm them.
        /// </summary>
        public static void ImportXriStarterAssets()
        {
            UnityEditor.PackageManager.PackageInfo xriPackage = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.XR.Interaction.Toolkit.XRInteractionManager).Assembly);
            if (xriPackage == null)
            {
                Debug.LogError("[Milestone6Bootstrap] Could not resolve the installed XR Interaction Toolkit package info.");
                return;
            }

            List<Sample> samples = Sample.FindByPackage(xriPackage.name, xriPackage.version).ToList();
            int starterAssetsIndex = samples.FindIndex(s => s.displayName == "Starter Assets");

            if (starterAssetsIndex < 0)
            {
                Debug.LogError("[Milestone6Bootstrap] Could not find the 'Starter Assets' sample for com.unity.xr.interaction.toolkit.");
                return;
            }

            Sample starterAssets = samples[starterAssetsIndex];
            bool imported = starterAssets.Import(Sample.ImportOptions.OverridePreviousImports);
            Debug.Log(imported
                ? $"[Milestone6Bootstrap] Imported '{starterAssets.displayName}' to '{starterAssets.importPath}'."
                : "[Milestone6Bootstrap] Sample import reported failure.");
        }

        /// <summary>
        /// Dumps the component hierarchy of the imported "XR Origin (XR Rig)" prefab so the
        /// exact GameObject names/component types can be confirmed before scripting anything
        /// against them. One-time inspection utility, not used by any runtime code.
        /// </summary>
        public static void DumpXrOriginRigHierarchy()
        {
            const string prefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Milestone6Bootstrap] Could not load prefab at '{prefabPath}'.");
                return;
            }

            DumpTransform(prefab.transform, 0);
        }

        private static void DumpTransform(Transform t, int depth)
        {
            string indent = new string(' ', depth * 2);
            Component[] components = t.GetComponents<Component>();
            string componentList = string.Join(", ", components.Select(c => c == null ? "<missing>" : c.GetType().Name));
            Debug.Log($"[Milestone6Bootstrap] {indent}{t.name}  [{componentList}]");

            foreach (Transform child in t)
            {
                DumpTransform(child, depth + 1);
            }
        }
    }
}
