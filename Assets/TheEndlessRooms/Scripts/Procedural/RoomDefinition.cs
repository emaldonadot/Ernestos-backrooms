using UnityEngine;

namespace EndlessRooms.Procedural
{
    /// <summary>
    /// Design-time metadata for one room type: category, connectors, allowed neighbors,
    /// rarity, and difficulty weight (PRD Section 8). The Milestone 2 generator consumes
    /// these; Milestone 1 only establishes the data shape and a couple of authored assets
    /// for the test scene.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "The Endless Rooms/Room Definition")]
    public sealed class RoomDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string RoomId;
        public RoomCategory Category;

        [Header("Prefab")]
        public GameObject RoomPrefab;

        [Header("Connections")]
        public RoomConnectorSocket[] ConnectorSockets;

        [Tooltip("If empty, this room allows any neighbor category.")]
        public RoomCategory[] AllowedNeighborCategories;

        [Header("Generation Weighting")]
        [Range(0f, 1f)] public float Rarity = 0.5f;
        [Min(0)] public int DifficultyWeight = 1;
        public bool IsMandatory;

        public bool AllowsNeighbor(RoomCategory neighborCategory)
        {
            if (AllowedNeighborCategories == null || AllowedNeighborCategories.Length == 0)
            {
                return true;
            }

            foreach (RoomCategory allowed in AllowedNeighborCategories)
            {
                if (allowed == neighborCategory)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
