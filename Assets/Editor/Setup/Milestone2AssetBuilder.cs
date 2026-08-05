using System.Linq;
using EndlessRooms.Procedural;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for Milestone 2's modular room prefab, RoomDefinition assets,
    /// and test scene — the same reproducible-setup-tooling pattern as
    /// <see cref="Milestone1SceneBuilder"/>. One-time setup utility, kept under
    /// Assets/Editor so it never ships in a build.
    /// </summary>
    public static class Milestone2AssetBuilder
    {
        private const float CellSize = 6f;
        private const float WallHeight = 3f;
        private const float WallThickness = 0.2f;

        private const string PrefabsFolder = "Assets/TheEndlessRooms/Prefabs";
        private const string RoomPrefabPath = PrefabsFolder + "/ModularRoomBase.prefab";
        private const string DefinitionsFolder = "Assets/TheEndlessRooms/ScriptableObjects/RoomDefinitions";
        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone2_ProceduralTestScene.unity";

        public static void BuildRoomPrefab()
        {
            EnsureFolder(PrefabsFolder);

            var root = new GameObject("ModularRoomBase");

            CreateBlock(root.transform, "Floor", new Vector3(0f, 0f, 0f), new Vector3(CellSize, WallThickness, CellSize));
            CreateBlock(root.transform, "Ceiling", new Vector3(0f, WallHeight, 0f), new Vector3(CellSize, WallThickness, CellSize));

            GameObject wallNorth = CreateBlock(root.transform, "Wall_North", new Vector3(0f, WallHeight / 2f, CellSize / 2f), new Vector3(CellSize, WallHeight, WallThickness));
            GameObject wallEast = CreateBlock(root.transform, "Wall_East", new Vector3(CellSize / 2f, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, CellSize));
            GameObject wallSouth = CreateBlock(root.transform, "Wall_South", new Vector3(0f, WallHeight / 2f, -CellSize / 2f), new Vector3(CellSize, WallHeight, WallThickness));
            GameObject wallWest = CreateBlock(root.transform, "Wall_West", new Vector3(-CellSize / 2f, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, CellSize));

            Transform socketNorth = CreateSocket(root.transform, "Socket_North", new Vector3(0f, WallHeight / 2f, CellSize / 2f));
            Transform socketEast = CreateSocket(root.transform, "Socket_East", new Vector3(CellSize / 2f, WallHeight / 2f, 0f));
            Transform socketSouth = CreateSocket(root.transform, "Socket_South", new Vector3(0f, WallHeight / 2f, -CellSize / 2f));
            Transform socketWest = CreateSocket(root.transform, "Socket_West", new Vector3(-CellSize / 2f, WallHeight / 2f, 0f));

            var roomInstance = root.AddComponent<RoomInstance>();
            var so = new SerializedObject(roomInstance);
            so.FindProperty("_wallNorth").objectReferenceValue = wallNorth;
            so.FindProperty("_wallEast").objectReferenceValue = wallEast;
            so.FindProperty("_wallSouth").objectReferenceValue = wallSouth;
            so.FindProperty("_wallWest").objectReferenceValue = wallWest;
            so.FindProperty("_socketNorth").objectReferenceValue = socketNorth;
            so.FindProperty("_socketEast").objectReferenceValue = socketEast;
            so.FindProperty("_socketSouth").objectReferenceValue = socketSouth;
            so.FindProperty("_socketWest").objectReferenceValue = socketWest;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[Milestone2AssetBuilder] Built '{RoomPrefabPath}'.");
        }

        public static void BuildRoomDefinitions()
        {
            EnsureFolder(DefinitionsFolder);
            var roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (roomPrefab == null)
            {
                Debug.LogError($"[Milestone2AssetBuilder] '{RoomPrefabPath}' not found. Run BuildRoomPrefab first.");
                return;
            }

            CreateOrUpdateDefinition("Standard", RoomCategory.Standard, roomPrefab, isMandatory: false);
            CreateOrUpdateDefinition("Corridor", RoomCategory.Corridor, roomPrefab, isMandatory: false);
            CreateOrUpdateDefinition("Junction", RoomCategory.Junction, roomPrefab, isMandatory: false);
            CreateOrUpdateDefinition("DeadEnd", RoomCategory.DeadEnd, roomPrefab, isMandatory: false);
            CreateOrUpdateDefinition("Exit", RoomCategory.Exit, roomPrefab, isMandatory: true);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Milestone2AssetBuilder] Built RoomDefinition assets in '{DefinitionsFolder}'.");
        }

        private static void CreateOrUpdateDefinition(string name, RoomCategory category, GameObject roomPrefab, bool isMandatory)
        {
            string path = $"{DefinitionsFolder}/{name}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RoomDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.RoomId = name;
            definition.Category = category;
            definition.RoomPrefab = roomPrefab;
            definition.IsMandatory = isMandatory;
            EditorUtility.SetDirty(definition);
        }

        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var entry = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Standard.asset");
            var exit = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Exit.asset");
            var corridor = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Corridor.asset");
            var junction = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/Junction.asset");
            var deadEnd = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"{DefinitionsFolder}/DeadEnd.asset");

            if (entry == null || exit == null || corridor == null || junction == null || deadEnd == null)
            {
                Debug.LogError("[Milestone2AssetBuilder] One or more RoomDefinition assets are missing. Run BuildRoomDefinitions first.");
                return;
            }

            var levelGo = new GameObject("ProceduralLevel");
            var builder = levelGo.AddComponent<ProceduralLevelBuilder>();
            var so = new SerializedObject(builder);
            so.FindProperty("_seed").intValue = 12345;
            so.FindProperty("_roomCount").intValue = 14;
            so.FindProperty("_entryDefinition").objectReferenceValue = entry;
            so.FindProperty("_exitDefinition").objectReferenceValue = exit;

            SerializedProperty fillers = so.FindProperty("_fillerDefinitions");
            var fillerDefinitions = new[] { entry, corridor, junction, deadEnd };
            fillers.arraySize = fillerDefinitions.Length;
            for (int i = 0; i < fillerDefinitions.Length; i++)
            {
                fillers.GetArrayElementAtIndex(i).objectReferenceValue = fillerDefinitions[i];
            }

            so.FindProperty("_buildOnStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Verify the generator + instantiation path headlessly, then clear the
            // generated rooms back out so the saved scene builds fresh on Start (rather
            // than shipping one seed's baked geometry as committed scene data).
            builder.BuildLevel();
            RoomGraph verifiedGraph = builder.LastGraph;
            Debug.Log($"[Milestone2AssetBuilder] Verification build: {verifiedGraph.Nodes.Count} rooms, {verifiedGraph.Connections.Count} connections, valid={RoomGraphValidator.Validate(verifiedGraph).IsValid}.");

            for (int i = levelGo.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(levelGo.transform.GetChild(i).gameObject);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[Milestone2AssetBuilder] Built and saved '{ScenePath}'.");
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

        private static Transform CreateSocket(Transform parent, string name, Vector3 localPosition)
        {
            var socket = new GameObject(name);
            socket.transform.SetParent(parent, false);
            socket.transform.localPosition = localPosition;
            return socket.transform;
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
