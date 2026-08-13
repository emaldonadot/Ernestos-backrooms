using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Design-time data for one carryable item (a key, a chain cutter, a pry bar) —
    /// same ScriptableObject-per-type pattern as <c>RoomDefinition</c>. Locks match on
    /// <see cref="ItemId"/> specifically (a named item unlocks a named door), not on
    /// any category, per the Milestone 9 design.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryItem", menuName = "The Endless Rooms/Inventory Item")]
    public sealed class InventoryItemDefinition : ScriptableObject
    {
        public string ItemId;
        public string DisplayName;
        [TextArea] public string Description;
    }
}
