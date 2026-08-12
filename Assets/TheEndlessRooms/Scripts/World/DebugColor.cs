using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EndlessRooms.World
{
    /// <summary>
    /// Flat, unmistakable colors for grey-box testing — walls, doors, the Attendant,
    /// hiding spots, and pickups all look identical otherwise (default primitive grey),
    /// which is exactly what made doors hard to tell apart from walls during Milestone
    /// 7 testing. Deliberately simple (Unlit shader, no lighting dependence) so color
    /// reads the same regardless of scene lighting. Replace with real materials as they
    /// arrive (Milestone 8's art pass) — this is placeholder-by-design, not a rendering
    /// system.
    /// </summary>
    public static class DebugColor
    {
        public static readonly Color Wall = Color.yellow;
        public static readonly Color Door = new(0.45f, 0.28f, 0.1f);
        public static readonly Color Attendant = Color.red;
        public static readonly Color HidingSpot = Color.blue;
        public static readonly Color Pickup = Color.green;
        public static readonly Color Note = new(0.9f, 0.87f, 0.7f);

        private static readonly Dictionary<string, Material> MaterialCache = new();

        public static void Apply(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material material = GetOrCreateMaterial(color);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// Materials assigned via <c>renderer.material</c> without ever being saved as
        /// a real asset are purely in-memory — when this ran inside a headless Editor
        /// process building/editing a prefab, the reference didn't survive past that
        /// process exiting, and reloading the prefab in a later session showed the
        /// "missing shader" pink/purple fallback instead. This persists one shared
        /// asset per color (in the Editor) and reuses it via <c>sharedMaterial</c>, so
        /// the reference is real and stable across sessions.
        /// </summary>
        private static Material GetOrCreateMaterial(Color color)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color);
            if (MaterialCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

#if UNITY_EDITOR
            const string folder = "Assets/TheEndlessRooms/Art/Materials/DebugColors";
            string path = $"{folder}/DebugColor_{key}.mat";

            var existingAsset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existingAsset != null)
            {
                MaterialCache[key] = existingAsset;
                return existingAsset;
            }

            EnsureFolderExists(folder);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            MaterialCache[key] = material;
            return material;
#else
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                return null;
            }

            var runtimeMaterial = new Material(shader) { color = color };
            MaterialCache[key] = runtimeMaterial;
            return runtimeMaterial;
#endif
        }

#if UNITY_EDITOR
        private static void EnsureFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = "Assets/TheEndlessRooms/Art/Materials";
            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets/TheEndlessRooms/Art", "Materials");
            }

            AssetDatabase.CreateFolder(parent, "DebugColors");
        }
#endif
    }
}
