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
    /// <see cref="Level1RoomGraphProvider"/>'s synthetic graph), the exit, and a first
    /// investigate-clue-key-locked door chain proving the mechanic end to end (more
    /// content is a follow-up, not a blocker for the first playable pass).
    /// </summary>
    public static class Milestone9Level1AssetBuilder
    {
        private const float WallHeight = 3f;
        private const float WallThickness = 0.2f;
        private const float DoorWidth = 2f;

        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone9_Level1TestScene.unity";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";
        private const string MovementConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/PlayerMovementConfig.asset";
        private const string ItemsFolder = "Assets/TheEndlessRooms/ScriptableObjects/Items";
        private const string JumpScareSpritePath = "Assets/TheEndlessRooms/Art/Textures/JumpScareMonster.png";
        private const float JumpScareSpritePixelsPerUnit = 700f;

        // Shared with BuildFirstKeyLockChain, which positions the R01 key/note relative
        // to the desk's actual position — keeping one constant means they can't drift
        // apart the way they did when the desk moved but the item offsets didn't.
        private const float OfficeDeskLocalX = -0.3f;

        private const string WallTexturePath = "Assets/TheEndlessRooms/Art/Textures/WallAlbedo_Level1.png";
        private const string DoorTexturePath = "Assets/TheEndlessRooms/Art/Textures/Door_Level1.png";
        private const string WallMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Wall_Level1.mat";
        private const string DoorMaterialPath = "Assets/TheEndlessRooms/Art/Materials/Door_Level1.mat";
        private const float WallTextureMetersPerTile = 2f; // WallAlbedo_Level1.png is a tileable ~2m x 2m square texture

        private const string ThunderClipPath = "Assets/TheEndlessRooms/Audio/ThunderCrash_Level1.wav";
        private const string ScreamClipPath = "Assets/TheEndlessRooms/Audio/CaptureScream_Level1.wav";
        private const string HeartbeatClipPath = "Assets/TheEndlessRooms/Audio/CaptureHeartbeat_Level1.wav";
        private const string HeartbeatVignetteTexturePathPrefix = "Assets/TheEndlessRooms/Art/Textures/HeartbeatVignette";

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
            var movementConfig = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
            GameObject playerGo = BuildPlayer(movementConfig, actionRefs, out InteractionCaster interactionCaster, out CameraShakeEffect cameraShake);
            playerGo.transform.position = new Vector3(0f, 1f, 1.5f);
            _ = cameraShake;

            LightningFlash lightningFlash = BuildNightAmbiance();

            var attendantConfig = Milestone7AssetBuilder.LoadOrCreateAttendantConfig();
            Milestone7AssetBuilder.BuildAttendant(levelGraphGo, attendantConfig, playerGo.transform);
            BuildAttendantAppearanceCycle(warningLights, courtyardRain, lightningFlash);

            BuildExitPoint();
            BuildFirstKeyLockChain(playerGo.transform);
            BuildJumpScares();

            BuildInteractionPromptUi(interactionCaster);
            BuildLevelCompleteUi();
            BuildGameOverUi();
            BuildHeartbeatVignetteUi();

            ApplyLevel1Materials(levelRoot);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone9Level1AssetBuilder] Built and saved '{ScenePath}' — {Level1Layout.Offices.Length} offices, 2 bathrooms, 2 courtyards, player, Attendant, exit, one key/lock chain, jump scares.");
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

            int wallCount = 0;
            int doorCount = 0;

            foreach (Transform t in levelRoot.GetComponentsInChildren<Transform>(true))
            {
                var renderer = t.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                if (wallMaterial != null && t.name.StartsWith("Wall_"))
                {
                    renderer.sharedMaterial = wallMaterial;

                    // A cube's UVs always span 0-1 per face regardless of localScale, so
                    // without this every wall segment — corridor end caps, 6m room walls,
                    // 2m door-flanking strips — would show the exact same single tile.
                    // The thinnest axis (WallThickness) is never the visible face, so the
                    // other two localScale components are the actual width/height to tile
                    // across.
                    float[] dims = { t.localScale.x, t.localScale.y, t.localScale.z };
                    System.Array.Sort(dims);
                    Material instanceMaterial = renderer.material;
                    instanceMaterial.mainTextureScale = new Vector2(dims[2] / WallTextureMetersPerTile, dims[1] / WallTextureMetersPerTile);
                    wallCount++;
                }
                else if (doorMaterial != null && t.name == "DoorPanel")
                {
                    // DoorWidth x WallHeight (2m x 3m) exactly matches Door_Level1.png's
                    // 2:3 aspect ratio, so this maps once with no tiling distortion.
                    renderer.sharedMaterial = doorMaterial;
                    doorCount++;
                }
            }

            Debug.Log($"[Milestone9Level1AssetBuilder] Applied wall/door materials to {wallCount} wall segments and {doorCount} door panels.");
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

            BuildRoomShellWithDoor(roomGo.transform, spec.Side, Level1Layout.RoomDepthX, Level1Layout.RoomWidthZ, out Door door);
            BuildOfficeFurniture(roomGo.transform, spec);

            if (spec.Id == "R11")
            {
                _pendingLockedDoor = door;
            }
        }

        private static Door _pendingLockedDoor;

        /// <summary>
        /// Floor, ceiling, back wall, two side walls, and a door-gap front wall (split
        /// into permanently-solid left/right pieces flanking a DoorWidth gap — same
        /// "split wall" convention Milestone 7 established for the procedural rooms, so
        /// a closed door actually blocks the whole opening instead of leaving it walkable).
        /// All positions are room-local (center of the room = origin) before LocalToWorld
        /// mirrors them for East-side rooms.
        /// </summary>
        private static void BuildRoomShellWithDoor(Transform room, Side side, float depthX, float widthZ, out Door door)
        {
            Vector3 roomCenter = room.position;

            CreateBlockWorld(room, "Floor", roomCenter, new Vector3(depthX, WallThickness, widthZ));
            CreateBlockWorld(room, "Ceiling", roomCenter + Vector3.up * WallHeight, new Vector3(depthX, WallThickness, widthZ));

            Vector3 backWallLocal = new(-depthX / 2f, WallHeight / 2f, 0f);
            CreateBlockWorld(room, "Wall_Back", Level1Layout.LocalToWorld(roomCenter, side, backWallLocal), new Vector3(WallThickness, WallHeight, widthZ));

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
                AddFurniture(room, "MeetingTable", Level1Layout.LocalToWorld(roomCenter, spec.Side, new Vector3(-1.5f, 0.375f, 0f)), new Vector3(1.0f, 0.75f, 2.2f), hideable: false);
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

            AddFurniture(roomGo.transform, "Toilet", Level1Layout.LocalToWorld(center, side, new Vector3(-2.0f, 0.4f, -2.0f)), new Vector3(0.6f, 0.8f, 0.6f), hideable: false);
            AddFurniture(roomGo.transform, "Sink", Level1Layout.LocalToWorld(center, side, new Vector3(-2.0f, 0.45f, 2.0f)), new Vector3(0.6f, 0.9f, 0.6f), hideable: false);
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
            main.startColor = new Color(0.75f, 0.8f, 0.9f, 0.55f);
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

            rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            return rain;
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
            };
        }

        private static GameObject BuildPlayer(PlayerMovementConfig config, ActionRefs actionRefs, out InteractionCaster interactionCaster, out CameraShakeEffect cameraShake)
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

            playerGo.AddComponent<Inventory>();

            return playerGo;
        }

        // ---------------------------------------------------------------- exit

        private static void BuildExitPoint()
        {
            float z = Level1Layout.RowCenterZ(Level1Layout.TotalRows) + Level1Layout.RowSpacing / 2f - 1f;
            var exitGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            exitGo.name = "ExitPoint";
            exitGo.transform.position = new Vector3(0f, 1f, z);
            exitGo.transform.localScale = Vector3.one * 0.6f;
            exitGo.AddComponent<ExitPoint>();
        }

        // ---------------------------------------------------------------- first key/lock chain

        /// <summary>
        /// Proves the investigate-clue-key-locked-door mechanic end to end: a clue in R01
        /// mentions a master key, the key itself sits in R01, and R11's door (near the
        /// exit) requires it. Only one chain for this first pass — more rooms getting
        /// clues/keys/locks is straightforward follow-up content once this is confirmed
        /// to work, not a blocker.
        /// </summary>
        private static void BuildFirstKeyLockChain(Transform player)
        {
            GameObject r01 = GameObject.Find("R01");
            if (r01 == null)
            {
                Debug.LogError("[Milestone9Level1AssetBuilder] Could not find R01 to place the first key/clue in.");
                return;
            }

            EnsureFolder(ItemsFolder);
            string itemPath = $"{ItemsFolder}/MasterKey.asset";
            var masterKey = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(itemPath);
            if (masterKey == null)
            {
                masterKey = ScriptableObject.CreateInstance<InventoryItemDefinition>();
                AssetDatabase.CreateAsset(masterKey, itemPath);
            }

            masterKey.ItemId = "master_key";
            masterKey.DisplayName = "Master Key";
            masterKey.Description = "A worn brass key. Someone kept it in their desk.";
            EditorUtility.SetDirty(masterKey);
            AssetDatabase.SaveAssets();

            // Rests on the R01 desk's actual desktop surface (Level1FurnitureBuilder.BuildDesk:
            // floorY 0.1 + deskHeight 0.75 = 0.85, plus half the key's own thickness). X
            // matches OfficeDeskLocalX, the same constant BuildOfficeFurniture positions
            // the desk with, since the desk's rotation only affects X/Z, not its own
            // local layout — desk width (and this Z=0 center) still runs along room Z.
            Vector3 keyPosition = r01.transform.position + new Vector3(OfficeDeskLocalX, 0.86f, 0f);
            var keyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyGo.name = "MasterKeyPickup";
            keyGo.transform.position = keyPosition;
            keyGo.transform.localScale = new Vector3(0.08f, 0.02f, 0.15f);
            DebugColor.Apply(keyGo, DebugColor.Pickup);
            var pickup = keyGo.AddComponent<InventoryPickup>();
            var pickupSo = new SerializedObject(pickup);
            pickupSo.FindProperty("_item").objectReferenceValue = masterKey;
            pickupSo.ApplyModifiedPropertiesWithoutUndo();

            var clueGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clueGo.name = "ClueNote_R01";
            clueGo.transform.position = r01.transform.position + new Vector3(OfficeDeskLocalX, 0.865f, -0.35f);
            clueGo.transform.localScale = new Vector3(0.22f, 0.03f, 0.28f);
            DebugColor.Apply(clueGo, DebugColor.Note);
            var note = clueGo.AddComponent<FieldNote>();
            var noteSo = new SerializedObject(note);
            noteSo.FindProperty("_promptLabel").stringValue = "Read Note";
            noteSo.FindProperty("_fragmentText").stringValue =
                "\"Left the master key in my desk again — third time this month. If R11's " +
                "stuck shut, that's where it'll be.\"";
            noteSo.ApplyModifiedPropertiesWithoutUndo();

            if (_pendingLockedDoor != null)
            {
                _pendingLockedDoor.SetRequiredItem(masterKey);
                _pendingLockedDoor.RestoreState(new Door.DoorState(isOpen: false, isLocked: true));
            }
            else
            {
                Debug.LogError("[Milestone9Level1AssetBuilder] R11's door wasn't captured — the key/lock chain has nothing to unlock.");
            }

            _ = player;
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
