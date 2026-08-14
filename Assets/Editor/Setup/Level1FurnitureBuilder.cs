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

        private const string PorcelainMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Porcelain_Level1.mat";
        private static Material _porcelainMaterial;

        private static Material PorcelainMaterial
        {
            get
            {
                if (_porcelainMaterial != null)
                {
                    return _porcelainMaterial;
                }

                _porcelainMaterial = AssetDatabase.LoadAssetAtPath<Material>(PorcelainMaterialPath);
                if (_porcelainMaterial != null)
                {
                    return _porcelainMaterial;
                }

                if (!AssetDatabase.IsValidFolder("Assets/TheEndlessRooms/Art/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets/TheEndlessRooms/Art", "Materials");
                }

                _porcelainMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.72f, 0.71f, 0.66f),
                };
                _porcelainMaterial.SetFloat("_Smoothness", 0.45f);
                AssetDatabase.CreateAsset(_porcelainMaterial, PorcelainMaterialPath);
                return _porcelainMaterial;
            }
        }

        private static GameObject Part(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material != null ? material : WornWoodMaterial;
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

            // The player rig's camera sits a fixed 1.6m above its CharacterController
            // root regardless of crouch, so putting the camera at a believable
            // under-desk height (~0.7m) means placing this hide anchor 1.6m below where
            // the camera should actually end up — i.e. well below the desk's own floor.
            // Harmless: HidingSpot disables the CharacterController for as long as it's
            // hidden, so nothing ever calls Move() against this "buried" position.
            var hideAnchor = new GameObject("HideAnchor");
            hideAnchor.transform.SetParent(root.transform, false);
            hideAnchor.transform.localPosition = new Vector3(0f, -1.0f, 0.15f);

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

            // Standing height (Y=0 here, same as this root's own floor level) — the
            // player's camera pivot is a fixed 1.6m above the CharacterController root
            // it's parented to, matching normal standing eye height. Facing local +Z,
            // i.e. toward the doors, so "Come Out" stays reachable via the door panels.
            var hideAnchor = new GameObject("HideAnchor");
            hideAnchor.transform.SetParent(root.transform, false);
            hideAnchor.transform.localPosition = Vector3.zero;

            return root;
        }

        /// <summary>Local +Z is the front (where a person sits, facing away from the tank).</summary>
        internal static GameObject BuildToilet(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Part(root.transform, "Bowl", new Vector3(0f, 0.20f, 0.09f), new Vector3(0.36f, 0.40f, 0.50f), PorcelainMaterial);
            Part(root.transform, "Seat", new Vector3(0f, 0.425f, 0.02f), new Vector3(0.36f, 0.05f, 0.44f), PorcelainMaterial);
            Part(root.transform, "Tank", new Vector3(0f, 0.57f, -0.25f), new Vector3(0.38f, 0.34f, 0.18f), PorcelainMaterial);
            Part(root.transform, "TankLid", new Vector3(0f, 0.76f, -0.25f), new Vector3(0.38f, 0.04f, 0.18f), PorcelainMaterial);

            return root;
        }

        /// <summary>Local +Z is the front (the faucet-and-basin side, facing away from the wall).</summary>
        internal static GameObject BuildSink(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Part(root.transform, "Basin", new Vector3(0f, 0.77f, 0f), new Vector3(0.60f, 0.16f, 0.48f), PorcelainMaterial);
            Part(root.transform, "Pedestal", new Vector3(0f, 0.345f, 0f), new Vector3(0.20f, 0.69f, 0.18f), PorcelainMaterial);
            Part(root.transform, "Faucet", new Vector3(0f, 0.94f, -0.12f), new Vector3(0.04f, 0.18f, 0.04f), PorcelainMaterial);
            Part(root.transform, "HandleHot", new Vector3(-0.15f, 0.87f, -0.1f), new Vector3(0.05f, 0.05f, 0.05f), PorcelainMaterial);
            Part(root.transform, "HandleCold", new Vector3(0.15f, 0.87f, -0.1f), new Vector3(0.05f, 0.05f, 0.05f), PorcelainMaterial);

            return root;
        }

        /// <summary>Local +Z is unused (symmetric) — chairs surround it on both long sides (local X), so orientation barely matters.</summary>
        internal static GameObject BuildMeetingTable(Transform parent, string name, Vector3 worldPosition, Quaternion rotation)
        {
            const float length = 2.40f;
            const float width = 1.00f;
            const float tableHeight = 0.75f;
            const float topThickness = 0.04f;
            const float legThickness = 0.08f;
            const float legHeight = 0.71f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPosition, rotation);

            Part(root.transform, "Tabletop", new Vector3(0f, tableHeight - topThickness / 2f, 0f), new Vector3(width, topThickness, length));

            float legX = width / 2f - legThickness / 2f - 0.02f;
            float legZ = length / 2f - legThickness / 2f - 0.06f;
            Part(root.transform, "Leg_FrontLeft", new Vector3(-legX, legHeight / 2f, legZ), new Vector3(legThickness, legHeight, legThickness));
            Part(root.transform, "Leg_FrontRight", new Vector3(legX, legHeight / 2f, legZ), new Vector3(legThickness, legHeight, legThickness));
            Part(root.transform, "Leg_BackLeft", new Vector3(-legX, legHeight / 2f, -legZ), new Vector3(legThickness, legHeight, legThickness));
            Part(root.transform, "Leg_BackRight", new Vector3(legX, legHeight / 2f, -legZ), new Vector3(legThickness, legHeight, legThickness));

            return root;
        }
    }
}
