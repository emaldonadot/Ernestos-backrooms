using System.Linq;
using EndlessRooms.Core;
using EndlessRooms.Player;
using EndlessRooms.Procedural;
using EndlessRooms.UI;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for Milestone 8's secret-room demo: the same PC rig and
    /// procedural setup as prior milestones, plus one "Maintenance Sub-Office" secret
    /// room reachable through a door disguised as a bookcase (<see cref="Door"/>'s
    /// existing swing/toggle mechanic, unchanged — only its visual and prompt text
    /// differ). Placed at a fixed offset from the player's spawn, same pattern already
    /// used for Milestone 7's hiding spots/pickup — not wired into the procedural graph
    /// yet (see docs/features/milestone-8-expanded-vertical-slice.md's plan).
    /// </summary>
    public static class Milestone8AssetBuilder
    {
        private const string DefinitionsFolder = "Assets/TheEndlessRooms/ScriptableObjects/RoomDefinitions";
        private const string InputActionsPath = "Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions";
        private const string MovementConfigPath = "Assets/TheEndlessRooms/ScriptableObjects/PlayerMovementConfig.asset";
        private const string RoomPrefabPath = "Assets/TheEndlessRooms/Prefabs/ModularRoomBase.prefab";
        private const string ModelsFolder = "Assets/TheEndlessRooms/Art/Models";
        private const string AudioFolder = "Assets/TheEndlessRooms/Audio";
        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone8_SecretRoomTestScene.unity";
        private const float DoorWidth = 2f;
        private const float WallHeight = 3f;

        private static readonly string WorkOrderText =
            "ALDERMERE BUSINESS PARK — FACILITIES WORK ORDER #4471\n\n" +
            "Requesting: Replacement fluorescent tubes, Sector 4, Stairwell C.\n" +
            "Status: Completed.\n\n" +
            "Note: third request this month for the same fixtures. Maintenance " +
            "insists they were replaced each time. No invoice on file for the parts. " +
            "Suggest checking with Contracts before resubmitting.";

        private static readonly string PersonnelLogText =
            "PERSONNEL LOG — B. OKAFOR, NIGHT SHIFT SUPERVISOR\n\n" +
            "Week 14.\n\n" +
            "Ramirez called out again — third time this rotation. HR still hasn't " +
            "processed his transfer paperwork from two months ago.\n\n" +
            "Elevator 3 back in service. Same technician as always, no invoice, no " +
            "name on the work order — front desk says he's not on our vendor list, " +
            "but he has full building access.\n\n" +
            "Starting to think the roster we're given every Monday doesn't match who's " +
            "actually clocking in.";

        private const string TexturesFolder = "Assets/TheEndlessRooms/Art/Textures";
        private const string MaterialsFolder = "Assets/TheEndlessRooms/Art/Materials";
        private const string WallMaterialPath = MaterialsFolder + "/Wall_Office.mat";

        /// <summary>
        /// Builds a real material from the user-provided wall textures and applies it
        /// to the shared ModularRoomBase prefab's four walls — replacing the flat
        /// DebugColor.Wall placeholder now that a real asset exists. A menu command
        /// (not part of BuildScene) so it can run inside an already-open Editor
        /// session without a competing batch process, same as the earlier wall-related
        /// one-time commands. Roughness isn't wired to a texture: URP Lit's Metallic
        /// map expects smoothness packed into its alpha channel, not a standalone
        /// roughness texture, and repacking it here isn't worth the complexity for one
        /// flat wall material — a fixed smoothness value is used instead.
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/Apply Wall Texture (One-Time)")]
        public static void ApplyWallTexture()
        {
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/Wall_Office_Albedo.png");
            if (albedo == null)
            {
                Debug.LogError($"[Milestone8AssetBuilder] Could not find '{TexturesFolder}/Wall_Office_Albedo.png'.");
                return;
            }

            string normalPath = $"{TexturesFolder}/Wall_Office_Normal.png";
            Texture2D normal = null;
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter)
            {
                if (normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                }

                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            }

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/TheEndlessRooms/Art", "Materials");
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Wall_Office" };
            material.SetTexture("_BaseMap", albedo);
            // Each wall segment (post door-split, see Milestone 7) is 2m wide x 3m tall;
            // the texture was authored to represent ~2m x 2m, so 1 tile per 2m width,
            // 1.5 tiles per 3m height.
            material.mainTextureScale = new Vector2(1f, 1.5f);

            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            material.SetFloat("_Smoothness", 0.35f);

            if (AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath) != null)
            {
                AssetDatabase.DeleteAsset(WallMaterialPath);
            }

            AssetDatabase.CreateAsset(material, WallMaterialPath);

            GameObject contents = PrefabUtility.LoadPrefabContents(RoomPrefabPath);

            // RoomInstance.GetWall() only returns the door-sized "gap" piece it
            // actually toggles — the two permanently-solid side pieces from Milestone
            // 7's wall split ("Wall_North_Left"/"Wall_North_Right" etc.) are separate,
            // unreferenced GameObjects. Match by name prefix instead of GetWall() so
            // every piece gets the real texture, not just the one RoomInstance knows
            // about (this is exactly why "only a section of the wall" showed it before).
            int recoloredCount = 0;
            foreach (Transform child in contents.transform)
            {
                if (!child.name.StartsWith("Wall_"))
                {
                    continue;
                }

                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                    recoloredCount++;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(contents, RoomPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            Debug.Log($"[Milestone8AssetBuilder] Applied '{WallMaterialPath}' to {recoloredCount} wall pieces.");
        }

        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera);
            }

            var actionRefs = LoadInputActionReferences();
            var movementConfig = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);

            new GameObject("GameBootstrap").AddComponent<Core.GameBootstrap>();

            GameObject levelGo = BuildLevelBuilder();
            BuildPlayerAndSpawner(levelGo, movementConfig, actionRefs, out InteractionCaster interactionCaster, out GameObject playerGo);

            BuildSecretRoom(playerGo.transform.position);
            BuildInteractionPromptUi(interactionCaster);
            BuildFieldNoteUi(actionRefs.Interact);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone8AssetBuilder] Built and saved '{ScenePath}'.");
        }

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
                    Debug.LogError($"[Milestone8AssetBuilder] Could not find action '{actionName}' in '{InputActionsPath}'.");
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

        private static GameObject BuildLevelBuilder()
        {
            var entry = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Standard.asset");
            var exit = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Exit.asset");
            var corridor = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Corridor.asset");
            var junction = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Junction.asset");
            var deadEnd = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/DeadEnd.asset");

            var levelGo = new GameObject("ProceduralLevel");
            var builder = levelGo.AddComponent<ProceduralLevelBuilder>();
            var so = new SerializedObject(builder);
            so.FindProperty("_seed").intValue = 8080;
            so.FindProperty("_roomCount").intValue = 8;
            so.FindProperty("_entryDefinition").objectReferenceValue = entry;
            so.FindProperty("_exitDefinition").objectReferenceValue = exit;

            SerializedProperty fillers = so.FindProperty("_fillerDefinitions");
            var fillerDefinitions = new[] { entry, corridor, junction, deadEnd };
            fillers.arraySize = fillerDefinitions.Length;
            for (int i = 0; i < fillerDefinitions.Length; i++)
            {
                fillers.GetArrayElementAtIndex(i).objectReferenceValue = fillerDefinitions[i];
            }

            so.FindProperty("_buildOnStart").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            return levelGo;
        }

        private static void BuildPlayerAndSpawner(GameObject levelGo, PlayerMovementConfig config, ActionRefs actionRefs, out InteractionCaster interactionCaster, out GameObject playerGo)
        {
            playerGo = new GameObject("Player") { tag = "Player" };

            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = config.StandingHeight;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, config.StandingHeight / 2f, 0f);

            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerGo.transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(cameraPivot, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();

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

            var spawnerGo = new GameObject("LevelPlayerSpawner");
            var spawner = spawnerGo.AddComponent<LevelPlayerSpawner>();
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("_levelBuilder").objectReferenceValue = levelGo.GetComponent<ProceduralLevelBuilder>();
            spawnerSo.FindProperty("_player").objectReferenceValue = playerGo.transform;
            spawnerSo.FindProperty("_playerCharacterController").objectReferenceValue = characterController;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Builds the secret room by reusing the exact same ModularRoomBase shell every
        /// regular room uses (so its walls/door-gap system just works), positioned at a
        /// fixed offset south of the player's spawn. The door between the secret room
        /// and open space is <see cref="Door"/>, completely unmodified mechanically —
        /// only its visual (a Bookcase_Disguise mesh instead of a plain panel) and its
        /// prompt text (via the new SetCustomPrompts) are different.
        /// </summary>
        private static void BuildSecretRoom(Vector3 playerSpawnPosition)
        {
            const float cellSize = 6f;
            Vector3 roomCenter = playerSpawnPosition + new Vector3(0f, -1f, -cellSize);

            var roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            var secretRoomGo = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
            secretRoomGo.name = "MaintenanceSubOffice";
            secretRoomGo.transform.position = roomCenter;

            var roomInstance = secretRoomGo.GetComponent<RoomInstance>();
            roomInstance.OpenWall(Direction.North);
            // No DebugColor.Apply here: this room instantiates the same
            // ModularRoomBase prefab every other room uses, so its walls already
            // carry whatever material that shared prefab has (the real Wall_Office
            // texture once one exists) — painting them yellow here would override it.

            Vector3 doorBoundary = roomCenter + new Vector3(0f, 0f, cellSize / 2f);
            BuildDisguisedDoor(doorBoundary);

            Vector3 deskPos = roomCenter + new Vector3(-1.5f, 0f, -1.3f);
            InstantiateProp("Desk_Office.fbx", deskPos, Quaternion.identity);
            InstantiateProp("FilingCabinet.fbx", roomCenter + new Vector3(1.8f, 0f, -1.8f), Quaternion.Euler(0f, 180f, 0f));
            GameObject binder = InstantiateProp("Binder_PersonnelLogs.fbx", deskPos + new Vector3(0.3f, 0.75f, 0f), Quaternion.Euler(0f, 20f, 0f));
            _ = binder;

            BuildFieldNote("WorkOrderNote", deskPos + new Vector3(-0.35f, 0.78f, 0f), WorkOrderText, "Read Work Order");
            BuildFieldNote("PersonnelLogNote", roomCenter + new Vector3(1.8f, 1.35f, -1.8f), PersonnelLogText, "Read Personnel Log");
        }

        private static void BuildDisguisedDoor(Vector3 boundaryPoint)
        {
            Vector3 hingePosition = boundaryPoint;
            hingePosition.x -= DoorWidth / 2f;

            var hinge = new GameObject("SecretDoorHinge");
            hinge.transform.position = hingePosition;

            var bookcasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/Bookcase_Disguise.fbx");
            var bookcaseInstance = (GameObject)PrefabUtility.InstantiatePrefab(bookcasePrefab, hinge.transform);
            bookcaseInstance.transform.localPosition = new Vector3(DoorWidth / 2f, 0f, 0f);

            var collider = bookcaseInstance.AddComponent<BoxCollider>();
            collider.size = new Vector3(DoorWidth, 2.2f, 0.4f);
            collider.center = new Vector3(0f, 1.1f, 0f);

            var door = hinge.AddComponent<Door>();
            door.Initialize(hinge.transform);
            door.SetCustomPrompts("Move the Bookcase", "Push the Bookcase Back");

            var revealClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/SecretRoom_Reveal_Sting.ogg");
            if (revealClip != null)
            {
                var audioSource = hinge.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;

                var revealSound = hinge.AddComponent<DoorRevealSound>();
                var revealSo = new SerializedObject(revealSound);
                revealSo.FindProperty("_door").objectReferenceValue = door;
                revealSo.FindProperty("_revealClip").objectReferenceValue = revealClip;
                revealSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject InstantiateProp(string fbxFileName, Vector3 position, Quaternion rotation)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{fbxFileName}");
            if (prefab == null)
            {
                Debug.LogError($"[Milestone8AssetBuilder] Could not load '{fbxFileName}' at '{ModelsFolder}'.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            instance.transform.rotation = rotation;

            var meshFilter = instance.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && instance.GetComponent<Collider>() == null)
            {
                var boxCollider = instance.AddComponent<BoxCollider>();
                Bounds bounds = meshFilter.sharedMesh.bounds;
                boxCollider.center = bounds.center;
                boxCollider.size = bounds.size;
            }

            return instance;
        }

        private static void BuildFieldNote(string name, Vector3 position, string fragmentText, string promptLabel)
        {
            var noteGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noteGo.name = name;
            noteGo.transform.position = position;
            noteGo.transform.localScale = new Vector3(0.22f, 0.03f, 0.28f);
            DebugColor.Apply(noteGo, DebugColor.Note);

            var note = noteGo.AddComponent<FieldNote>();
            var so = new SerializedObject(note);
            so.FindProperty("_promptLabel").stringValue = promptLabel;
            so.FindProperty("_fragmentText").stringValue = fragmentText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

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
