using UnityEditor;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Two courtyard planters built from the user's reference sheets (Office Planter 01
    /// — round ceramic pot, snake plant; Office Planter 02 — rectangular concrete
    /// planter, broad-leaf plant), converted from the sheets' centimeter dimensions to
    /// meters. Leaves are flattened box "blades" fanned out from a central pivot per
    /// leaf (rotate the pivot, grow the box up its local Y) rather than any curved mesh
    /// — matches this project's box/cylinder-kitbash convention and the reference
    /// sheets' own "simple leaf meshes for optimized modeling" note.
    /// </summary>
    internal static class Level1PlanterBuilder
    {
        private const string MaterialsFolder = "Assets/TheEndlessRooms/Art/Materials";

        private static Material _ceramicMaterial;
        private static Material _concreteMaterial;
        private static Material _soilMaterial;
        private static Material _snakeLeafMaterial;
        private static Material _broadLeafMaterial;

        private static Material GetOrCreate(ref Material cache, string name, Color color, float smoothness)
        {
            if (cache != null)
            {
                return cache;
            }

            string path = $"{MaterialsFolder}/{name}_Level1.mat";
            cache = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (cache != null)
            {
                return cache;
            }

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/TheEndlessRooms/Art", "Materials");
            }

            cache = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            cache.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(cache, path);
            return cache;
        }

        private static Material CeramicMaterial => GetOrCreate(ref _ceramicMaterial, "PlanterCeramic", new Color(0.8f, 0.78f, 0.74f), 0.3f);
        private static Material ConcreteMaterial => GetOrCreate(ref _concreteMaterial, "PlanterConcrete", new Color(0.54f, 0.53f, 0.5f), 0.12f);
        private static Material SoilMaterial => GetOrCreate(ref _soilMaterial, "PlanterSoil", new Color(0.17f, 0.12f, 0.08f), 0.05f);
        private static Material SnakeLeafMaterial => GetOrCreate(ref _snakeLeafMaterial, "PlanterSnakeLeaf", new Color(0.15f, 0.32f, 0.14f), 0.25f);
        private static Material BroadLeafMaterial => GetOrCreate(ref _broadLeafMaterial, "PlanterBroadLeaf", new Color(0.13f, 0.29f, 0.12f), 0.3f);

        private static GameObject Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Local Y is the cylinder's own axis — height is the full length, diameter is X/Z.</summary>
        private static GameObject Cylinder(Transform parent, string name, Vector3 localPosition, float diameter, float height, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(diameter, height / 2f, diameter);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static void AddLeafCluster(Transform root, float baseY, int leafCount, float leafWidth, float leafThickness, float leafHeight, float heightJitter, float baseTiltDegrees, float tiltJitterDegrees, float angleOffset, Material material)
        {
            for (int i = 0; i < leafCount; i++)
            {
                float angle = angleOffset + i * (360f / leafCount);
                float height = leafHeight * (1f - heightJitter + heightJitter * 2f * (i % 3) / 2f);
                float tilt = baseTiltDegrees + tiltJitterDegrees * (i % 2);

                var pivot = new GameObject($"LeafPivot_{i}");
                pivot.transform.SetParent(root, false);
                pivot.transform.localPosition = new Vector3(0f, baseY, 0f);
                pivot.transform.localRotation = Quaternion.Euler(tilt, angle, 0f);

                Box(pivot.transform, "Leaf", new Vector3(0f, height / 2f, 0f), new Vector3(leafWidth, height, leafThickness), material);
            }
        }

        /// <summary>Office Planter 01: 28cm-diameter round ceramic pot, 24cm tall, 70cm overall plant height.</summary>
        internal static GameObject BuildRoundCeramicSnakePlant(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float potDiameter = 0.28f;
            const float potHeight = 0.24f;
            const float overallPlantHeight = 0.70f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Cylinder(root.transform, "Pot", new Vector3(0f, potHeight / 2f, 0f), potDiameter, potHeight, CeramicMaterial);
            Cylinder(root.transform, "Soil", new Vector3(0f, potHeight - 0.02f, 0f), potDiameter - 0.03f, 0.04f, SoilMaterial);

            float leafHeight = overallPlantHeight - potHeight;
            AddLeafCluster(root.transform, potHeight - 0.03f, leafCount: 8, leafWidth: 0.035f, leafThickness: 0.012f, leafHeight: leafHeight, heightJitter: 0.15f, baseTiltDegrees: 8f, tiltJitterDegrees: 6f, angleOffset: 0f, SnakeLeafMaterial);

            return root;
        }

        /// <summary>Office Planter 02: 100x35x45cm rectangular concrete planter, 70cm overall plant height.</summary>
        internal static GameObject BuildRectangularConcreteBroadLeafPlanter(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float length = 1.0f;
            const float width = 0.35f;
            const float height = 0.45f;
            const float wallThickness = 0.05f;
            const float overallPlantHeight = 0.70f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Box(root.transform, "Planter", new Vector3(0f, height / 2f, 0f), new Vector3(length, height, width), ConcreteMaterial);
            Box(root.transform, "Soil", new Vector3(0f, height - wallThickness * 0.6f, 0f), new Vector3(length - wallThickness * 2f, 0.06f, width - wallThickness * 2f), SoilMaterial);

            float leafHeight = overallPlantHeight - height * 0.7f;
            AddLeafCluster(root.transform, height - 0.05f, leafCount: 6, leafWidth: 0.1f, leafThickness: 0.02f, leafHeight: leafHeight, heightJitter: 0.18f, baseTiltDegrees: 12f, tiltJitterDegrees: 8f, angleOffset: 15f, BroadLeafMaterial);

            return root;
        }
    }
}
