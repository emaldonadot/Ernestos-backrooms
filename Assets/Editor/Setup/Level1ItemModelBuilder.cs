using UnityEditor;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Small handheld pickup-item models for Level 1 (battery, cassette, cassette
    /// recorder, two flashlights, two keys) — same box/cylinder-kitbash approach as
    /// Level1FurnitureBuilder's room furniture, built from the user's dimensioned
    /// blueprint references (cm converted to m). These are small enough that cylinders
    /// read fine at gameplay distance, unlike the room-scale furniture which stuck to
    /// cubes only.
    /// </summary>
    internal static class Level1ItemModelBuilder
    {
        private const string MaterialsFolder = "Assets/TheEndlessRooms/Art/Materials";

        private static Material _darkPlasticMaterial;
        private static Material _metalMaterial;
        private static Material _goldMaterial;
        private static Material _bronzeMaterial;
        private static Material _flashlightBodyMaterial;
        private static Material _flashlightLensMaterial;
        private static Material _uvLensMaterial;
        private static Material _paperLabelMaterial;

        private static Material GetOrCreate(ref Material cache, string name, Color color, float smoothness, float metallic, Color? emission = null)
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
            cache.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                cache.SetColor("_EmissionColor", emission.Value);
                cache.EnableKeyword("_EMISSION");
                cache.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            AssetDatabase.CreateAsset(cache, path);
            return cache;
        }

        private static Material DarkPlasticMaterial => GetOrCreate(ref _darkPlasticMaterial, "ItemDarkPlastic", new Color(0.06f, 0.06f, 0.065f), 0.35f, 0.1f);
        private static Material MetalMaterial => GetOrCreate(ref _metalMaterial, "ItemMetal", new Color(0.55f, 0.55f, 0.58f), 0.6f, 0.8f);
        private static Material GoldMaterial => GetOrCreate(ref _goldMaterial, "ItemGold", new Color(0.72f, 0.6f, 0.2f), 0.75f, 0.9f);
        private static Material BronzeMaterial => GetOrCreate(ref _bronzeMaterial, "ItemBronze", new Color(0.42f, 0.3f, 0.16f), 0.5f, 0.75f);
        private static Material FlashlightBodyMaterial => GetOrCreate(ref _flashlightBodyMaterial, "ItemFlashlightBody", new Color(0.08f, 0.08f, 0.09f), 0.55f, 0.6f);
        private static Material FlashlightLensMaterial => GetOrCreate(ref _flashlightLensMaterial, "ItemFlashlightLens", new Color(0.85f, 0.85f, 0.75f), 0.8f, 0f, new Color(0.9f, 0.9f, 0.7f) * 1.5f);
        private static Material UvLensMaterial => GetOrCreate(ref _uvLensMaterial, "ItemUvLens", new Color(0.35f, 0.1f, 0.55f), 0.8f, 0f, new Color(0.55f, 0.1f, 0.9f) * 1.5f);
        private static Material PaperLabelMaterial => GetOrCreate(ref _paperLabelMaterial, "ItemPaperLabel", new Color(0.82f, 0.78f, 0.65f), 0.1f, 0f);

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

        /// <summary>Local Y is the cylinder's own axis (Unity's primitive cylinder stands upright by default) — height is the full length, diameter is X/Z.</summary>
        private static GameObject Cylinder(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, float diameter, float height, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            // Unity's cylinder primitive is 2m tall x 1m diameter at scale 1, so height needs halving to convert to a localScale.
            go.transform.localScale = new Vector3(diameter, height / 2f, diameter);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // ---------------------------------------------------------------- battery

        /// <summary>D-cell, lying on its side (local +Z is the cylinder's long axis).</summary>
        internal static GameObject BuildBattery(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float length = 0.0615f;
            const float diameter = 0.0342f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Cylinder(root.transform, "Body", Vector3.zero, Quaternion.Euler(90f, 0f, 0f), diameter, length, DarkPlasticMaterial);
            Cylinder(root.transform, "PositiveTerminal", new Vector3(0f, 0f, length / 2f + 0.001f), Quaternion.Euler(90f, 0f, 0f), 0.0095f, 0.0015f, MetalMaterial);

            return root;
        }

        // ---------------------------------------------------------------- cassette

        /// <summary>Local +Z is "up" when held flat (the side with the label), local +X the long edge.</summary>
        internal static GameObject BuildCassette(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float width = 0.10f;
            const float height = 0.064f;
            const float depth = 0.012f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Box(root.transform, "Shell", Vector3.zero, new Vector3(width, depth, height), DarkPlasticMaterial);
            Box(root.transform, "Label", new Vector3(0f, depth / 2f + 0.0005f, 0.008f), new Vector3(width - 0.014f, 0.001f, height * 0.45f), PaperLabelMaterial);
            Cylinder(root.transform, "SpoolLeft", new Vector3(-0.024f, depth / 2f + 0.001f, -0.006f), Quaternion.identity, 0.02f, 0.002f, MetalMaterial);
            Cylinder(root.transform, "SpoolRight", new Vector3(0.024f, depth / 2f + 0.001f, -0.006f), Quaternion.identity, 0.02f, 0.002f, MetalMaterial);

            return root;
        }

        // ---------------------------------------------------------------- cassette recorder

        /// <summary>Local +Z is the front (buttons/cassette door face), local +X the width.</summary>
        internal static GameObject BuildCassetteRecorder(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float width = 0.17f;
            const float bodyHeight = 0.11f;
            const float depth = 0.045f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Box(root.transform, "Body", new Vector3(0f, 0f, 0f), new Vector3(width, bodyHeight, depth), DarkPlasticMaterial);
            Cylinder(root.transform, "SpeakerGrill", new Vector3(width * 0.22f, 0.01f, depth / 2f + 0.001f), Quaternion.Euler(90f, 0f, 0f), 0.05f, 0.003f, MetalMaterial);
            Box(root.transform, "CassetteWindow", new Vector3(-width * 0.2f, 0.015f, depth / 2f + 0.001f), new Vector3(0.06f, 0.001f, 0.045f), PaperLabelMaterial);

            // Carry handle: two uprights + a top bar, forming a simple loop above the body.
            const float handleHeight = 0.06f;
            Box(root.transform, "Handle_Left", new Vector3(-width / 2f + 0.015f, bodyHeight / 2f + handleHeight / 2f, 0f), new Vector3(0.012f, handleHeight, 0.012f), DarkPlasticMaterial);
            Box(root.transform, "Handle_Right", new Vector3(width / 2f - 0.015f, bodyHeight / 2f + handleHeight / 2f, 0f), new Vector3(0.012f, handleHeight, 0.012f), DarkPlasticMaterial);
            Box(root.transform, "Handle_Top", new Vector3(0f, bodyHeight / 2f + handleHeight, 0f), new Vector3(width - 0.03f, 0.012f, 0.012f), DarkPlasticMaterial);

            return root;
        }

        // ---------------------------------------------------------------- flashlights

        /// <summary>Local +Z is the direction the light points.</summary>
        internal static GameObject BuildFlashlight(Transform parent, string name, Vector3 worldPosition, Quaternion rotation, out Transform lensTransform)
        {
            return BuildFlashlightShared(parent, name, worldPosition, rotation, 0.21f, 0.045f, 0.055f, 0.047f, FlashlightBodyMaterial, FlashlightLensMaterial, out lensTransform);
        }

        /// <summary>Local +Z is the direction the light points.</summary>
        internal static GameObject BuildUvFlashlight(Transform parent, string name, Vector3 worldPosition, Quaternion rotation, out Transform lensTransform)
        {
            return BuildFlashlightShared(parent, name, worldPosition, rotation, 0.16f, 0.038f, 0.042f, 0.04f, FlashlightBodyMaterial, UvLensMaterial, out lensTransform);
        }

        private static GameObject BuildFlashlightShared(Transform parent, string name, Vector3 worldPosition, Quaternion rotation, float length, float bodyDiameter, float headDiameter, float tailDiameter, Material bodyMaterial, Material lensMaterial, out Transform lensTransform)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            const float headLength = 0.035f;
            const float tailLength = 0.02f;
            float bodyLength = length - headLength - tailLength;

            float tailZ = -length / 2f + tailLength / 2f;
            float bodyZ = tailZ + tailLength / 2f + bodyLength / 2f;
            float headZ = bodyZ + bodyLength / 2f + headLength / 2f;

            Cylinder(root.transform, "TailCap", new Vector3(0f, 0f, tailZ), Quaternion.Euler(90f, 0f, 0f), tailDiameter, tailLength, bodyMaterial);
            Cylinder(root.transform, "Body", new Vector3(0f, 0f, bodyZ), Quaternion.Euler(90f, 0f, 0f), bodyDiameter, bodyLength, bodyMaterial);
            Box(root.transform, "Switch", new Vector3(0f, bodyDiameter / 2f + 0.003f, bodyZ), new Vector3(0.012f, 0.006f, 0.03f), DarkPlasticMaterial);
            Cylinder(root.transform, "Head", new Vector3(0f, 0f, headZ), Quaternion.Euler(90f, 0f, 0f), headDiameter, headLength, bodyMaterial);

            var lens = Cylinder(root.transform, "Lens", new Vector3(0f, 0f, headZ + headLength / 2f), Quaternion.Euler(90f, 0f, 0f), headDiameter * 0.85f, 0.004f, lensMaterial);
            lensTransform = lens.transform;

            return root;
        }

        // ---------------------------------------------------------------- keys

        internal static GameObject BuildGoldenKey(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            return BuildKeyShared(parent, name, worldPosition, rotation, 0.09f, 0.036f, 0.006f, 0.02f, 0.013f, GoldMaterial);
        }

        internal static GameObject BuildBronzeKey(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            return BuildKeyShared(parent, name, worldPosition, rotation, 0.085f, 0.032f, 0.006f, 0.018f, 0.012f, BronzeMaterial);
        }

        /// <summary>Local +Z is the shaft's length axis, bow at -Z, bit teeth at +Z.</summary>
        private static GameObject BuildKeyShared(Transform parent, string name, Vector3 worldPosition, Quaternion rotation, float overallLength, float bowWidth, float thickness, float bitWidth, float bitHeight, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            float shaftLength = overallLength - bowWidth / 2f;
            float bowZ = -overallLength / 2f + bowWidth / 2f;
            float shaftZ = bowZ + bowWidth / 2f + shaftLength / 2f - 0.01f;

            // Bow: a flat ring approximated as a disc with a smaller, slightly recessed
            // disc "hole" — full ornate scrollwork isn't practical with primitives at
            // this scale, but the disc-with-a-dark-center silhouette still clearly
            // reads as a key's bow from normal pickup viewing distance.
            Cylinder(root.transform, "Bow", new Vector3(0f, 0f, bowZ), Quaternion.Euler(90f, 0f, 0f), bowWidth, thickness, material);
            Cylinder(root.transform, "BowHole", new Vector3(0f, 0f, bowZ), Quaternion.Euler(90f, 0f, 0f), bowWidth * 0.5f, thickness + 0.001f, DarkPlasticMaterial);

            Cylinder(root.transform, "Shaft", new Vector3(0f, 0f, shaftZ), Quaternion.Euler(90f, 0f, 0f), thickness * 1.4f, shaftLength, material);

            float bitZ = overallLength / 2f - bitHeight / 2f;
            Box(root.transform, "Bit", new Vector3(bitWidth / 4f, 0f, bitZ), new Vector3(bitWidth / 2f, thickness, bitHeight), material);

            return root;
        }
    }
}
