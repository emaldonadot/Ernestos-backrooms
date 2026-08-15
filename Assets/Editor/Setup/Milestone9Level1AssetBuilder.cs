using System.Collections.Generic;
using System.Linq;
using EndlessRooms.AI;
using EndlessRooms.Core;
using EndlessRooms.Player;
using EndlessRooms.UI;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Side = EndlessRooms.World.Level1Layout.Side;
using OfficeSpec = EndlessRooms.World.Level1Layout.OfficeSpec;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for Milestone 9's Level 1: a fixed, hand-authored cross-spine
    /// office building (not procedural — see docs/features/milestone-9-playable-office-levels.md
    /// and <see cref="Level1Layout"/> for the shared position math). 14 offices at
    /// 5m(deep) x 6m(wide) x 3m(tall), a 6m-wide corridor, 2 paired bathrooms, 2 open-air
    /// courtyards, a PC player, the Attendant (unchanged from Milestone 7, pathing via
    /// <see cref="Level1RoomGraphProvider"/>'s synthetic graph), the exit (Golden Key
    /// gated), and the full collectible/lock progression chain: ID Card unlocks the UV
    /// Flashlight room and the Cassette's drawer; Cassette+Recorder reveal the Bronze
    /// Key's room and the male-bathroom UV code; Bronze Key unlocks the Battery's
    /// drawer; Battery powers the UV Flashlight; the revealed code opens R12's keypad
    /// safe, which holds the Golden Key. Spider-web placeholders (SpiderWebClue) mark
    /// every progression room.
    /// </summary>
    public static class Milestone9Level1AssetBuilder
    {
        private const float WallHeight = 3f;
        private const float WallThickness = 0.2f;
        private const float DoorWidth = 2f;

        // Window.png blueprint: 2.00m x 1.20m window, 2.05m x 1.25m rough wall opening.
        private const float WindowOpeningWidth = 2.05f;
        private const float WindowOpeningHeight = 1.25f;
        private const float WindowSillHeight = 0.9f;
        private const float WindowWidth = 2.0f;
        private const float WindowHeight = 1.2f;

        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone9_Level1TestScene.unity";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";
        private const string MovementConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/PlayerMovementConfig.asset";
        private const string ItemsFolder = "Assets/TheEndlessRooms/ScriptableObjects/Items";
        private const string JumpScareSpritePath = "Assets/TheEndlessRooms/Art/Textures/JumpScareMonster.png";
        private const float JumpScareSpritePixelsPerUnit = 700f;

        // Matches BuildOfficeFurniture's own desk placement — reused by BuildProgressionContent
        // to rest desk-top items at the actual desk position instead of a second
        // hardcoded number that could drift out of sync with it.
        private const float OfficeDeskLocalX = -0.3f;

        private const string WallTexturePath = "Assets/TheEndlessRooms/Art/Textures/WallAlbedo_Level1.png";
        private const string DoorTexturePath = "Assets/TheEndlessRooms/Art/Textures/Door_Level1.png";
        private const string WallMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Wall_Level1.mat";
        private const string DoorMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Door_Level1.mat";
        private const float WallTextureMetersPerTile = 2f; // WallAlbedo_Level1.png is a tileable ~2m x 2m square texture

        private const string CarpetTexturePath = "Assets/TheEndlessRooms/Art/Textures/Carpet_Level1.png";
        private const string CeilingTexturePath = "Assets/TheEndlessRooms/Art/Textures/Ceiling_Level1.png";
        private const string CarpetMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Carpet_Level1.mat";
        private const string CeilingMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Ceiling_Level1.mat";
        // Carpet_Level1.png already bakes in a visible tile grid, so tiling it too
        // tightly would double up grid lines; treated as one big 4m x 4m patch instead.
        private const float CarpetTextureMetersPerTile = 4f;
        private const float CeilingTextureMetersPerTile = 2.5f;

        private const string MenBathroomDoorTexturePath = "Assets/TheEndlessRooms/Art/Textures/Door_BathroomMen_Level1.png";
        private const string WomenBathroomDoorTexturePath = "Assets/TheEndlessRooms/Art/Textures/Door_BathroomWomen_Level1.png";
        private const string MenBathroomDoorMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Door_BathroomMen_Level1.mat";
        private const string WomenBathroomDoorMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Door_BathroomWomen_Level1.mat";

        private const string BathroomWallTexturePath = "Assets/TheEndlessRooms/Art/Textures/BathroomWall_Level1.png";
        private const string BathroomFloorTexturePath = "Assets/TheEndlessRooms/Art/Textures/BathroomFloor_Level1.png";
        private const string BathroomWallMaterialPath = "Assets/TheEndlessRooms/Art/Materials/BathroomWall_Level1.mat";
        private const string BathroomFloorMaterialPath = "Assets/TheEndlessRooms/Art/Materials/BathroomFloor_Level1.mat";

        private const string ThunderClipPath = "Assets/TheEndlessRooms/Audio/ThunderCrash_Level1.wav";
        private const string ScreamClipPath = "Assets/TheEndlessRooms/Audio/CaptureScream_Level1.wav";
        private const string HeartbeatClipPath = "Assets/TheEndlessRooms/Audio/CaptureHeartbeat_Level1.wav";
        private const string HeartbeatVignetteTexturePathPrefix = "Assets/TheEndlessRooms/Art/Textures/HeartbeatVignette";

        private const string CassetteMessageText =
            "(A woman's voice, half-buried in tape hiss)\n\n" +
            "\"...if anyone finds this — there's a UV bulb in the maintenance kit. " +
            "Power it up and use it in the men's room, there's something written on " +
            "the wall that doesn't show under normal light. And if you need a bronze " +
            "key for anything, check R09 — it's just sitting there on the desk.\"";

        [MenuItem("Tools/The Endless Rooms/M9 Level 1/Build Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            new GameObject("GameBootstrap").AddComponent<Core.GameBootstrap>();

            var levelRoot = new GameObject("Level1_HorrorOffice").transform;

            BuildMainCorridor(levelRoot);
            BuildCrossCorridorArms(levelRoot);

            foreach (OfficeSpec office in Level1Layout.Offices)
            {
                BuildOffice(levelRoot, office);
            }

            var courtyardRain = new[]
            {
                BuildCourtyard(levelRoot, Level1Layout.CourtyardRow, Side.West),
                BuildCourtyard(levelRoot, Level1Layout.CourtyardRow, Side.East),
            };
            BuildBathroom(levelRoot, Level1Layout.BathroomRow, Side.West, "Bathroom_Women");
            BuildBathroom(levelRoot, Level1Layout.BathroomRow, Side.East, "Bathroom_Men");

            GameObject levelGraphGo = BuildLevelGraph();
            FlickeringLight[] warningLights = BuildCorridorWarningLights(levelRoot);

            ActionRefs actionRefs = LoadInputActionReferences();
            CollectibleItems items = LoadOrCreateCollectibleItems();
            var movementConfig = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
            GameObject playerGo = BuildPlayer(movementConfig, actionRefs, items, out InteractionCaster interactionCaster, out CameraShakeEffect cameraShake, out Inventory inventory, out InventorySelectionController selectionController);
            playerGo.transform.position = new Vector3(0f, 1f, 1.5f);
            _ = cameraShake;

            LightningFlash lightningFlash = BuildNightAmbiance();

            var attendantConfig = Milestone7AssetBuilder.LoadOrCreateAttendantConfig();
            Milestone7AssetBuilder.BuildAttendant(levelGraphGo, attendantConfig, playerGo.transform);
            BuildAttendantAppearanceCycle(warningLights, courtyardRain, lightningFlash);

            BuildExitPoint(items.GoldenKey);
            BuildJumpScares();
            BuildCentralCorridorFurniture(items);
            BuildProgressionContent(items, inventory);

            BuildInteractionPromptUi(interactionCaster);
            BuildLevelCompleteUi();
            BuildGameOverUi();
            BuildHeartbeatVignetteUi();
            BuildFieldNoteUi(actionRefs.Interact);
            BuildKeypadEntryUi(actionRefs.Interact);
            BuildInventoryHudUi(inventory, selectionController);

            ApplyLevel1Materials(levelRoot);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone9Level1AssetBuilder] Built and saved '{ScenePath}' — {Level1Layout.Offices.Length} offices, 2 bathrooms, 2 courtyards, player, Attendant, Golden-Key-gated exit, full 8-item progression chain, jump scares.");
        }

        // ---------------------------------------------------------------- materials

        /// <summary>
        /// Replaces the plain default-material walls/doors with real textures. Runs as
        /// a post-pass over the finished hierarchy (matching every "Wall_"-prefixed and
        /// "DoorPanel" object by name, recursively — Level 1's rooms are nested several
        /// levels deep, unlike Milestone 8's flat shared room prefab) rather than
        /// threading a material through every CreateBlock/PlaceDoor call site.
        /// </summary>
        private static void ApplyLevel1Materials(Transform levelRoot)
        {
            Material wallMaterial = CreateSimpleTexturedMaterial(WallTexturePath, WallMaterialPath, mirrorHorizontal: false);
            // Door_Level1.png is 1024x1536 (2:3), exactly matching the door panel's
            // 2m x 3m face, so it already maps with zero stretching — mirrored
            // horizontally here so the handle reads on the opposite side from the
            // source art.
            Material doorMaterial = CreateSimpleTexturedMaterial(DoorTexturePath, DoorMaterialPath, mirrorHorizontal: true);
            Material carpetMaterial = CreateSimpleTexturedMaterial(CarpetTexturePath, CarpetMaterialPath, mirrorHorizontal: false);
            Material ceilingMaterial = CreateSimpleTexturedMaterial(CeilingTexturePath, CeilingMaterialPath, mirrorHorizontal: false);
            Material menBathroomDoorMaterial = CreateSimpleTexturedMaterial(MenBathroomDoorTexturePath, MenBathroomDoorMaterialPath, mirrorHorizontal: true);
            Material womenBathroomDoorMaterial = CreateSimpleTexturedMaterial(WomenBathroomDoorTexturePath, WomenBathroomDoorMaterialPath, mirrorHorizontal: true);
            Material bathroomWallMaterial = CreateSimpleTexturedMaterial(BathroomWallTexturePath, BathroomWallMaterialPath, mirrorHorizontal: false);
            Material bathroomFloorMaterial = CreateSimpleTexturedMaterial(BathroomFloorTexturePath, BathroomFloorMaterialPath, mirrorHorizontal: false);

            int wallCount = 0;
            int doorCount = 0;
            int floorCount = 0;
            int ceilingCount = 0;

            foreach (Transform t in levelRoot.GetComponentsInChildren<Transform>(true))
            {
                var renderer = t.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                // Walls/floors sit directly under their room GameObject (see
                // BuildRoomShellWithDoor/BuildBathroom), so the immediate parent's name
                // is enough to tell a bathroom's own surfaces from every other room's.
                string parentName = t.parent != null ? t.parent.name : "";
                bool isBathroom = parentName is "Bathroom_Men" or "Bathroom_Women";
                // Only the walls fully inside the bathroom (back + both sides) get the
                // tile look — the door-flanking wall stays the regular office material
                // so the transition from corridor to bathroom entrance reads as
                // continuous with the rest of the building, not a hard style cut right
                // at the doorway.
                bool isBathroomInteriorWall = isBathroom && t.name is "Wall_Back" or "Wall_SideA" or "Wall_SideB";

                if (t.name.StartsWith("Wall_"))
                {
                    Material resolvedWallMaterial = isBathroomInteriorWall ? bathroomWallMaterial : wallMaterial;
                    if (resolvedWallMaterial == null)
                    {
                        continue;
                    }

                    renderer.sharedMaterial = resolvedWallMaterial;

                    // A cube's UVs always span 0-1 per face regardless of localScale, so
                    // without this every wall segment — corridor end caps, 6m room walls,
                    // 2m door-flanking strips — would show the exact same single tile.
                    // The thinnest axis (WallThickness) is never the visible face, so the
                    // other two localScale components are the actual width/height to tile
                    // across.
                    ApplyPerInstanceTiling(renderer, t.localScale, WallTextureMetersPerTile);
                    wallCount++;
                }
                else if (t.name == "DoorPanel")
                {
                    string hingeName = parentName;
                    Material resolvedDoorMaterial = hingeName switch
                    {
                        "Bathroom_Men_Door" => menBathroomDoorMaterial,
                        "Bathroom_Women_Door" => womenBathroomDoorMaterial,
                        _ => doorMaterial,
                    };

                    if (resolvedDoorMaterial != null)
                    {
                        // DoorWidth x WallHeight (2m x 3m) exactly matches every door
                        // texture's 2:3 aspect ratio, so this maps once with no tiling
                        // distortion.
                        renderer.sharedMaterial = resolvedDoorMaterial;
                        doorCount++;
                    }
                }
                else if (t.name == "Floor" || t.name.StartsWith("Floor_"))
                {
                    Material resolvedFloorMaterial = isBathroom ? bathroomFloorMaterial : carpetMaterial;
                    if (resolvedFloorMaterial == null)
                    {
                        continue;
                    }

                    renderer.sharedMaterial = resolvedFloorMaterial;
                    ApplyPerInstanceTiling(renderer, t.localScale, isBathroom ? WallTextureMetersPerTile : CarpetTextureMetersPerTile);
                    floorCount++;
                }
                else if (ceilingMaterial != null && (t.name == "Ceiling" || t.name.StartsWith("Ceiling_")))
                {
                    renderer.sharedMaterial = ceilingMaterial;
                    ApplyPerInstanceTiling(renderer, t.localScale, CeilingTextureMetersPerTile);
                    ceilingCount++;
                }
            }

            Debug.Log($"[Milestone9Level1AssetBuilder] Applied materials: {wallCount} walls, {doorCount} doors, {floorCount} floors, {ceilingCount} ceilings.");
        }

        /// <summary>Cube UVs always span 0-1 per face regardless of localScale — this makes a tiled material's repeat count match the object's actual real-world size instead of showing one stretched tile. The thinnest axis (wall/floor thickness) is never the visible face.</summary>
        private static void ApplyPerInstanceTiling(Renderer renderer, Vector3 localScale, float metersPerTile)
        {
            float[] dims = { localScale.x, localScale.y, localScale.z };
            System.Array.Sort(dims);
            Material instanceMaterial = renderer.material;
            instanceMaterial.mainTextureScale = new Vector2(dims[2] / metersPerTile, dims[1] / metersPerTile);
        }

        /// <summary>
        /// The scene had never had its lighting touched — still Unity's stock
        /// NewSceneSetup.DefaultGameObjects daytime Directional Light plus the default
        /// bright procedural Skybox, both plainly visible (and lighting the whole
        /// building) through the two open-air courtyards. Retunes the existing
        /// Directional Light into dim moonlight, swaps in a dark night skybox, and
        /// switches ambient lighting from skybox-derived to a fixed dark flat color so
        /// the baseline mood doesn't depend on skybox exposure quirks.
        /// </summary>
        private static LightningFlash BuildNightAmbiance()
        {
            LightningFlash lightningFlash = null;
            GameObject sunGo = GameObject.Find("Directional Light");
            if (sunGo != null)
            {
                var sunLight = sunGo.GetComponent<Light>();
                if (sunLight != null)
                {
                    sunLight.intensity = 0.12f;
                    sunLight.color = new Color(0.55f, 0.62f, 0.78f);
                }

                // Reuses the moonlight itself as the lightning source rather than adding
                // a second light — a flash brightening the same light that represents
                // the night sky reads correctly, and its SetStorming(false) path resets
                // straight back to this tuned moonlight intensity/color.
                lightningFlash = sunGo.AddComponent<LightningFlash>();
                var flashSo = new SerializedObject(lightningFlash);
                flashSo.FindProperty("_light").objectReferenceValue = sunLight;
                flashSo.FindProperty("_baseIntensity").floatValue = 0.12f;
                flashSo.ApplyModifiedPropertiesWithoutUndo();
            }

            Texture2D skyTexture = GenerateNightSkyTexture();
            EnsureFolder("Assets/TheEndlessRooms/Art/Textures");
            const string skyTexturePath = "Assets/TheEndlessRooms/Art/Textures/NightSky_Level1.asset";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(skyTexturePath) != null)
            {
                AssetDatabase.DeleteAsset(skyTexturePath);
            }

            AssetDatabase.CreateAsset(skyTexture, skyTexturePath);

            var skybox = new Material(Shader.Find("Skybox/Panoramic"));
            // Both property names set defensively — Skybox/Panoramic's main-texture
            // property name isn't 100% certain from memory alone; SetTexture on a
            // nonexistent property is a silent no-op, so this is cheap insurance rather
            // than risking an untextured skybox.
            skybox.SetTexture("_MainTex", skyTexture);
            skybox.SetTexture("_Tex", skyTexture);
            skybox.SetFloat("_Exposure", 1.1f);
            skybox.SetFloat("_Rotation", 0f);
            skybox.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            skybox.SetFloat("_Mapping", 1f); // Latitude Longitude Layout (equirectangular)
            skybox.SetFloat("_ImageType", 0f); // 360 Degrees
            skybox.SetFloat("_MirrorOnBack", 0f);

            EnsureFolder("Assets/TheEndlessRooms/Art/Materials");
            const string skyboxPath = "Assets/TheEndlessRooms/Art/Materials/NightSky_Level1.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(skyboxPath) != null)
            {
                AssetDatabase.DeleteAsset(skyboxPath);
            }

            AssetDatabase.CreateAsset(skybox, skyboxPath);

            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.035f, 0.05f);

            return lightningFlash;
        }

        /// <summary>
        /// Generates a simple equirectangular night-sky texture (dark gradient, scattered
        /// stars, wispy Perlin-noise clouds, one glowing full moon) purely in code — no
        /// such asset was provided, and this project has no 3D/HDRI asset import pipeline
        /// (see Level1FurnitureBuilder's doc comment for the same reasoning applied to
        /// furniture). Deterministic seed so rebuilds don't reshuffle the stars/moon.
        /// </summary>
        private static Texture2D GenerateNightSkyTexture()
        {
            const int width = 1024;
            const int height = 512;
            var pixels = new Color[width * height];
            var rng = new System.Random(1337);

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                float horizonGlow = Mathf.Clamp01(1f - Mathf.Abs(t - 0.55f) * 1.8f);
                Color baseColor = Color.Lerp(new Color(0.008f, 0.008f, 0.03f), new Color(0.05f, 0.06f, 0.11f), horizonGlow);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = baseColor;
                }
            }

            const int starCount = 1400;
            int starBandHeight = (int)(height * 0.72f);
            for (int i = 0; i < starCount; i++)
            {
                int x = rng.Next(width);
                int y = rng.Next(starBandHeight);
                float brightness = 0.55f + (float)rng.NextDouble() * 0.45f;
                pixels[y * width + x] = new Color(brightness, brightness, Mathf.Min(1f, brightness * 0.95f + 0.05f));
            }

            const float cloudScale = 5.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise(x / (float)width * cloudScale, y / (float)height * cloudScale);
                    float coverage = Mathf.SmoothStep(0.58f, 0.78f, noise);
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    int index = y * width + x;
                    pixels[index] = Color.Lerp(pixels[index], new Color(0.1f, 0.11f, 0.14f), coverage * 0.55f);
                }
            }

            Vector2 moonCenter = new(width * 0.64f, height * 0.24f);
            float moonRadius = height * 0.055f;
            float moonGlowRadius = moonRadius * 2.4f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), moonCenter);
                    int index = y * width + x;
                    if (dist <= moonRadius)
                    {
                        pixels[index] = new Color(0.95f, 0.94f, 0.87f);
                    }
                    else if (dist <= moonGlowRadius)
                    {
                        float glow = 1f - (dist - moonRadius) / (moonGlowRadius - moonRadius);
                        pixels[index] = Color.Lerp(pixels[index], new Color(0.8f, 0.82f, 0.78f), Mathf.Clamp01(glow) * 0.4f);
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Repeat,
            };
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Minimal textured-material helper for Level 1's own wall/door art (no normal map provided this round) — see Milestone8AssetBuilder.CreateTexturedMaterial for the fuller version used by the procedural rooms.</summary>
        private static Material CreateSimpleTexturedMaterial(string texturePath, string materialPath, bool mirrorHorizontal)
        {
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (albedo == null)
            {
                Debug.LogError($"[Milestone9Level1AssetBuilder] Could not find '{texturePath}'.");
                return null;
            }

            EnsureFolder("Assets/TheEndlessRooms/Art/Materials");

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", albedo);
            material.SetFloat("_Smoothness", 0.25f);
            // Negative X scale flips the U axis; wall tiling gets overwritten per-wall
            // right after this call anyway, so this default only actually matters for
            // the (non-tiled) door material.
            material.mainTextureScale = mirrorHorizontal ? new Vector2(-1f, 1f) : Vector2.one;

            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
            {
                AssetDatabase.DeleteAsset(materialPath);
            }

            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        // ---------------------------------------------------------------- corridor shell

        private static void BuildMainCorridor(Transform parent)
        {
            var corridorGo = new GameObject("MainCorridor");
            corridorGo.transform.SetParent(parent, false);

            float totalLength = Level1Layout.TotalRows * Level1Layout.RowSpacing;
            float centerZ = totalLength / 2f;

            CreateBlock(corridorGo.transform, "Floor", new Vector3(0f, 0f, centerZ), new Vector3(Level1Layout.CorridorWidth, WallThickness, totalLength));
            CreateBlock(corridorGo.transform, "Ceiling", new Vector3(0f, WallHeight, centerZ), new Vector3(Level1Layout.CorridorWidth, WallThickness, totalLength));

            CreateBlock(corridorGo.transform, "Wall_SouthEnd", new Vector3(0f, WallHeight / 2f, -WallThickness / 2f), new Vector3(Level1Layout.CorridorWidth, WallHeight, WallThickness));
            CreateBlock(corridorGo.transform, "Wall_NorthEnd", new Vector3(0f, WallHeight / 2f, totalLength + WallThickness / 2f), new Vector3(Level1Layout.CorridorWidth, WallHeight, WallThickness));
        }

        private static void BuildCrossCorridorArms(Transform parent)
        {
            var crossGo = new GameObject("CrossCorridorArms");
            crossGo.transform.SetParent(parent, false);

            float z = Level1Layout.RowCenterZ(Level1Layout.CrossCorridorRow);
            float mainEdge = Level1Layout.CorridorWidth / 2f;
            float armLength = Level1Layout.CorridorWidth;

            foreach (Side side in new[] { Side.West, Side.East })
            {
                float sign = side == Side.West ? -1f : 1f;
                float armCenterX = (mainEdge + armLength / 2f) * sign;

                CreateBlock(crossGo.transform, $"Floor_{side}", new Vector3(armCenterX, 0f, z), new Vector3(armLength, WallThickness, Level1Layout.CorridorWidth));
                CreateBlock(crossGo.transform, $"Ceiling_{side}", new Vector3(armCenterX, WallHeight, z), new Vector3(armLength, WallThickness, Level1Layout.CorridorWidth));

                CreateBlock(crossGo.transform, $"Wall_{side}_North", new Vector3(armCenterX, WallHeight / 2f, z + Level1Layout.CorridorWidth / 2f), new Vector3(armLength, WallHeight, WallThickness));
                CreateBlock(crossGo.transform, $"Wall_{side}_South", new Vector3(armCenterX, WallHeight / 2f, z - Level1Layout.CorridorWidth / 2f), new Vector3(armLength, WallHeight, WallThickness));
            }
        }

        // ---------------------------------------------------------------- offices

        private static void BuildOffice(Transform parent, OfficeSpec spec)
        {
            Vector3 center = spec.IsCrossArm ? Level1Layout.CrossArmRoomCenter(spec.Side) : Level1Layout.RoomCenter(spec.Row, spec.Side);
            var roomGo = new GameObject(spec.Id);
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            BuildRoomShellWithDoor(roomGo.transform, spec.Side, Level1Layout.RoomDepthX, Level1Layout.RoomWidthZ, out Door door, includeWindow: true);
            BuildOfficeFurniture(roomGo.transform, spec);
            BuildRoomLight(roomGo.transform);

            _officeDoors[spec.Id] = door;
        }

        private static readonly Dictionary<string, Door> _officeDoors = new();

        /// <summary>Offices and bathrooms had no light source at all — everything else in Level 1 (corridor, courtyards) does. A steady ceiling fixture, not the corridor's flickering warning kind, since these rooms are meant to be safely explorable.</summary>
        private static void BuildRoomLight(Transform room)
        {
            var lightGo = new GameObject("RoomLight");
            lightGo.transform.SetParent(room, false);
            lightGo.transform.localPosition = new Vector3(0f, WallHeight - 0.15f, 0f);
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 1.1f;
            light.color = new Color(0.95f, 0.93f, 0.85f);
        }

        /// <summary>
        /// Floor, ceiling, back wall, two side walls, and a door-gap front wall (split
        /// into permanently-solid left/right pieces flanking a DoorWidth gap — same
        /// "split wall" convention Milestone 7 established for the procedural rooms, so
        /// a closed door actually blocks the whole opening instead of leaving it walkable).
        /// All positions are room-local (center of the room = origin) before LocalToWorld
        /// mirrors them for East-side rooms.
        /// </summary>
        private static void BuildRoomShellWithDoor(Transform room, Side side, float depthX, float widthZ, out Door door, bool includeWindow = false)
        {
            Vector3 roomCenter = room.position;

            CreateBlockWorld(room, "Floor", roomCenter, new Vector3(depthX, WallThickness, widthZ));
            CreateBlockWorld(room, "Ceiling", roomCenter + Vector3.up * WallHeight, new Vector3(depthX, WallThickness, widthZ));

            if (includeWindow)
            {
                BuildBackWallWithWindow(room, roomCenter, side, depthX, widthZ);
            }
            else
            {
                Vector3 backWallLocal = new(-depthX / 2f, WallHeight / 2f, 0f);
                CreateBlockWorld(room, "Wall_Back", Level1Layout.LocalToWorld(roomCenter, side, backWallLocal), new Vector3(WallThickness, WallHeight, widthZ));
            }

            Vector3 sideWallLocalNear = new(0f, WallHeight / 2f, widthZ / 2f);
            Vector3 sideWallLocalFar = new(0f, WallHeight / 2f, -widthZ / 2f);
            CreateBlockWorld(room, "Wall_SideA", Level1Layout.LocalToWorld(roomCenter, side, sideWallLocalNear), new Vector3(depthX, WallHeight, WallThickness));
            CreateBlockWorld(room, "Wall_SideB", Level1Layout.LocalToWorld(roomCenter, side, sideWallLocalFar), new Vector3(depthX, WallHeight, WallThickness));

            float sideWidth = (widthZ - DoorWidth) / 2f;
            float sideOffset = sideWidth / 2f + DoorWidth / 2f;
            Vector3 frontWallLocal = new(depthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(room, "Wall_Front_Left", Level1Layout.LocalToWorld(roomCenter, side, frontWallLocal + new Vector3(0f, 0f, -sideOffset)), new Vector3(WallThickness, WallHeight, sideWidth));
            CreateBlockWorld(room, "Wall_Front_Right", Level1Layout.LocalToWorld(roomCenter, side, frontWallLocal + new Vector3(0f, 0f, sideOffset)), new Vector3(WallThickness, WallHeight, sideWidth));

            Vector3 doorBoundaryWorld = Level1Layout.LocalToWorld(roomCenter, side, new Vector3(depthX / 2f, 0f, 0f));
            door = PlaceDoor(room, doorBoundaryWorld, $"{room.name}_Door");
        }

        /// <summary>
        /// Splits the back wall (opposite the door) into four segments framing a window
        /// opening centered on it — same "split wall" idea the door gap already uses,
        /// just on all four sides of a hole instead of two.
        /// </summary>
        private static void BuildBackWallWithWindow(Transform room, Vector3 roomCenter, Side side, float depthX, float widthZ)
        {
            float backX = -depthX / 2f;
            float sideWidth = (widthZ - WindowOpeningWidth) / 2f;
            float sideOffset = sideWidth / 2f + WindowOpeningWidth / 2f;

            CreateBlockWorld(room, "Wall_Back_Left", Level1Layout.LocalToWorld(roomCenter, side, new Vector3(backX, WallHeight / 2f, -sideOffset)), new Vector3(WallThickness, WallHeight, sideWidth));
            CreateBlockWorld(room, "Wall_Back_Right", Level1Layout.LocalToWorld(roomCenter, side, new Vector3(backX, WallHeight / 2f, sideOffset)), new Vector3(WallThickness, WallHeight, sideWidth));

            float topOfOpening = WindowSillHeight + WindowOpeningHeight;
            float aboveHeight = WallHeight - topOfOpening;
            CreateBlockWorld(room, "Wall_Back_Below", Level1Layout.LocalToWorld(roomCenter, side, new Vector3(backX, WindowSillHeight / 2f, 0f)), new Vector3(WallThickness, WindowSillHeight, WindowOpeningWidth));
            CreateBlockWorld(room, "Wall_Back_Above", Level1Layout.LocalToWorld(roomCenter, side, new Vector3(backX, topOfOpening + aboveHeight / 2f, 0f)), new Vector3(WallThickness, aboveHeight, WindowOpeningWidth));

            Vector3 windowCenter = Level1Layout.LocalToWorld(roomCenter, side, new Vector3(backX, WindowSillHeight + WindowOpeningHeight / 2f, 0f));
            BuildWindow(room, windowCenter);
        }

        /// <summary>Steel frame + tinted glass sitting in the back-wall opening — nothing exists beyond that wall, so the glass looks straight out at the new night skybox (stars/moon), which is the point.</summary>
        private static void BuildWindow(Transform room, Vector3 worldCenter)
        {
            var windowGo = new GameObject("Window");
            windowGo.transform.SetParent(room, true);
            windowGo.transform.position = worldCenter;

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(windowGo.transform, false);
            frame.transform.position = worldCenter;
            frame.transform.localScale = new Vector3(0.08f, WindowHeight, WindowWidth);
            frame.GetComponent<Renderer>().sharedMaterial = GetOrCreateWindowFrameMaterial();

            var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Glass";
            glass.transform.SetParent(windowGo.transform, false);
            glass.transform.position = worldCenter;
            glass.transform.localScale = new Vector3(0.02f, WindowHeight - 0.15f, WindowWidth - 0.15f);
            glass.GetComponent<Renderer>().sharedMaterial = GetOrCreateWindowGlassMaterial();

            var blinds = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blinds.name = "Blinds";
            blinds.transform.SetParent(windowGo.transform, false);
            blinds.transform.position = worldCenter + new Vector3(0f, WindowHeight / 2f - 0.14f, 0f);
            blinds.transform.localScale = new Vector3(0.03f, 0.16f, WindowWidth - 0.2f);
            Object.DestroyImmediate(blinds.GetComponent<Collider>());
            blinds.GetComponent<Renderer>().sharedMaterial = GetOrCreateWindowFrameMaterial();
        }

        private static Material GetOrCreateWindowFrameMaterial()
        {
            const string path = "Assets/TheEndlessRooms/Art/Materials/WindowFrame_Level1.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.09f, 0.09f, 0.1f),
            };
            material.SetFloat("_Smoothness", 0.4f);
            material.SetFloat("_Metallic", 0.3f);

            EnsureFolder("Assets/TheEndlessRooms/Art/Materials");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material GetOrCreateWindowGlassMaterial()
        {
            return GetOrCreateUnlitTransparentMaterial("Assets/TheEndlessRooms/Art/Materials/WindowGlass_Level1.mat", new Color(0.55f, 0.65f, 0.7f, 0.3f));
        }

        /// <summary>
        /// Same hinge+panel construction as <see cref="ProceduralLevelBuilder.PlaceDoor"/>'s
        /// East/West case (the connecting wall runs along Z here, same as it does between
        /// rooms placed side-by-side along a procedural corridor) — reused by hand since
        /// this level isn't built by that system.
        /// </summary>
        private static Door PlaceDoor(Transform parent, Vector3 boundaryWorldPos, string doorName)
        {
            Vector3 hingePosition = boundaryWorldPos;
            hingePosition.z -= DoorWidth / 2f;

            var hinge = new GameObject(doorName);
            hinge.transform.SetParent(parent.parent, true);
            hinge.transform.position = hingePosition;

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            panel.transform.SetParent(hinge.transform, false);
            panel.transform.localPosition = new Vector3(0f, WallHeight / 2f, DoorWidth / 2f);
            panel.transform.localScale = new Vector3(WallThickness, WallHeight, DoorWidth);
            DebugColor.Apply(panel, DebugColor.Door);

            var door = hinge.AddComponent<Door>();
            door.Initialize(hinge.transform);
            // No explicit SaveId: hinge.name (doorName, e.g. "R01_Door") is already
            // unique per door, so the default name-based SaveId works as-is — same
            // reasoning as the secret room's door in Milestone8AssetBuilder.
            return door;
        }

        private static void BuildOfficeFurniture(Transform room, OfficeSpec spec)
        {
            Vector3 roomCenter = room.position;
            float floorY = WallThickness / 2f;

            // Local +X always points from the back wall into the room (see
            // Level1Layout.LocalToWorld); local +Z is unmirrored room width. Passing a
            // zero origin turns LocalToWorld into a pure local->world direction
            // transform, so furniture models (authored with local +Z = "front") end up
            // facing the right way regardless of which side of the corridor the office
            // is on.
            Vector3 intoRoomFromBackWall = Level1Layout.LocalToWorld(Vector3.zero, spec.Side, Vector3.right);
            Vector3 intoRoomFromSideWall = Level1Layout.LocalToWorld(Vector3.zero, spec.Side, Vector3.back);

            if (spec.UseMeetingTable)
            {
                // Centered in the room (not against a wall), rotated 90 degrees from the
                // first pass so its 2.4m length runs along room Z (the 6m-wide axis)
                // instead of room X (the 5m-deep, door-to-back-wall walking axis) —
                // leaves much more clearance to walk around it front-to-back.
                Vector3 tablePos = Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(0f, floorY, 0f));
                Level1FurnitureBuilder.BuildMeetingTable(room, "MeetingTable", tablePos, Quaternion.LookRotation(Vector3.forward));

                // Local +X and -X directions, mirrored per Side the same way position
                // offsets are — hardcoding Vector3.left/right here would face every chair
                // the wrong way in East-side rooms, where X is flipped.
                Quaternion facePositiveLocalXSide = Quaternion.LookRotation(Level1Layout.LocalToWorld(Vector3.zero, spec.Side, Vector3.left));
                Quaternion faceNegativeLocalXSide = Quaternion.LookRotation(Level1Layout.LocalToWorld(Vector3.zero, spec.Side, Vector3.right));

                float[] chairOffsets = { -0.8f, 0f, 0.8f };
                foreach (float offset in chairOffsets)
                {
                    Vector3 nearSideChairPos = Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(0.87f, floorY, offset));
                    Level1FurnitureBuilder.BuildChair(room, "MeetingChair", nearSideChairPos, facePositiveLocalXSide);

                    Vector3 farSideChairPos = Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(-0.87f, floorY, offset));
                    Level1FurnitureBuilder.BuildChair(room, "MeetingChair", farSideChairPos, faceNegativeLocalXSide);
                }
            }
            else
            {
                // Desk sits toward the room's middle rather than hugging the back wall,
                // rotated so a seated person looks across it toward the door (a classic
                // "see who's coming in" layout) — the chair goes on the desk's far side
                // from the door, tucked between the desk and the back wall.
                Vector3 deskPos = Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(OfficeDeskLocalX, floorY, 0f));
                GameObject desk = Level1FurnitureBuilder.BuildDesk(room, "Desk", deskPos, Quaternion.LookRotation(-intoRoomFromBackWall));
                AddHidingSpot(desk);

                Vector3 chairPos = Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(OfficeDeskLocalX - 0.7f, floorY, 0f));
                Level1FurnitureBuilder.BuildChair(room, "Chair", chairPos, Quaternion.LookRotation(intoRoomFromBackWall));
            }

            Vector3 closetPos = Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(-1.7f, floorY, -2.3f));
            GameObject closet = Level1FurnitureBuilder.BuildCloset(room, "Closet", closetPos, Quaternion.LookRotation(intoRoomFromBackWall));
            AddHidingSpot(closet);

            if (spec.HasShelf)
            {
                // -1.85 (not -2.15): the bookshelf's 0.9m width runs along room Z once
                // rotated to face the side wall, so its X-center needs enough clearance
                // from the back wall (X=-2.5) for that width's near edge not to clip it.
                Vector3 shelfPos = Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(-1.85f, floorY, 2.7f));
                Level1FurnitureBuilder.BuildBookshelf(room, "Shelf", shelfPos, Quaternion.LookRotation(intoRoomFromSideWall));
            }
        }

        // ---------------------------------------------------------------- bathrooms

        private static void BuildBathroom(Transform parent, int row, Side side, string name)
        {
            Vector3 center = Level1Layout.RoomCenter(row, side);
            var roomGo = new GameObject(name);
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            BuildRoomShellWithDoor(roomGo.transform, side, Level1Layout.RoomDepthX, Level1Layout.RoomWidthZ, out _);

            float floorY = WallThickness / 2f;
            Vector3 intoRoomFromBackWall = Level1Layout.LocalToWorld(Vector3.zero, side, Vector3.right);

            Vector3 toiletPos = Level1Layout.LocalToWorld(center, side, new Vector3(-1.85f, floorY, -2.0f));
            Level1FurnitureBuilder.BuildToilet(roomGo.transform, "Toilet", toiletPos, Quaternion.LookRotation(intoRoomFromBackWall));

            Vector3 sinkPos = Level1Layout.LocalToWorld(center, side, new Vector3(-1.85f, floorY, 2.0f));
            Level1FurnitureBuilder.BuildSink(roomGo.transform, "Sink", sinkPos, Quaternion.LookRotation(intoRoomFromBackWall));

            BuildRoomLight(roomGo.transform);
        }

        // ---------------------------------------------------------------- courtyards

        private static ParticleSystem BuildCourtyard(Transform parent, int row, Side side)
        {
            Vector3 center = Level1Layout.RoomCenter(row, side);
            var roomGo = new GameObject($"Courtyard_{side}");
            roomGo.transform.SetParent(parent, false);
            roomGo.transform.position = center;

            CreateBlockWorld(roomGo.transform, "Floor", center, new Vector3(Level1Layout.RoomDepthX, WallThickness, Level1Layout.RoomWidthZ));

            Vector3 backWallLocal = new(-Level1Layout.RoomDepthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(roomGo.transform, "Wall_Back", Level1Layout.LocalToWorld(center, side, backWallLocal), new Vector3(WallThickness, WallHeight, Level1Layout.RoomWidthZ));
            CreateBlockWorld(roomGo.transform, "Wall_SideA", Level1Layout.LocalToWorld(center, side, new Vector3(0f, WallHeight / 2f, Level1Layout.RoomWidthZ / 2f)), new Vector3(Level1Layout.RoomDepthX, WallHeight, WallThickness));
            CreateBlockWorld(roomGo.transform, "Wall_SideB", Level1Layout.LocalToWorld(center, side, new Vector3(0f, WallHeight / 2f, -Level1Layout.RoomWidthZ / 2f)), new Vector3(Level1Layout.RoomDepthX, WallHeight, WallThickness));

            AddFurniture(roomGo.transform, "PlanterPlaceholder", Level1Layout.LocalToWorld(center, side, new Vector3(-1.8f, 0.3f, 0f)), new Vector3(0.8f, 0.6f, 0.8f), hideable: false);

            return BuildCourtyardRain(roomGo.transform, center);
        }

        /// <summary>Off by default (playOnAwake=false, never .Play()'d here) — AttendantAppearanceController starts/stops it alongside its existing light-intensifying calls during Warning/Hunting.</summary>
        private static ParticleSystem BuildCourtyardRain(Transform parent, Vector3 center)
        {
            var rainGo = new GameObject("Rain");
            rainGo.transform.SetParent(parent, false);
            rainGo.transform.position = center + new Vector3(0f, WallHeight + 1.5f, 0f);

            var rain = rainGo.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = rain.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.2f;
            main.startSpeed = 0f;
            main.startSize = 0.03f;
            // Plain white + full alpha here — the rain material's own color/alpha (see
            // GetOrCreateRainMaterial) is the single source of truth for tint and
            // transparency, since whether a given shader multiplies material color by
            // particle vertex color isn't guaranteed, and doubling up two independent
            // alpha values would just make the rain fainter than intended for no reason.
            main.startColor = Color.white;
            main.maxParticles = 500;

            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = 260f;

            ParticleSystem.ShapeModule shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(Level1Layout.RoomDepthX, 0.1f, Level1Layout.RoomWidthZ);

            ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(-9f);

            var renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.05f;
            renderer.lengthScale = 2.5f;
            // Unity's default particle material is lit, so the scene's blue-tinted
            // moonlight/ambient was mixing with the rain's own tint into a purple-ish
            // result — an explicit unlit transparent material renders the intended blue
            // directly, with no lighting interaction to shift it.
            renderer.sharedMaterial = GetOrCreateRainMaterial();

            rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            return rain;
        }

        private static Material GetOrCreateRainMaterial()
        {
            return GetOrCreateUnlitTransparentMaterial("Assets/TheEndlessRooms/Art/Materials/Rain_Level1.mat", new Color(0.55f, 0.7f, 0.95f, 0.55f));
        }

        /// <summary>The plain Unlit shader (not a particle/glass-specific variant) — already used successfully elsewhere in this project (walls/doors), so its property names are known-good rather than guessed.</summary>
        private static Material GetOrCreateUnlitTransparentMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f); // Alpha blend
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            EnsureFolder("Assets/TheEndlessRooms/Art/Materials");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------------------------------------------------------------- level graph / Attendant

        private static GameObject BuildLevelGraph()
        {
            var levelGraphGo = new GameObject("Level1Graph");
            var builder = levelGraphGo.AddComponent<ProceduralLevelBuilder>();
            var so = new SerializedObject(builder);
            so.FindProperty("_cellSize").floatValue = Level1Layout.GraphCellSize;
            so.FindProperty("_buildOnStart").boolValue = false;
            // Level 1's floor (see BuildMainCorridor etc.) is a WallThickness-thick block
            // centered on Y=0, so its walkable top surface is at WallThickness/2, not the
            // ProceduralLevelBuilder default of Y=1 tuned for the old ModularRoomBase
            // prefab. The Attendant applies no gravity of its own, so this has to be
            // exact or it hovers/sinks relative to the real floor.
            so.FindProperty("_roomAnchorHeight").floatValue = WallThickness / 2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var providerGo = new GameObject("Level1RoomGraphProvider");
            var provider = providerGo.AddComponent<Level1RoomGraphProvider>();
            var providerSo = new SerializedObject(provider);
            providerSo.FindProperty("_levelBuilder").objectReferenceValue = builder;
            providerSo.ApplyModifiedPropertiesWithoutUndo();

            return levelGraphGo;
        }

        /// <summary>
        /// One ceiling light per main-corridor row, disabled by default (steady, normal
        /// light) — FlickeringLight only turns on as AttendantAppearanceController's
        /// warning cue, not as an always-on ambient effect the way Milestone 8's rooms
        /// use it.
        /// </summary>
        private static FlickeringLight[] BuildCorridorWarningLights(Transform parent)
        {
            var lightsGo = new GameObject("CorridorWarningLights");
            lightsGo.transform.SetParent(parent, false);

            var lights = new FlickeringLight[Level1Layout.TotalRows];
            for (int row = 1; row <= Level1Layout.TotalRows; row++)
            {
                var lightGo = new GameObject($"WarningLight_Row{row}");
                lightGo.transform.SetParent(lightsGo.transform, false);
                lightGo.transform.position = Level1Layout.CorridorCellCenter(row) + new Vector3(0f, WallHeight - 0.15f, 0f);
                lightGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 8f;
                light.color = new Color(0.85f, 0.9f, 1f);

                // Always enabled: FlickeringLight itself flickers gently as ambient
                // baseline atmosphere all the time now, and AttendantAppearanceController
                // only intensifies (not toggles) it during Warning/Hunting via SetIntensified.
                var flicker = lightGo.AddComponent<FlickeringLight>();
                // FlickeringLight.Update() drives Light.intensity every frame starting
                // from its own _baseIntensity field, not whatever Light.intensity was set
                // to above — has to be wired here directly, or the light stays at
                // FlickeringLight's serialized default (1) regardless of this dimmer,
                // darker-corridor target.
                var flickerSo = new SerializedObject(flicker);
                flickerSo.FindProperty("_baseIntensity").floatValue = 0.6f;
                flickerSo.ApplyModifiedPropertiesWithoutUndo();
                lights[row - 1] = flicker;
            }

            return lights;
        }

        /// <summary>
        /// Wires AttendantAppearanceController onto a dedicated manager object — the
        /// Attendant GameObject itself (built by Milestone7AssetBuilder.BuildAttendant,
        /// found by name here since that method doesn't hand back a reference) starts
        /// inactive; the appearance controller decides when it's actually present.
        /// </summary>
        private static void BuildAttendantAppearanceCycle(FlickeringLight[] warningLights, ParticleSystem[] courtyardRain, LightningFlash lightningFlash)
        {
            GameObject attendantGo = GameObject.Find("TheAttendant");
            if (attendantGo == null)
            {
                Debug.LogError("[Milestone9Level1AssetBuilder] Could not find 'TheAttendant' to wire the appearance cycle onto.");
                return;
            }

            var attendantController = attendantGo.GetComponent<AttendantController>();
            attendantGo.SetActive(false);

            var managerGo = new GameObject("AttendantAppearanceManager");
            var audioSource = managerGo.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;

            var thunderAudioSource = managerGo.AddComponent<AudioSource>();
            thunderAudioSource.spatialBlend = 0f;

            var appearance = managerGo.AddComponent<AttendantAppearanceController>();
            var so = new SerializedObject(appearance);
            so.FindProperty("_attendantGo").objectReferenceValue = attendantGo;
            so.FindProperty("_attendant").objectReferenceValue = attendantController;
            so.FindProperty("_warningAudioSource").objectReferenceValue = audioSource;
            so.FindProperty("_lightningFlash").objectReferenceValue = lightningFlash;
            so.FindProperty("_thunderAudioSource").objectReferenceValue = thunderAudioSource;
            so.FindProperty("_thunderSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ThunderClipPath);

            SerializedProperty lightsProp = so.FindProperty("_warningLights");
            lightsProp.arraySize = warningLights.Length;
            for (int i = 0; i < warningLights.Length; i++)
            {
                lightsProp.GetArrayElementAtIndex(i).objectReferenceValue = warningLights[i];
            }

            SerializedProperty rainProp = so.FindProperty("_rainEffects");
            rainProp.arraySize = courtyardRain.Length;
            for (int i = 0; i < courtyardRain.Length; i++)
            {
                rainProp.GetArrayElementAtIndex(i).objectReferenceValue = courtyardRain[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- player

        private struct ActionRefs
        {
            public InputActionReference Move;
            public InputActionReference Look;
            public InputActionReference Sprint;
            public InputActionReference Crouch;
            public InputActionReference Interact;
            public InputActionReference CycleInventoryNext;
            public InputActionReference CycleInventoryPrevious;
            public InputActionReference UseItem;
        }

        private static ActionRefs LoadInputActionReferences()
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath).OfType<InputActionReference>().ToList();

            InputActionReference Find(string actionName)
            {
                var reference = subAssets.FirstOrDefault(r => r.action.name == actionName);
                if (reference == null)
                {
                    Debug.LogError($"[Milestone9Level1AssetBuilder] Could not find action '{actionName}' in '{InputActionsPath}'.");
                }

                return reference;
            }

            return new ActionRefs
            {
                Move = Find("Move"),
                Look = Find("Look"),
                Sprint = Find("Sprint"),
                Crouch = Find("Crouch"),
                Interact = Find("Interact"),
                CycleInventoryNext = Find("CycleInventoryNext"),
                CycleInventoryPrevious = Find("CycleInventoryPrevious"),
                UseItem = Find("UseItem"),
            };
        }

        private struct CollectibleItems
        {
            public InventoryItemDefinition Battery;
            public InventoryItemDefinition Cassette;
            public InventoryItemDefinition CassetteRecorder;
            public InventoryItemDefinition UvFlashlight;
            public InventoryItemDefinition Flashlight;
            public InventoryItemDefinition GoldenKey;
            public InventoryItemDefinition BronzeKey;
            public InventoryItemDefinition IdCard;
        }

        private static CollectibleItems LoadOrCreateCollectibleItems()
        {
            EnsureFolder(ItemsFolder);

            InventoryItemDefinition Create(string fileName, string itemId, string displayName, string description)
            {
                string path = $"{ItemsFolder}/{fileName}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);
                if (existing != null)
                {
                    return existing;
                }

                var item = ScriptableObject.CreateInstance<InventoryItemDefinition>();
                item.ItemId = itemId;
                item.DisplayName = displayName;
                item.Description = description;
                AssetDatabase.CreateAsset(item, path);
                return item;
            }

            return new CollectibleItems
            {
                Battery = Create("Battery", "battery", "D Battery", "A worn D-cell battery. Powers small electronics."),
                Cassette = Create("Cassette", "cassette", "Audio Cassette", "A labeled cassette tape. Needs a player."),
                CassetteRecorder = Create("CassetteRecorder", "cassette_recorder", "Cassette Recorder", "A portable cassette recorder. Needs a tape."),
                UvFlashlight = Create("UvFlashlight", "uv_flashlight", "UV Flashlight", "Reveals markings invisible under normal light. Needs batteries."),
                Flashlight = Create("Flashlight", "flashlight", "Flashlight", "A dependable flashlight."),
                GoldenKey = Create("GoldenKey", "golden_key", "Golden Key", "An ornate golden key."),
                BronzeKey = Create("BronzeKey", "bronze_key", "Bronze Key", "A plain bronze key."),
                IdCard = Create("IdCard", "id_card", "Office ID Card", "An employee ID card. Probably still opens a few doors around here."),
            };
        }

        private static GameObject BuildPlayer(PlayerMovementConfig config, ActionRefs actionRefs, CollectibleItems items, out InteractionCaster interactionCaster, out CameraShakeEffect cameraShake, out Inventory inventory, out InventorySelectionController selectionController)
        {
            var playerGo = new GameObject("Player") { tag = "Player" };

            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = config.StandingHeight;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, config.StandingHeight / 2f, 0f);

            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerGo.transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);

            // Milestone 9 fix: the earlier pass omitted this entirely (same gap found
            // and fixed in Milestone8AssetBuilder's PC rig) — without a CameraShakeEffect
            // component somewhere under the player, AttendantController.HandleCapture's
            // shake intensity has nothing to apply to, so getting caught feels like
            // nothing happened even once a real capture consequence exists.
            var shakeAnchor = new GameObject("CameraShakeAnchor").transform;
            shakeAnchor.SetParent(cameraPivot, false);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(shakeAnchor, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();

            cameraShake = shakeAnchor.gameObject.AddComponent<CameraShakeEffect>();
            var shakeSo = new SerializedObject(cameraShake);
            shakeSo.FindProperty("_shakeTarget").objectReferenceValue = cameraGo.transform;
            shakeSo.ApplyModifiedPropertiesWithoutUndo();

            var playerController = playerGo.AddComponent<PlayerController>();
            var controllerSo = new SerializedObject(playerController);
            controllerSo.FindProperty("_config").objectReferenceValue = config;
            controllerSo.FindProperty("_moveAction").objectReferenceValue = actionRefs.Move;
            controllerSo.FindProperty("_lookAction").objectReferenceValue = actionRefs.Look;
            controllerSo.FindProperty("_sprintAction").objectReferenceValue = actionRefs.Sprint;
            controllerSo.FindProperty("_crouchAction").objectReferenceValue = actionRefs.Crouch;
            controllerSo.FindProperty("_cameraPivot").objectReferenceValue = cameraPivot;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            interactionCaster = playerGo.AddComponent<InteractionCaster>();
            var casterSo = new SerializedObject(interactionCaster);
            casterSo.FindProperty("_viewCamera").objectReferenceValue = camera;
            casterSo.FindProperty("_interactAction").objectReferenceValue = actionRefs.Interact;
            casterSo.ApplyModifiedPropertiesWithoutUndo();

            inventory = playerGo.AddComponent<Inventory>();
            var inventorySo = new SerializedObject(inventory);
            SerializedProperty catalogProp = inventorySo.FindProperty("_itemCatalog");
            var catalogItems = new[] { items.Battery, items.Cassette, items.CassetteRecorder, items.UvFlashlight, items.Flashlight, items.GoldenKey, items.BronzeKey, items.IdCard };
            catalogProp.arraySize = catalogItems.Length;
            for (int i = 0; i < catalogItems.Length; i++)
            {
                catalogProp.GetArrayElementAtIndex(i).objectReferenceValue = catalogItems[i];
            }

            inventorySo.ApplyModifiedPropertiesWithoutUndo();

            selectionController = playerGo.AddComponent<InventorySelectionController>();
            var selectionSo = new SerializedObject(selectionController);
            selectionSo.FindProperty("_inventory").objectReferenceValue = inventory;
            selectionSo.FindProperty("_cycleNextAction").objectReferenceValue = actionRefs.CycleInventoryNext;
            selectionSo.FindProperty("_cyclePreviousAction").objectReferenceValue = actionRefs.CycleInventoryPrevious;
            selectionSo.FindProperty("_useItemAction").objectReferenceValue = actionRefs.UseItem;
            selectionSo.ApplyModifiedPropertiesWithoutUndo();

            // Both beams live on the camera so they always point wherever the player is
            // looking, same as a handheld light would.
            Light flashlightBeam = BuildHandheldBeam(cameraGo.transform, "FlashlightBeam", new Color(1f, 0.97f, 0.85f), 12f, 45f);
            Light uvBeam = BuildHandheldBeam(cameraGo.transform, "UvBeam", new Color(0.55f, 0.15f, 0.95f), 8f, 35f);

            var flashlight = playerGo.AddComponent<PlayerFlashlight>();
            var flashlightSo = new SerializedObject(flashlight);
            flashlightSo.FindProperty("_flashlightItem").objectReferenceValue = items.Flashlight;
            flashlightSo.FindProperty("_inventory").objectReferenceValue = inventory;
            flashlightSo.FindProperty("_beam").objectReferenceValue = flashlightBeam;
            flashlightSo.ApplyModifiedPropertiesWithoutUndo();

            var uvFlashlight = playerGo.AddComponent<PlayerUvFlashlight>();
            var uvFlashlightSo = new SerializedObject(uvFlashlight);
            uvFlashlightSo.FindProperty("_uvFlashlightItem").objectReferenceValue = items.UvFlashlight;
            uvFlashlightSo.FindProperty("_batteryItem").objectReferenceValue = items.Battery;
            uvFlashlightSo.FindProperty("_inventory").objectReferenceValue = inventory;
            uvFlashlightSo.FindProperty("_beam").objectReferenceValue = uvBeam;
            uvFlashlightSo.ApplyModifiedPropertiesWithoutUndo();

            var cassettePlayer = playerGo.AddComponent<CassetteMessagePlayer>();
            var cassettePlayerSo = new SerializedObject(cassettePlayer);
            cassettePlayerSo.FindProperty("_cassetteItem").objectReferenceValue = items.Cassette;
            cassettePlayerSo.FindProperty("_recorderItem").objectReferenceValue = items.CassetteRecorder;
            cassettePlayerSo.FindProperty("_inventory").objectReferenceValue = inventory;
            cassettePlayerSo.FindProperty("_messageText").stringValue = CassetteMessageText;
            cassettePlayerSo.ApplyModifiedPropertiesWithoutUndo();

            return playerGo;
        }

        private static Light BuildHandheldBeam(Transform cameraTransform, string name, Color color, float range, float spotAngle)
        {
            var beamGo = new GameObject(name);
            beamGo.transform.SetParent(cameraTransform, false);

            var beam = beamGo.AddComponent<Light>();
            beam.type = LightType.Spot;
            beam.color = color;
            beam.range = range;
            beam.spotAngle = spotAngle;
            beam.intensity = 2.5f;
            beam.enabled = false;
            return beam;
        }

        // ---------------------------------------------------------------- exit

        private static void BuildExitPoint(InventoryItemDefinition requiredItem)
        {
            float z = Level1Layout.RowCenterZ(Level1Layout.TotalRows) + Level1Layout.RowSpacing / 2f - 1f;
            var exitGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            exitGo.name = "ExitPoint";
            exitGo.transform.position = new Vector3(0f, 1f, z);
            exitGo.transform.localScale = Vector3.one * 0.6f;
            var exitPoint = exitGo.AddComponent<ExitPoint>();
            var so = new SerializedObject(exitPoint);
            so.FindProperty("_requiredItem").objectReferenceValue = requiredItem;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- jump scares

        private static void BuildJumpScares()
        {
            AddJumpScare("R07");
            AddJumpScare("R09");
        }

        private static void AddJumpScare(string roomId)
        {
            GameObject room = GameObject.Find(roomId);
            if (room == null)
            {
                return;
            }

            var triggerGo = new GameObject($"JumpScare_{roomId}");
            triggerGo.transform.SetParent(room.transform, false);
            triggerGo.transform.localPosition = Vector3.zero;
            var collider = triggerGo.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(Level1Layout.RoomDepthX, WallHeight, Level1Layout.RoomWidthZ);
            triggerGo.AddComponent<AudioSource>();

            var visual = new GameObject("ScareVisual");
            visual.transform.SetParent(triggerGo.transform, false);
            visual.transform.localPosition = new Vector3(1.5f, WallThickness / 2f, 0f); // floor's actual walkable top surface, not its Y=0 center

            // BottomCenter alignment so this transform sits at the figure's feet (floor level).
            Sprite sprite = EditorSpriteImportUtility.LoadOrImportSprite(JumpScareSpritePath, JumpScareSpritePixelsPerUnit, SpriteAlignment.BottomCenter);
            if (sprite != null)
            {
                var spriteRenderer = visual.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                visual.AddComponent<BillboardSprite>();
            }

            var jumpScare = triggerGo.AddComponent<JumpScareTrigger>();
            var so = new SerializedObject(jumpScare);
            so.FindProperty("_scareVisual").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- collectible items / progression

        private const string SafeCode = "2187";

        private static Side GetOfficeSide(string roomId)
        {
            foreach (OfficeSpec spec in Level1Layout.Offices)
            {
                if (spec.Id == roomId)
                {
                    return spec.Side;
                }
            }

            return Side.West;
        }

        /// <summary>West couch + 2 side tables on the west arm of the cross corridor, mirrored on the east arm — the normal Flashlight sits in one of the west side tables (req. 1); everything else there is dressing.</summary>
        private static void BuildCentralCorridorFurniture(CollectibleItems items)
        {
            GameObject crossCorridor = GameObject.Find("CrossCorridorArms");
            Transform parent = crossCorridor != null ? crossCorridor.transform : null;

            foreach (Side side in new[] { Side.West, Side.East })
            {
                Vector3 cellCenter = Level1Layout.CrossArmCorridorCellCenter(side);
                float wallSign = -1f; // couches back onto the arm's south wall, facing north into the corridor
                Vector3 couchPos = cellCenter + new Vector3(0f, 0f, wallSign * (Level1Layout.CorridorWidth / 2f - 0.9f));
                Level1FurnitureBuilder.BuildCouch(parent, $"Couch_{side}", couchPos, Quaternion.identity);

                Vector3 tableLeftPos = couchPos + new Vector3(-1.15f, 0f, 0f);
                Vector3 tableRightPos = couchPos + new Vector3(1.15f, 0f, 0f);
                GameObject tableLeft = Level1FurnitureBuilder.BuildSideTable(parent, $"SideTable_{side}_A", tableLeftPos, Quaternion.identity);
                Level1FurnitureBuilder.BuildSideTable(parent, $"SideTable_{side}_B", tableRightPos, Quaternion.identity);

                if (side == Side.West)
                {
                    Vector3 flashlightPos = tableLeftPos + new Vector3(0f, 0.5f + 0.03f, 0f);
                    GameObject flashlightGo = Level1ItemModelBuilder.BuildFlashlight(tableLeft.transform, items.Flashlight.DisplayName, flashlightPos, Quaternion.Euler(0f, 90f, 0f), out _);
                    AddPickup(flashlightGo, items.Flashlight);
                }
            }
        }

        /// <summary>
        /// Room-by-room placement for the full item/lock/clue chain (see
        /// docs/features/milestone-9-playable-office-levels.md for the design):
        /// R01 ID Card -> (R07 UV Flashlight door, R08 Cassette drawer) -> tape reveals
        /// R09 Bronze Key -> R02 Battery drawer -> combine -> Male Bathroom code ->
        /// R12 keypad safe -> Golden Key -> exit. R03/R05/R06/R10/R11/R13/R14 are
        /// lore/red-herring rooms with no spider clue.
        /// </summary>
        private static void BuildProgressionContent(CollectibleItems items, Inventory inventory)
        {
            var uvPoweredGate = new GameObject("UvPoweredGate").AddComponent<UvPoweredGate>();
            var uvCodeKnownGate = new GameObject("UvCodeKnownGate").AddComponent<UvCodeKnownGate>();

            // R01 — ID Card, sitting openly on the desk.
            if (TryGetRoom("R01", out Transform r01, out Vector3 r01Center, out Side r01Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r01Center, r01Side, new Vector3(OfficeDeskLocalX, 0.86f, 0f));
                GameObject idCardGo = Level1ItemModelBuilder.BuildIdCard(r01, items.IdCard.DisplayName, pos, Quaternion.identity);
                AddPickup(idCardGo, items.IdCard);
                BuildSpiderWeb(r01, r01Center, r01Side, inventory, items.IdCard, null, null, null);
            }

            // R02 — Battery locked in a drawer that only the Bronze Key opens.
            if (TryGetRoom("R02", out Transform r02, out Vector3 r02Center, out Side r02Side))
            {
                Vector3 cabinetPos = Level1Layout.LocalToWorld(r02Center, r02Side, new Vector3(1.3f, WallThickness / 2f, -1.0f));
                Quaternion facing = Quaternion.LookRotation(Level1Layout.LocalToWorld(Vector3.zero, r02Side, Vector3.right));
                GameObject cabinet = Level1FurnitureBuilder.BuildLockableDrawerCabinet(r02, "BatteryCabinet", cabinetPos, facing);

                GameObject batteryGo = Level1ItemModelBuilder.BuildBattery(cabinet.transform, items.Battery.DisplayName, cabinetPos + facing * new Vector3(0f, 0.31f, 0.28f), facing);
                AddPickup(batteryGo, items.Battery);

                AddLockableDrawer(cabinet, items.BronzeKey, consume: true, batteryGo);
                BuildSpiderWeb(r02, r02Center, r02Side, inventory, items.Battery, items.BronzeKey, null, null);
            }

            // R03 — lore only, no lock, no clue.
            if (TryGetRoom("R03", out Transform r03, out Vector3 r03Center, out Side r03Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r03Center, r03Side, new Vector3(OfficeDeskLocalX, 0.865f, -0.35f));
                SpawnLoreNote(r03, pos, "Read Note", "\"Third work order this month for the R03 radiator. Nobody's coming to fix it. Nobody's coming for any of it.\"");
            }

            // R04 — Recorder, sitting openly (meeting-table room, no desk).
            if (TryGetRoom("R04", out Transform r04, out Vector3 r04Center, out Side r04Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r04Center, r04Side, new Vector3(1.3f, WallThickness / 2f + 0.06f, 1.0f));
                GameObject recorderGo = Level1ItemModelBuilder.BuildCassetteRecorder(r04, items.CassetteRecorder.DisplayName, pos, Quaternion.identity);
                AddPickup(recorderGo, items.CassetteRecorder);
                BuildSpiderWeb(r04, r04Center, r04Side, inventory, items.CassetteRecorder, null, null, null);
            }

            // R05 — red herring: ID Card also opens this drawer, but it's empty besides a flavor note. No clue web (red herrings are indistinguishable from real progression rooms until searched).
            if (TryGetRoom("R05", out Transform r05, out Vector3 r05Center, out Side r05Side))
            {
                Vector3 cabinetPos = Level1Layout.LocalToWorld(r05Center, r05Side, new Vector3(1.3f, WallThickness / 2f, -1.0f));
                Quaternion facing = Quaternion.LookRotation(Level1Layout.LocalToWorld(Vector3.zero, r05Side, Vector3.right));
                GameObject cabinet = Level1FurnitureBuilder.BuildLockableDrawerCabinet(r05, "RedHerringCabinet", cabinetPos, facing);
                GameObject badgeNote = BuildLoreNoteGameObject(cabinet.transform, cabinetPos + facing * new Vector3(0f, 0.31f, 0.28f), "Look Inside", "Just a dusty old employee badge, long expired. Not what you were hoping for.");
                AddLockableDrawer(cabinet, items.IdCard, consume: false, badgeNote);
            }

            // R06 — lore only.
            if (TryGetRoom("R06", out Transform r06, out Vector3 r06Center, out Side r06Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r06Center, r06Side, new Vector3(OfficeDeskLocalX, 0.865f, -0.35f));
                SpawnLoreNote(r06, pos, "Read File", "A personnel file, water-damaged past reading — only the header survives: \"...TERMINATION PENDING...\"");
            }

            // R07 — UV Flashlight, sitting openly, but the room door itself needs the ID Card (reusable, not consumed).
            if (TryGetRoom("R07", out Transform r07, out Vector3 r07Center, out Side r07Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r07Center, r07Side, new Vector3(OfficeDeskLocalX, 0.86f, 0f));
                GameObject uvGo = Level1ItemModelBuilder.BuildUvFlashlight(r07, items.UvFlashlight.DisplayName, pos, Quaternion.identity, out _);
                AddPickup(uvGo, items.UvFlashlight);
                BuildSpiderWeb(r07, r07Center, r07Side, inventory, items.UvFlashlight, null, null, null);

                if (_officeDoors.TryGetValue("R07", out Door r07Door))
                {
                    r07Door.SetRequiredItem(items.IdCard, consume: false);
                    r07Door.RestoreState(new Door.DoorState(isOpen: false, isLocked: true));
                }
            }

            // R08 — Cassette locked in a drawer, also opened by the ID Card.
            if (TryGetRoom("R08", out Transform r08, out Vector3 r08Center, out Side r08Side))
            {
                Vector3 cabinetPos = Level1Layout.LocalToWorld(r08Center, r08Side, new Vector3(1.3f, WallThickness / 2f, -1.0f));
                Quaternion facing = Quaternion.LookRotation(Level1Layout.LocalToWorld(Vector3.zero, r08Side, Vector3.right));
                GameObject cabinet = Level1FurnitureBuilder.BuildLockableDrawerCabinet(r08, "CassetteCabinet", cabinetPos, facing);

                GameObject cassetteGo = Level1ItemModelBuilder.BuildCassette(cabinet.transform, items.Cassette.DisplayName, cabinetPos + facing * new Vector3(0f, 0.31f, 0.28f), facing);
                AddPickup(cassetteGo, items.Cassette);

                AddLockableDrawer(cabinet, items.IdCard, consume: false, cassetteGo);
                BuildSpiderWeb(r08, r08Center, r08Side, inventory, items.Cassette, items.IdCard, null, null);
            }

            // R09 — Bronze Key, sitting openly (its significance is only explained once the tape is heard, but nothing physically stops picking it up early).
            if (TryGetRoom("R09", out Transform r09, out Vector3 r09Center, out Side r09Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r09Center, r09Side, new Vector3(OfficeDeskLocalX, 0.86f, 0f));
                GameObject bronzeGo = Level1ItemModelBuilder.BuildBronzeKey(r09, items.BronzeKey.DisplayName, pos, Quaternion.identity);
                AddPickup(bronzeGo, items.BronzeKey);
                BuildSpiderWeb(r09, r09Center, r09Side, inventory, items.BronzeKey, null, null, null);
            }

            // R10 — red herring: a decorative locked-looking cabinet with no actual lock component (never interactable), plus a note with a fake code. No clue web.
            if (TryGetRoom("R10", out Transform r10, out Vector3 r10Center, out Side r10Side))
            {
                Vector3 cabinetPos = Level1Layout.LocalToWorld(r10Center, r10Side, new Vector3(1.3f, WallThickness / 2f, -1.0f));
                Quaternion facing = Quaternion.LookRotation(Level1Layout.LocalToWorld(Vector3.zero, r10Side, Vector3.right));
                Level1FurnitureBuilder.BuildLockableDrawerCabinet(r10, "DecorativeCabinet", cabinetPos, facing);

                Vector3 notePos = Level1Layout.LocalToWorld(r10Center, r10Side, new Vector3(OfficeDeskLocalX, 0.865f, -0.35f));
                SpawnLoreNote(r10, notePos, "Read Note", "A scrap taped to the desk: \"the code is 4471, don't forget it this time\" — someone's handwriting, but it doesn't look right somehow.");
            }

            // R11 — lore only, near the exit and the female bathroom.
            if (TryGetRoom("R11", out Transform r11, out Vector3 r11Center, out Side r11Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r11Center, r11Side, new Vector3(OfficeDeskLocalX, 0.865f, -0.35f));
                SpawnLoreNote(r11, pos, "Read Note", "\"Almost to the stairwell. Whatever's been fixing this place, it's never once let anyone actually leave.\"");
            }

            // R12 — the keypad safe, holding the Golden Key.
            if (TryGetRoom("R12", out Transform r12, out Vector3 r12Center, out Side r12Side))
            {
                Vector3 safePos = Level1Layout.LocalToWorld(r12Center, r12Side, new Vector3(1.3f, WallThickness / 2f, 0f));
                Quaternion facing = Quaternion.LookRotation(Level1Layout.LocalToWorld(Vector3.zero, r12Side, Vector3.right));
                GameObject safeGo = Level1FurnitureBuilder.BuildKeypadSafe(r12, "KeypadSafe", safePos, facing);

                GameObject goldenGo = Level1ItemModelBuilder.BuildGoldenKey(safeGo.transform, items.GoldenKey.DisplayName, safePos + facing * new Vector3(0f, 0.3f, 0.3f), facing);
                AddPickup(goldenGo, items.GoldenKey);
                goldenGo.SetActive(false);

                var safe = safeGo.AddComponent<KeypadSafe>();
                var safeSo = new SerializedObject(safe);
                safeSo.FindProperty("_code").stringValue = SafeCode;
                safeSo.FindProperty("_revealedContent").objectReferenceValue = goldenGo;
                safeSo.ApplyModifiedPropertiesWithoutUndo();

                BuildSpiderWeb(r12, r12Center, r12Side, inventory, items.GoldenKey, null, uvCodeKnownGate, null);
            }

            // R13/R14 — bonus lore-only rooms at the ends of the cross arm.
            if (TryGetRoom("R13", out Transform r13, out Vector3 r13Center, out Side r13Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r13Center, r13Side, new Vector3(0f, 0.865f, 0f));
                SpawnLoreNote(r13, pos, "Read Note", "\"West stairwell's been sealed since before I started. Nobody says why.\"");
            }

            if (TryGetRoom("R14", out Transform r14, out Vector3 r14Center, out Side r14Side))
            {
                Vector3 pos = Level1Layout.LocalToWorld(r14Center, r14Side, new Vector3(0f, 0.865f, 0f));
                SpawnLoreNote(r14, pos, "Read Note", "Meeting minutes, dated years ago, for a project with no name — every line item redacted but one: \"maintenance continues.\"");
            }

            BuildUvRevealedBathroomClue(uvPoweredGate);
        }

        /// <summary>A FieldNote that's completely inert until the UV flashlight is switched on — UvRevealedProp (which has to live on an always-active wrapper, see its own doc comment) toggles the actual note active/inactive.</summary>
        private static void BuildUvRevealedBathroomClue(UvPoweredGate uvPoweredGate)
        {
            GameObject bathroom = GameObject.Find("Bathroom_Men");
            if (bathroom == null)
            {
                return;
            }

            Vector3 clueWorldPos = Level1Layout.LocalToWorld(bathroom.transform.position, Side.East, new Vector3(-2.35f, 1.2f, 2.0f));
            GameObject clueGo = BuildLoreNoteGameObject(bathroom.transform, clueWorldPos, "Read Scrawled Code",
                $"Scrawled faintly on the wall, only visible under UV light:\n\n\"{string.Join(" - ", SafeCode.ToCharArray())}\"");
            clueGo.SetActive(false);

            var wrapperGo = new GameObject("UvClueWrapper");
            wrapperGo.transform.SetParent(bathroom.transform, true);
            var revealed = wrapperGo.AddComponent<UvRevealedProp>();
            var revealedSo = new SerializedObject(revealed);
            revealedSo.FindProperty("_target").objectReferenceValue = clueGo;
            revealedSo.ApplyModifiedPropertiesWithoutUndo();

            KeypadSafe r12Safe = GameObject.Find("R12")?.GetComponentInChildren<KeypadSafe>(true);
            BuildSpiderWeb(bathroom.transform, bathroom.transform.position, Side.East, null, null, null, uvPoweredGate, r12Safe);
        }

        // ---------------------------------------------------------------- progression helpers

        private static bool TryGetRoom(string roomId, out Transform room, out Vector3 center, out Side side)
        {
            GameObject go = GameObject.Find(roomId);
            if (go == null)
            {
                Debug.LogError($"[Milestone9Level1AssetBuilder] Could not find room '{roomId}' for progression content.");
                room = null;
                center = Vector3.zero;
                side = Side.West;
                return false;
            }

            room = go.transform;
            center = go.transform.position;
            side = GetOfficeSide(roomId);
            return true;
        }

        private static void AddPickup(GameObject go, InventoryItemDefinition item)
        {
            var pickup = go.AddComponent<InventoryPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("_item").objectReferenceValue = item;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Wires a LockableDrawer onto an already-built cabinet prop's root, revealing (activating) revealedContent once unlocked. revealedContent starts inactive here since LockableDrawer.Awake (which also does this) never runs during headless Editor scripting.</summary>
        private static LockableDrawer AddLockableDrawer(GameObject cabinetRoot, InventoryItemDefinition requiredItem, bool consume, GameObject revealedContent)
        {
            var drawer = cabinetRoot.AddComponent<LockableDrawer>();
            drawer.Configure(requiredItem, consume, revealedContent);
            return drawer;
        }

        private static GameObject BuildLoreNoteGameObject(Transform parent, Vector3 worldPosition, string promptLabel, string text)
        {
            var noteGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noteGo.name = "Note";
            noteGo.transform.SetParent(parent, true);
            noteGo.transform.position = worldPosition;
            noteGo.transform.localScale = new Vector3(0.22f, 0.02f, 0.28f);
            DebugColor.Apply(noteGo, DebugColor.Note);

            var note = noteGo.AddComponent<FieldNote>();
            var noteSo = new SerializedObject(note);
            noteSo.FindProperty("_promptLabel").stringValue = promptLabel;
            noteSo.FindProperty("_fragmentText").stringValue = text;
            noteSo.ApplyModifiedPropertiesWithoutUndo();

            return noteGo;
        }

        private static void SpawnLoreNote(Transform room, Vector3 worldPosition, string promptLabel, string text)
        {
            BuildLoreNoteGameObject(room, worldPosition, promptLabel, text);
        }

        /// <summary>
        /// Mounts the placeholder spider-web clue near a back-wall ceiling corner, facing
        /// into the room, and wires a SpiderWebClue logic component to it. Pass null for
        /// any dependency this particular clue doesn't need.
        /// </summary>
        private static void BuildSpiderWeb(Transform room, Vector3 roomCenter, Side side, Inventory inventory, InventoryItemDefinition obtainedItem, InventoryItemDefinition requiredItem, MonoBehaviour showGate, MonoBehaviour dismissGate)
        {
            Vector3 intoRoom = Level1Layout.LocalToWorld(Vector3.zero, side, Vector3.right);
            Vector3 localOffset = new(-Level1Layout.RoomDepthX / 2f + 0.04f, WallHeight - 0.3f, Level1Layout.RoomWidthZ / 2f - 0.4f);
            Vector3 worldPos = Level1Layout.LocalToWorld(roomCenter, side, localOffset);

            Level1ItemModelBuilder.BuildSpiderWebPlaceholder(room, "SpiderWebVisual", worldPos, Quaternion.LookRotation(intoRoom), out GameObject webOnly, out GameObject spider);

            // SpiderWebClue.Refresh() (which sets the correct active state) only runs
            // from OnEnable, which never fires during headless Editor scripting — so the
            // freshly-saved scene needs its own copy of that same "everything starts
            // unowned/undiscovered" initial computation, or both visuals would be left
            // active (as CreatePrimitive/Box leaves them) until Play mode first ticks.
            bool startsWithSpider = obtainedItem != null && requiredItem == null && showGate == null;
            spider.SetActive(startsWithSpider);
            webOnly.SetActive(!startsWithSpider);

            var clueGo = new GameObject("SpiderWebClue");
            clueGo.transform.SetParent(room, true);
            var clue = clueGo.AddComponent<SpiderWebClue>();
            var so = new SerializedObject(clue);
            so.FindProperty("_inventory").objectReferenceValue = inventory;
            so.FindProperty("_obtainedItem").objectReferenceValue = obtainedItem;
            so.FindProperty("_requiredItem").objectReferenceValue = requiredItem;
            so.FindProperty("_showGateBehaviour").objectReferenceValue = showGate;
            so.FindProperty("_dismissGateBehaviour").objectReferenceValue = dismissGate;
            so.FindProperty("_spiderVisual").objectReferenceValue = spider;
            so.FindProperty("_webOnlyVisual").objectReferenceValue = webOnly;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- UI

        private static void BuildInteractionPromptUi(InteractionCaster interactionCaster)
        {
            var canvasGo = new GameObject("PromptCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var promptRoot = new GameObject("PromptRoot");
            promptRoot.transform.SetParent(canvasGo.transform, false);
            var promptRootRect = promptRoot.AddComponent<RectTransform>();
            promptRootRect.anchorMin = new Vector2(0.5f, 0.15f);
            promptRootRect.anchorMax = new Vector2(0.5f, 0.15f);
            promptRootRect.sizeDelta = new Vector2(400f, 40f);

            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(promptRoot.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 24;
            text.color = Color.white;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            promptRoot.SetActive(false);

            var promptUi = canvasGo.AddComponent<InteractionPromptUI>();
            var promptSo = new SerializedObject(promptUi);
            promptSo.FindProperty("_interactionCaster").objectReferenceValue = interactionCaster;
            promptSo.FindProperty("_promptText").objectReferenceValue = text;
            promptSo.FindProperty("_promptRoot").objectReferenceValue = promptRoot;
            promptSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Same functional (not styled) screen-space panel Milestones 4-6 already established for GameEvents.LevelCompleted — reused as-is.</summary>
        private static void BuildLevelCompleteUi()
        {
            var canvasGo = new GameObject("LevelCompleteCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.4f);
            panelRect.anchorMax = new Vector2(0.7f, 0.6f);
            panelRect.sizeDelta = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelRoot.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "You Escaped The Building";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 28;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            panelRoot.SetActive(false);

            var levelCompleteUi = canvasGo.AddComponent<LevelCompleteUI>();
            var so = new SerializedObject(levelCompleteUi);
            so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Same functional panel shape as BuildLevelCompleteUi, driven by GameEvents.PlayerCaptured instead — the "lose" consequence that was previously missing entirely.</summary>
        private static void BuildGameOverUi()
        {
            var canvasGo = new GameObject("GameOverCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100; // always above PromptCanvas/LevelCompleteCanvas regardless of hierarchy order
            canvasGo.AddComponent<CanvasScaler>();

            var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.4f);
            panelRect.anchorMax = new Vector2(0.7f, 0.6f);
            panelRect.sizeDelta = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0.4f, 0f, 0f, 0.9f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelRoot.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "You Were Caught";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 28;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            panelRoot.SetActive(false);

            var gameOverUi = canvasGo.AddComponent<GameOverController>();
            var so2 = new SerializedObject(gameOverUi);
            so2.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            // 2.5s (not the 2s default) so the heartbeat vignette (5 beats at 2/sec) has
            // time to finish playing before the scene reloads out from under it.
            so2.FindProperty("_reloadDelaySeconds").floatValue = 2.5f;
            so2.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The player's own scared reaction to being caught: a full-screen vignette
        /// (sorting order below GameOverCanvas's 100, so "You Were Caught" still reads
        /// on top of it) cycling through 3 user-provided frames, plus a scream + fast
        /// heartbeat one-shot. See HeartbeatVignetteController for the timing.
        /// </summary>
        private static void BuildHeartbeatVignetteUi()
        {
            var canvasGo = new GameObject("HeartbeatVignetteCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 90;
            canvasGo.AddComponent<CanvasScaler>();

            var imageGo = new GameObject("VignetteImage", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var imageRect = imageGo.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.sizeDelta = Vector2.zero;
            var image = imageGo.GetComponent<Image>();
            image.preserveAspect = false;
            imageGo.SetActive(false);

            var sprites = new Sprite[3];
            for (int i = 0; i < 3; i++)
            {
                sprites[i] = EditorSpriteImportUtility.LoadOrImportSprite($"{HeartbeatVignetteTexturePathPrefix}{i + 1}_Level1.png", 100f, SpriteAlignment.Center);
            }

            var screamAudioSource = canvasGo.AddComponent<AudioSource>();
            screamAudioSource.spatialBlend = 0f;
            screamAudioSource.volume = 1f;
            var heartbeatAudioSource = canvasGo.AddComponent<AudioSource>();
            heartbeatAudioSource.spatialBlend = 0f;
            heartbeatAudioSource.volume = 1f;

            var vignette = canvasGo.AddComponent<HeartbeatVignetteController>();
            var so = new SerializedObject(vignette);
            so.FindProperty("_vignetteRoot").objectReferenceValue = imageGo;
            so.FindProperty("_vignetteImage").objectReferenceValue = image;
            so.FindProperty("_screamAudioSource").objectReferenceValue = screamAudioSource;
            so.FindProperty("_screamSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ScreamClipPath);
            so.FindProperty("_heartbeatAudioSource").objectReferenceValue = heartbeatAudioSource;
            so.FindProperty("_heartbeatSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(HeartbeatClipPath);

            SerializedProperty framesProp = so.FindProperty("_frames");
            framesProp.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Same construction Milestone 8 already established (see Milestone8AssetBuilder.BuildFieldNoteUi) — Level 1 never had this wired in at all, so R01's clue note has been unreadable this whole time.</summary>
        private static void BuildFieldNoteUi(InputActionReference dismissAction)
        {
            var canvasGo = new GameObject("FieldNoteCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.25f);
            panelRect.anchorMax = new Vector2(0.75f, 0.75f);
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

        private static void BuildKeypadEntryUi(InputActionReference dismissAction)
        {
            var canvasGo = new GameObject("KeypadEntryCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.38f, 0.42f);
            panelRect.anchorMax = new Vector2(0.62f, 0.58f);
            panelRect.sizeDelta = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.92f);

            var textGo = new GameObject("Digits", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelRoot.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 32;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            panelRoot.SetActive(false);

            var keypadUi = canvasGo.AddComponent<KeypadEntryUI>();
            var so = new SerializedObject(keypadUi);
            so.FindProperty("_digitsText").objectReferenceValue = text;
            so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("_dismissAction").objectReferenceValue = dismissAction;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildInventoryHudUi(Inventory inventory, InventorySelectionController selectionController)
        {
            var canvasGo = new GameObject("InventoryHudCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var textGo = new GameObject("ItemListText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.LowerCenter;
            text.color = Color.white;
            text.fontSize = 16;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.02f);
            textRect.anchorMax = new Vector2(0.9f, 0.1f);
            textRect.sizeDelta = Vector2.zero;

            var hud = canvasGo.AddComponent<InventoryHudController>();
            var so = new SerializedObject(hud);
            so.FindProperty("_inventory").objectReferenceValue = inventory;
            so.FindProperty("_selection").objectReferenceValue = selectionController;
            so.FindProperty("_listText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- shared helpers

        private static void AddFurniture(Transform parent, string name, Vector3 worldPosition, Vector3 size, bool hideable)
        {
            GameObject furniture = CreateBlockWorld(parent, name, worldPosition, size);
            if (hideable)
            {
                furniture.AddComponent<HidingSpot>();
            }
        }

        /// <summary>Adds a HidingSpot to a Level1FurnitureBuilder model, wiring its "HideAnchor" child (see BuildDesk/BuildCloset) as the camera teleport target if one exists.</summary>
        private static void AddHidingSpot(GameObject furnitureRoot)
        {
            var hidingSpot = furnitureRoot.AddComponent<HidingSpot>();
            Transform hideAnchor = furnitureRoot.transform.Find("HideAnchor");
            if (hideAnchor == null)
            {
                return;
            }

            var so = new SerializedObject(hidingSpot);
            so.FindProperty("_hideAnchor").objectReferenceValue = hideAnchor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateBlockWorld(Transform parent, string name, Vector3 worldPosition, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, true);
            block.transform.position = worldPosition;
            block.transform.localScale = scale;
            return block;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = scale;
            return block;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
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
    }
}
