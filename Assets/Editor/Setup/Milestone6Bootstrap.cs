using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

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
        /// The manual step <see cref="ConfigurePlatform"/>'s doc comment deferred to
        /// Project Settings' UI, done via the actual OpenXR editor API instead — turns
        /// out it was never actually done on this machine despite docs/QUEST_TESTING.md
        /// claiming otherwise: Assets/XR/Settings/OpenXR Package Settings.asset still had
        /// both features disabled for Android, which would show a black screen or crash
        /// on launch per that doc's own warning. Doing it via GetFeature&lt;T&gt;() rather
        /// than hand-editing the YAML directly, since the asset stores a list of feature
        /// instances alongside other per-platform bookkeeping this API already knows how
        /// to update correctly.
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/Enable Meta Quest OpenXR Features (Android)")]
        public static void EnableMetaQuestFeaturesForAndroid()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
            {
                Debug.LogError("[Milestone6Bootstrap] No OpenXRSettings found for the Android build target group.");
                return;
            }

            var metaQuestFeature = settings.GetFeature<MetaQuestFeature>();
            if (metaQuestFeature != null)
            {
                metaQuestFeature.enabled = true;
            }
            else
            {
                Debug.LogError("[Milestone6Bootstrap] MetaQuestFeature not found in Android OpenXR settings.");
            }

            var touchPlusProfile = settings.GetFeature<MetaQuestTouchPlusControllerProfile>();
            if (touchPlusProfile != null)
            {
                touchPlusProfile.enabled = true;
            }
            else
            {
                Debug.LogError("[Milestone6Bootstrap] MetaQuestTouchPlusControllerProfile not found in Android OpenXR settings.");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Milestone6Bootstrap] Enabled Meta Quest Support + Touch Plus Controller Profile for Android.");
        }

        /// <summary>
        /// The actual root cause of "Quest build shows a flat 2D panel, controllers do
        /// nothing": OpenXR features being enabled (see
        /// <see cref="EnableMetaQuestFeaturesForAndroid"/>) only configures which OpenXR
        /// extensions/interaction profiles are available — XR Plug-in Management still
        /// needs a *loader* assigned per build target before the app ever calls into XR
        /// at all. Assets/XR/XRGeneralSettingsPerBuildTarget.asset had zero entries for
        /// any platform, meaning no loader was ever assigned, so XRGeneralSettings never
        /// started a subsystem — the app just ran as a plain flat Android activity, which
        /// Quest's OS renders as a floating 2D window instead of anything immersive, and
        /// with no active XR input subsystem, the controllers were never polled either.
        /// Uses XRPackageMetadataStore.AssignLoader, the same scriptable API Unity's own
        /// XR Plug-in Management window uses internally when a loader checkbox is ticked.
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/Assign OpenXR Loader (Android)")]
        public static void AssignOpenXRLoaderForAndroid()
        {
            const string settingsAssetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
            var buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(settingsAssetPath);
            if (buildTargetSettings == null)
            {
                Debug.LogError($"[Milestone6Bootstrap] Could not load '{settingsAssetPath}'.");
                return;
            }

            const BuildTargetGroup buildTargetGroup = BuildTargetGroup.Android;

            XRGeneralSettings settings = buildTargetSettings.SettingsForBuildTarget(buildTargetGroup);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                settings.name = "XR General Settings Android";
                AssetDatabase.AddObjectToAsset(settings, buildTargetSettings);
                buildTargetSettings.SetSettingsForBuildTarget(buildTargetGroup, settings);
            }

            if (settings.Manager == null)
            {
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = "XR Manager Settings Android";
                AssetDatabase.AddObjectToAsset(manager, settings);
                settings.Manager = manager;
            }

            settings.InitManagerOnStart = true;
            // AssignLoader alone left both of these false on a freshly-created manager —
            // without them, XRManagerSettings never actually calls InitializeLoaderSync/
            // StartSubsystems on its own, so nothing would call into XR at runtime even
            // with a loader assigned and InitManagerOnStart set.
            settings.Manager.automaticLoading = true;
            settings.Manager.automaticRunning = true;

            bool assigned = XRPackageMetadataStore.AssignLoader(settings.Manager, typeof(OpenXRLoader).FullName, buildTargetGroup);
            Debug.Log(assigned
                ? "[Milestone6Bootstrap] Assigned the OpenXR loader to the Android build target and enabled init-on-start."
                : "[Milestone6Bootstrap] OpenXR loader was already assigned to the Android build target (or the assignment reported no-op).");

            EditorUtility.SetDirty(buildTargetSettings);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(settings.Manager);
            AssetDatabase.SaveAssets();
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
