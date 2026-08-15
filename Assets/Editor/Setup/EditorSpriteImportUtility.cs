using UnityEditor;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>Shared by Milestone7AssetBuilder (the Attendant) and Milestone9Level1AssetBuilder (jump-scare figures) so both monster-art sprites get identical, correct import settings.</summary>
    internal static class EditorSpriteImportUtility
    {
        internal static Sprite LoadOrImportSprite(string path, float pixelsPerUnit, SpriteAlignment alignment)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = pixelsPerUnit;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)alignment;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
