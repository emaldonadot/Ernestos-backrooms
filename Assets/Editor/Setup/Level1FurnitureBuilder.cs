using UnityEditor;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Box-kitbash furniture models for Level 1, built from the user's dimensioned
    /// blueprint references (all measurements converted cm to m, 1 Unity unit = 1
    /// meter). No FBX import pipeline exists in this project (see the secret room's
    /// ChatGPT-generated textures for the established "no 3D asset import" precedent),
    /// so these are assembled the same way every other piece of Level 1 geometry is:
    /// primitive cubes parented under a root, sharing one flat "old worn stained wood"
    /// material. Deliberately skips fine details invisible at gameplay distance/lighting
    /// (hinges, locks, cable grommet holes, interior closet hanging rod) — overall
    /// silhouette and the most visually distinguishing features (drawer fronts, shelf
    /// gaps, closet double doors) are what actually read in a dim horror-lit room.
    /// </summary>
    internal static class Level1FurnitureBuilder
    {
        private const string MaterialPath = "Assets/TheEndlessRooms/Art/Materials/WornWood_Level1.mat";
        private static Material _wornWoodMaterial;

        private static Material WornWoodMaterial
        {
            get
            {
                if (_wornWoodMaterial != null)
                {
                    return _wornWoodMaterial;
                }

                _wornWoodMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (_wornWoodMaterial != null)
                {
                    return _wornWoodMaterial;
                }

                if (!AssetDatabase.IsValidFolder("Assets/TheEndlessRooms/Art/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets/TheEndlessRooms/Art", "Materials");
                }

                _wornWoodMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.16f, 0.11f, 0.07f),
                };
                _wornWoodMaterial.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(_wornWoodMaterial, MaterialPath);
                return _wornWoodMaterial;
            }
        }

        private static GameObject Part(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = WornWoodMaterial;
            return go;
        }

        /// <summary>Local +Z is this model's "front" — callers orient it with Quaternion.LookRotation.</summary>
        internal static GameObject BuildChair(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float width = 0.48f;
            const float depth = 0.54f;
            const float seatHeight = 0.46f;
            const float seatThickness = 0.03f;
            const float backrestHeight = 0.40f;
            const float legThickness = 0.045f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Part(root.transform, "Seat", new Vector3(0f, seatHeight, 0f), new Vector3(width, seatThickness, depth));
            Part(root.transform, "Backrest", new Vector3(0f, seatHeight + backrestHeight / 2f, -depth / 2f + legThickness / 2f), new Vector3(width, backrestHeight, 0.03f));

            float legX = width / 2f - legThickness / 2f;
            float frontZ = depth / 2f - legThickness / 2f;
            float backZ = -depth / 2f + legThickness / 2f;

            Part(root.transform, "Leg_FrontLeft", new Vector3(-legX, seatHeight / 2f, frontZ), new Vector3(legThickness, seatHeight, legThickness));
            Part(root.transform, "Leg_FrontRight", new Vector3(legX, seatHeight / 2f, frontZ), new Vector3(legThickness, seatHeight, legThickness));
            Part(root.transform, "Leg_BackLeft", new Vector3(-legX, (seatHeight + backrestHeight) / 2f, backZ), new Vector3(legThickness, seatHeight + backrestHeight, legThickness));
            Part(root.transform, "Leg_BackRight", new Vector3(legX, (seatHeight + backrestHeight) / 2f, backZ), new Vector3(legThickness, seatHeight + backrestHeight, legThickness));

            return root;
        }

        /// <summary>Local +Z is the side facing away from the shelf's own back panel.</summary>
        internal static GameObject BuildBookshelf(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float width = 0.90f;
            const float depth = 0.30f;
            const float height = 1.80f;
            const float panelThickness = 0.02f;
            const float plinthHeight = 0.10f;
            const float shelfThickness = 0.02f;
            const float interiorWidth = 0.86f;
            const float interiorDepth = 0.28f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Part(root.transform, "Plinth", new Vector3(0f, plinthHeight / 2f, 0f), new Vector3(width, plinthHeight, depth));
            Part(root.transform, "Top", new Vector3(0f, height - 0.015f, 0f), new Vector3(width, 0.03f, depth));
            Part(root.transform, "Side_Left", new Vector3(-(width / 2f - panelThickness / 2f), (height + plinthHeight) / 2f, 0f), new Vector3(panelThickness, height - plinthHeight, depth));
            Part(root.transform, "Side_Right", new Vector3(width / 2f - panelThickness / 2f, (height + plinthHeight) / 2f, 0f), new Vector3(panelThickness, height - plinthHeight, depth));
            Part(root.transform, "Back", new Vector3(0f, (height + plinthHeight) / 2f, -depth / 2f + 0.0075f), new Vector3(interiorWidth, height - plinthHeight, 0.015f));

            float[] shelfCenters = { 0.41f, 0.76f, 1.11f, 1.46f };
            for (int i = 0; i < shelfCenters.Length; i++)
            {
                Part(root.transform, $"Shelf_{i + 1}", new Vector3(0f, shelfCenters[i], 0f), new Vector3(interiorWidth, shelfThickness, interiorDepth));
            }

            return root;
        }

        /// <summary>Local +Z faces away from the desk's back edge (where a chair would sit) — drawer fronts face this direction.</summary>
        internal static GameObject BuildDesk(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float width = 1.60f;
            const float depth = 0.80f;
            const float deskHeight = 0.75f;
            const float desktopThickness = 0.04f;
            const float pedestalWidth = 0.48f;
            const float pedestalDepth = 0.70f;
            const float pedestalHeight = 0.71f;
            const float modestyHeight = 0.30f;
            const float modestyWidth = 0.64f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Part(root.transform, "Desktop", new Vector3(0f, deskHeight - desktopThickness / 2f, 0f), new Vector3(width, desktopThickness, depth));

            float pedestalX = width / 2f - pedestalWidth / 2f;
            Part(root.transform, "Pedestal_Left", new Vector3(-pedestalX, pedestalHeight / 2f, 0f), new Vector3(pedestalWidth, pedestalHeight, pedestalDepth));
            Part(root.transform, "Pedestal_Right", new Vector3(pedestalX, pedestalHeight / 2f, 0f), new Vector3(pedestalWidth, pedestalHeight, pedestalDepth));

            Part(root.transform, "ModestyPanel", new Vector3(0f, deskHeight - desktopThickness - modestyHeight / 2f, -pedestalDepth / 2f + 0.01f), new Vector3(modestyWidth, modestyHeight, 0.02f));

            // Drawer fronts + handles, proud of each pedestal's outward face (local +Z).
            float pedestalFaceZ = pedestalDepth / 2f + 0.005f;
            float[] drawerCenters = { pedestalHeight - 0.16f, pedestalHeight - 0.16f - 0.17f - 0.17f };
            foreach (float x in new[] { -pedestalX, pedestalX })
            {
                foreach (float drawerY in drawerCenters)
                {
                    Part(root.transform, "DrawerFront", new Vector3(x, drawerY, pedestalFaceZ), new Vector3(pedestalWidth - 0.06f, 0.30f, 0.01f));
                    Part(root.transform, "DrawerHandle", new Vector3(x, drawerY, pedestalFaceZ + 0.02f), new Vector3(0.12f, 0.02f, 0.03f));
                }
            }

            return root;
        }

        /// <summary>Local +Z is the side the doors open toward (into the room).</summary>
        internal static GameObject BuildCloset(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float width = 1.00f;
            const float depth = 0.60f;
            const float height = 2.10f;
            const float panelThickness = 0.03f;
            const float plinthHeight = 0.08f;
            const float doorWidth = 0.48f;
            const float doorHeight = 1.80f;
            const float doorThickness = 0.025f;
            const float doorGap = 0.04f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Part(root.transform, "Plinth", new Vector3(0f, plinthHeight / 2f, 0f), new Vector3(width, plinthHeight, depth));
            Part(root.transform, "Top", new Vector3(0f, height - 0.02f, 0f), new Vector3(width, 0.04f, depth));
            Part(root.transform, "Side_Left", new Vector3(-(width / 2f - panelThickness / 2f), (height + plinthHeight) / 2f, 0f), new Vector3(panelThickness, height - plinthHeight, depth));
            Part(root.transform, "Side_Right", new Vector3(width / 2f - panelThickness / 2f, (height + plinthHeight) / 2f, 0f), new Vector3(panelThickness, height - plinthHeight, depth));
            Part(root.transform, "Back", new Vector3(0f, (height + plinthHeight) / 2f, -depth / 2f + 0.0075f), new Vector3(width - 0.06f, height - plinthHeight, 0.015f));

            float doorCenterY = plinthHeight + doorHeight / 2f;
            float doorX = doorGap / 2f + doorWidth / 2f;
            float doorZ = depth / 2f - doorThickness / 2f;
            Part(root.transform, "Door_Left", new Vector3(-doorX, doorCenterY, doorZ), new Vector3(doorWidth, doorHeight, doorThickness));
            Part(root.transform, "Door_Right", new Vector3(doorX, doorCenterY, doorZ), new Vector3(doorWidth, doorHeight, doorThickness));
            Part(root.transform, "Handle_Left", new Vector3(-doorGap / 2f - 0.03f, doorCenterY, doorZ + 0.02f), new Vector3(0.03f, 0.14f, 0.03f));
            Part(root.transform, "Handle_Right", new Vector3(doorGap / 2f + 0.03f, doorCenterY, doorZ + 0.02f), new Vector3(0.03f, 0.14f, 0.03f));

            return root;
        }
    }
}
