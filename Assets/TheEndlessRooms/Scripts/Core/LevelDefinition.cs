using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// One entry in the main menu's level list — same ScriptableObject-per-entry
    /// pattern as <see cref="InventoryItemDefinition"/>. <see cref="LevelId"/> is the
    /// stable identifier <see cref="LevelProgressService"/> persists completion against
    /// (never rename an existing one — that silently orphans a player's save); <see
    /// cref="SceneName"/> is the actual scene asset name passed to
    /// <c>SceneManager.LoadScene</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "The Endless Rooms/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public string LevelId;
        public string DisplayName;
        [TextArea] public string Description;
        public string SceneName;
    }
}
