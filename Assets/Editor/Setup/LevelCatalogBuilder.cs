using EndlessRooms.Core;
using UnityEditor;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Creates/loads the <see cref="LevelDefinition"/>/<see cref="LevelCatalog"/> assets
    /// the main menu and each level's own <c>LevelCompletionRecorder</c> share — one
    /// source of truth so both builders (Milestone9Level1AssetBuilder for the recorder,
    /// MainMenuAssetBuilder for the level-select list) agree on IDs/scene names. Adding
    /// Level 2 later means adding one more Create(...) call and one more catalog entry
    /// here — nothing else in the menu changes.
    /// </summary>
    internal static class LevelCatalogBuilder
    {
        private const string LevelsFolder = "Assets/TheEndlessRooms/ScriptableObjects/Levels";

        internal static LevelDefinition LoadOrCreateLevel1()
        {
            Milestone9Level1AssetBuilder.EnsureFolder(LevelsFolder);

            string path = $"{LevelsFolder}/Level1.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (level != null)
            {
                return level;
            }

            level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "level1";
            level.DisplayName = "Level 1: The Office";
            level.Description = "A night-shift stairwell that never reaches the ground floor.";
            level.SceneName = "Milestone9_Level1TestScene";
            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();
            return level;
        }

        internal static LevelCatalog LoadOrCreateCatalog()
        {
            Milestone9Level1AssetBuilder.EnsureFolder(LevelsFolder);

            LevelDefinition level1 = LoadOrCreateLevel1();

            string path = $"{LevelsFolder}/MainCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            // Only one level exists today — this just keeps the catalog's contents in
            // sync with reality on every rebuild rather than trusting whatever was
            // serialized the first time the asset was created.
            catalog.Levels = new[] { level1 };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }
    }
}
