using EndlessRooms.Procedural;
using EndlessRooms.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless builder for the rest of Milestone 8's scope beyond the secret room
    /// (see <see cref="Milestone8AssetBuilder"/>): Storage/OfficeCluster room variety
    /// and the guaranteed Atrium landmark room, per
    /// docs/features/milestone-8-expanded-vertical-slice.md. Every new prefab starts
    /// from a copy of the already-finished ModularRoomBase (same split walls, sockets,
    /// RoomInstance wiring, real wall/floor textures) so the procedural door/wall
    /// system — and the M7 fix for the "wall fully disappears at any connection" bug —
    /// keeps working unchanged; new content is purely additive interior dressing (or,
    /// for the Atrium, additional height above the untouched 3m door band).
    /// </summary>
    public static class Milestone8VerticalSliceAssetBuilder
    {
        private const string PrefabsFolder = "Assets/TheEndlessRooms/Prefabs";
        private const string BaseRoomPrefabPath = PrefabsFolder + "/ModularRoomBase.prefab";
        private const string StorageRoomPrefabPath = PrefabsFolder + "/Storage_Room.prefab";
        private const string OfficeClusterRoomPrefabPath = PrefabsFolder + "/OfficeCluster_Room.prefab";
        private const string AtriumRoomPrefabPath = PrefabsFolder + "/Atrium_Room.prefab";
        private const string DefinitionsFolder = "Assets/TheEndlessRooms/ScriptableObjects/RoomDefinitions";

        private const float WallHeight = 3f;
        private const float AtriumHeight = WallHeight * 3f;

        /// <summary>
        /// One-time, persistent change to the shared ModularRoomBase prefab — same
        /// technique as Milestone 7/8's wall-split and texture fixes — so every room in
        /// every scene picks up a flickering ceiling fixture automatically. Must run
        /// before (re)building the Storage/OfficeCluster/Atrium variants below, since
        /// they each start from a fresh copy of whatever this prefab currently
        /// contains, not a live nested-prefab link.
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/M8 Vertical Slice/Add Flickering Light To Base Room (One-Time)")]
        public static void AddFlickeringLightToBaseRoom()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(BaseRoomPrefabPath);

            if (contents.transform.Find("CeilingLight") == null)
            {
                AddCeilingLight(contents.transform, "CeilingLight", new Vector3(0f, WallHeight - 0.15f, 0f));
            }

            PrefabUtility.SaveAsPrefabAsset(contents, BaseRoomPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log($"[Milestone8VerticalSliceAssetBuilder] Added flickering ceiling light to '{BaseRoomPrefabPath}'.");
        }

        private static void AddCeilingLight(Transform parent, string name, Vector3 localPosition)
        {
            var lightGo = new GameObject(name);
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = localPosition;
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 1.5f;
            light.color = new Color(0.85f, 0.9f, 1f);

            lightGo.AddComponent<FlickeringLight>();
        }

        [MenuItem("Tools/The Endless Rooms/M8 Vertical Slice/Build Storage Room Prefab")]
        public static void BuildStorageRoomPrefab()
        {
            GameObject contents = CloneBaseRoom("Storage_Room");

            // Clustered toward the corners, leaving the center cross (where a door on
            // any of the 4 walls could open into) clear — obstacle placement can't
            // assume which walls end up with doors, since that's decided per-instance
            // by the procedural graph.
            AddObstacle(contents.transform, "Crate_NE", new Vector3(1.8f, 0.5f, 1.8f), new Vector3(1f, 1f, 1f));
            AddObstacle(contents.transform, "Crate_NW", new Vector3(-1.8f, 0.5f, 1.8f), new Vector3(1f, 1f, 1.2f));
            AddObstacle(contents.transform, "Crate_SE", new Vector3(1.8f, 0.5f, -1.8f), new Vector3(1.2f, 1f, 1f));
            AddObstacle(contents.transform, "Shelf_SW", new Vector3(-1.7f, 1f, -1.7f), new Vector3(1.2f, 2f, 1.2f));
            AddObstacle(contents.transform, "Crate_OffAxis", new Vector3(1f, 0.4f, 0.3f), new Vector3(0.8f, 0.8f, 0.8f));

            PrefabUtility.SaveAsPrefabAsset(contents, StorageRoomPrefabPath);
            Object.DestroyImmediate(contents);
            Debug.Log($"[Milestone8VerticalSliceAssetBuilder] Built '{StorageRoomPrefabPath}'.");
        }

        [MenuItem("Tools/The Endless Rooms/M8 Vertical Slice/Build Office Cluster Room Prefab")]
        public static void BuildOfficeClusterRoomPrefab()
        {
            GameObject contents = CloneBaseRoom("OfficeCluster_Room");

            // Two partition walls (one per axis) split the 6x6 room into four small
            // cubicle-like pockets around a shared 1.8m-wide open cross in the middle,
            // so it's still fully traversable regardless of which exterior wall(s) end
            // up with doors.
            const float thickness = 0.15f;
            const float segmentLength = 2.1f;
            const float segmentCenter = 1.95f;

            AddPartition(contents.transform, "Partition_NS_North", new Vector3(0f, WallHeight / 2f, segmentCenter), new Vector3(thickness, WallHeight, segmentLength));
            AddPartition(contents.transform, "Partition_NS_South", new Vector3(0f, WallHeight / 2f, -segmentCenter), new Vector3(thickness, WallHeight, segmentLength));
            AddPartition(contents.transform, "Partition_EW_East", new Vector3(segmentCenter, WallHeight / 2f, 0f), new Vector3(segmentLength, WallHeight, thickness));
            AddPartition(contents.transform, "Partition_EW_West", new Vector3(-segmentCenter, WallHeight / 2f, 0f), new Vector3(segmentLength, WallHeight, thickness));

            PrefabUtility.SaveAsPrefabAsset(contents, OfficeClusterRoomPrefabPath);
            Object.DestroyImmediate(contents);
            Debug.Log($"[Milestone8VerticalSliceAssetBuilder] Built '{OfficeClusterRoomPrefabPath}'.");
        }

        [MenuItem("Tools/The Endless Rooms/M8 Vertical Slice/Build Atrium Landmark Prefab")]
        public static void BuildAtriumPrefab()
        {
            GameObject contents = CloneBaseRoom("Atrium_Room");

            // The door band (ground floor to 3m, same as every other room) is left
            // completely untouched, so a connection into the Atrium looks like a normal
            // doorway from the outside — the scale reveals itself only once you're
            // through. Everything above 3m is new: solid upper walls (no gaps; doors
            // never need to reach this high), a raised ceiling, and the mezzanine +
            // escalator.
            Transform ceiling = contents.transform.Find("Ceiling");
            if (ceiling != null)
            {
                Vector3 pos = ceiling.localPosition;
                pos.y = AtriumHeight;
                ceiling.localPosition = pos;
            }

            // The base room's ceiling light comes along with the clone at its original
            // 3m height — move it up to the real ceiling, and add a second fixture near
            // the mezzanine so the tall space isn't lit only from far overhead.
            Transform ceilingLight = contents.transform.Find("CeilingLight");
            if (ceilingLight != null)
            {
                Vector3 pos = ceilingLight.localPosition;
                pos.y = AtriumHeight - 0.15f;
                ceilingLight.localPosition = pos;
            }

            // Mezzanine height is constrained by the room's small 6x6 footprint, not
            // "half the Atrium's height": the ramp has to finish climbing *before* it
            // reaches the mezzanine's footprint, or its rising slope clips straight
            // through the mezzanine's underside instead of meeting it edge-to-edge.
            // With the mezzanine occupying the north ~2.8m of the room (below), that
            // leaves only the south ~3.1m of floor for the entire climb — at the
            // 45-degree slope limit (no jump in this game, and discrete steps taller
            // than the ~0.3m default step offset are simply impassable), that caps the
            // achievable height at just over 3m. 2.2m keeps a comfortable margin.
            const float mezzanineHeight = 2.2f;
            const float mezzanineNearEdgeZ = 0.1f;

            AddCeilingLight(contents.transform, "MezzanineLight", new Vector3(0f, mezzanineHeight + 0.3f, 1.5f));

            AddUpperWall(contents.transform, "UpperWall_North", new Vector3(0f, (WallHeight + AtriumHeight) / 2f, 3f), new Vector3(6f, AtriumHeight - WallHeight, 0.2f));
            AddUpperWall(contents.transform, "UpperWall_South", new Vector3(0f, (WallHeight + AtriumHeight) / 2f, -3f), new Vector3(6f, AtriumHeight - WallHeight, 0.2f));
            AddUpperWall(contents.transform, "UpperWall_East", new Vector3(3f, (WallHeight + AtriumHeight) / 2f, 0f), new Vector3(0.2f, AtriumHeight - WallHeight, 6f));
            AddUpperWall(contents.transform, "UpperWall_West", new Vector3(-3f, (WallHeight + AtriumHeight) / 2f, 0f), new Vector3(0.2f, AtriumHeight - WallHeight, 6f));

            // Mezzanine walkway along the north side — its near (south) edge is at
            // mezzanineNearEdgeZ, matched exactly by the ramp's end point below so the
            // two meet flush instead of overlapping.
            AddUpperWall(contents.transform, "Mezzanine_Floor", new Vector3(0f, mezzanineHeight, 1.5f), new Vector3(6f, 0.2f, 2.8f));
            AddUpperWall(contents.transform, "Mezzanine_Railing", new Vector3(0f, mezzanineHeight + 0.5f, 0.15f), new Vector3(6f, 0.9f, 0.1f));

            // A "broken escalator" as a single continuous ramp along the west wall —
            // rough/disused in flavor (no handrail, bare incline). It finishes climbing
            // to mezzanineHeight exactly at mezzanineNearEdgeZ, so there's no stretch
            // where the rising ramp surface and the flat mezzanine underside overlap in
            // the same space (which is what caused the "hit the floor going up" bug —
            // the ramp previously kept climbing 1.7m past the mezzanine's near edge
            // while still underneath its footprint, so the rising slope ran straight
            // into the mezzanine floor above it).
            Vector3 rampStart = new(-2f, 0f, -2.9f);
            Vector3 rampEnd = new(-2f, mezzanineHeight, mezzanineNearEdgeZ);
            AddRamp(contents.transform, "Escalator_Ramp", rampStart, rampEnd, width: 1.8f, thickness: 0.3f);

            PrefabUtility.SaveAsPrefabAsset(contents, AtriumRoomPrefabPath);
            Object.DestroyImmediate(contents);
            Debug.Log($"[Milestone8VerticalSliceAssetBuilder] Built '{AtriumRoomPrefabPath}'.");
        }

        [MenuItem("Tools/The Endless Rooms/M8 Vertical Slice/Build Room Definitions")]
        public static void BuildRoomDefinitions()
        {
            EnsureFolder(DefinitionsFolder);

            var storagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StorageRoomPrefabPath);
            var officeClusterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OfficeClusterRoomPrefabPath);

            if (storagePrefab == null || officeClusterPrefab == null)
            {
                Debug.LogError("[Milestone8VerticalSliceAssetBuilder] Storage/OfficeCluster prefabs are missing. Build those first.");
                return;
            }

            CreateOrUpdateDefinition("Storage", RoomCategory.Storage, storagePrefab);
            CreateOrUpdateDefinition("OfficeCluster", RoomCategory.OfficeCluster, officeClusterPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Milestone8VerticalSliceAssetBuilder] Built Storage/OfficeCluster RoomDefinition assets in '{DefinitionsFolder}'.");
        }

        private static void CreateOrUpdateDefinition(string name, RoomCategory category, GameObject roomPrefab)
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
            definition.IsMandatory = false;
            EditorUtility.SetDirty(definition);
        }

        private static GameObject CloneBaseRoom(string name)
        {
            GameObject baseRoom = AssetDatabase.LoadAssetAtPath<GameObject>(BaseRoomPrefabPath);
            var contents = (GameObject)Object.Instantiate(baseRoom);
            contents.name = name;
            return contents;
        }

        private static void AddObstacle(Transform parent, string name, Vector3 localPosition, Vector3 scale)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = name;
            obstacle.transform.SetParent(parent, false);
            obstacle.transform.localPosition = localPosition;
            obstacle.transform.localScale = scale;
        }

        private static void AddPartition(Transform parent, string name, Vector3 localPosition, Vector3 scale)
        {
            AddObstacle(parent, name, localPosition, scale);
        }

        private static void AddUpperWall(Transform parent, string name, Vector3 localPosition, Vector3 scale)
        {
            AddObstacle(parent, name, localPosition, scale);
        }

        /// <summary>
        /// A single tilted box spanning <paramref name="start"/> to <paramref name="end"/> —
        /// its local Z axis (length) points along that direction via
        /// <see cref="Quaternion.LookRotation(Vector3)"/> rather than a hand-picked Euler
        /// angle, so the slope is exactly whatever the two points imply, no separate
        /// angle computation to get wrong.
        /// </summary>
        private static void AddRamp(Transform parent, string name, Vector3 start, Vector3 end, float width, float thickness)
        {
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name;
            ramp.transform.SetParent(parent, false);

            Vector3 direction = end - start;
            float length = direction.magnitude;

            ramp.transform.localPosition = (start + end) / 2f;
            ramp.transform.localRotation = Quaternion.LookRotation(direction.normalized);
            ramp.transform.localScale = new Vector3(width, thickness, length);
        }

        /// <summary>
        /// Actually runs generation once against the saved M8 scene's level builder
        /// in memory — the scene itself only stores an *unbuild* level (_buildOnStart
        /// is false; the real build happens at runtime), so this is the only way to
        /// confirm the new landmark placement + Storage/OfficeCluster categories
        /// actually work together before relying on interactive Play-mode testing.
        /// Doesn't save the scene — this is a throwaway in-memory check.
        /// </summary>
        [MenuItem("Tools/The Endless Rooms/M8 Vertical Slice/Verify M8 Level Generation")]
        public static void VerifyLevelGeneration()
        {
            const string scenePath = "Assets/TheEndlessRooms/Scenes/Milestone8_SecretRoomTestScene.unity";
            EditorSceneManager.OpenScene(scenePath);

            var builder = Object.FindAnyObjectByType<ProceduralLevelBuilder>();
            if (builder == null)
            {
                Debug.LogError("[Milestone8VerticalSliceAssetBuilder] No ProceduralLevelBuilder found in the scene.");
                return;
            }

            builder.BuildLevel();
            RoomGraph graph = builder.LastGraph;

            bool foundLandmark = false;
            foreach (Transform child in builder.transform)
            {
                if (child.name.StartsWith("Landmark_"))
                {
                    foundLandmark = true;
                    break;
                }
            }

            Debug.Log($"[Milestone8VerticalSliceAssetBuilder] Verification build: {graph.Nodes.Count} rooms, {graph.Connections.Count} connections, valid={RoomGraphValidator.Validate(graph).IsValid}, landmarkPlaced={foundLandmark}.");

            for (int i = builder.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(builder.transform.GetChild(i).gameObject);
            }
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
    }
}
