using System.IO;
using UnityEditor;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Procedurally-drawn inventory icons (rounded chip + a shape/color reading as each
    /// item's real silhouette) — CPU pixel-writes rather than a camera render-to-texture
    /// pipeline, since headless builds run with <c>-nographics</c> and Camera.Render()
    /// isn't reliable there. Colors are pulled from the same palette
    /// Level1ItemModelBuilder uses for the actual 3D models, so an icon and its pickup
    /// read as the same object. Placeholder-quality (like SpiderWebClue's art) until/if
    /// real icon art replaces these — swapping just means overwriting the PNGs this
    /// writes to Assets/TheEndlessRooms/Art/Textures/Icons/.
    /// </summary>
    internal static class Level1ItemIconBuilder
    {
        private const string IconsFolder = "Assets/TheEndlessRooms/Art/Textures/Icons";
        private const int Size = 128;

        private static readonly Color DarkPlastic = new(0.1f, 0.1f, 0.11f);
        private static readonly Color Metal = new(0.6f, 0.6f, 0.63f);
        private static readonly Color Gold = new(0.85f, 0.7f, 0.25f);
        private static readonly Color Bronze = new(0.55f, 0.38f, 0.2f);
        private static readonly Color WarmLens = new(0.95f, 0.9f, 0.65f);
        private static readonly Color UvLens = new(0.65f, 0.25f, 0.95f);
        private static readonly Color Paper = new(0.85f, 0.81f, 0.68f);
        private static readonly Color CardBody = new(0.92f, 0.93f, 0.95f);
        private static readonly Color CardPhoto = new(0.28f, 0.31f, 0.35f);
        private static readonly Color CardStripe = new(0.7f, 0.15f, 0.17f);

        internal static Sprite BuildBatteryIcon() => GetOrCreate("Icon_Battery", tex =>
        {
            DrawRoundedRect(tex, 64f, 60f, 26f, 46f, 8f, DarkPlastic);
            DrawRoundedRect(tex, 64f, 104f, 10f, 10f, 3f, Metal);
        });

        internal static Sprite BuildCassetteIcon() => GetOrCreate("Icon_Cassette", tex =>
        {
            DrawRoundedRect(tex, 64f, 64f, 50f, 34f, 8f, DarkPlastic);
            DrawRoundedRect(tex, 64f, 64f, 40f, 12f, 3f, Paper);
            DrawCircle(tex, 44f, 64f, 7f, Metal);
            DrawCircle(tex, 84f, 64f, 7f, Metal);
        });

        internal static Sprite BuildCassetteRecorderIcon() => GetOrCreate("Icon_CassetteRecorder", tex =>
        {
            DrawRoundedRect(tex, 64f, 64f, 52f, 40f, 10f, DarkPlastic);
            DrawCircle(tex, 40f, 64f, 12f, Metal);
            DrawRoundedRect(tex, 84f, 64f, 16f, 12f, 2f, Paper);
        });

        internal static Sprite BuildFlashlightIcon() => GetOrCreate("Icon_Flashlight", tex =>
        {
            DrawRoundedRect(tex, 64f, 66f, 14f, 48f, 6f, DarkPlastic);
            DrawCircle(tex, 64f, 22f, 15f, WarmLens);
        });

        internal static Sprite BuildUvFlashlightIcon() => GetOrCreate("Icon_UvFlashlight", tex =>
        {
            DrawRoundedRect(tex, 64f, 66f, 14f, 48f, 6f, DarkPlastic);
            DrawCircle(tex, 64f, 22f, 15f, UvLens);
        });

        internal static Sprite BuildGoldenKeyIcon() => GetOrCreate("Icon_GoldenKey", tex => DrawKey(tex, Gold));

        internal static Sprite BuildBronzeKeyIcon() => GetOrCreate("Icon_BronzeKey", tex => DrawKey(tex, Bronze));

        internal static Sprite BuildIdCardIcon() => GetOrCreate("Icon_IdCard", tex =>
        {
            DrawRoundedRect(tex, 64f, 64f, 48f, 34f, 8f, CardBody);
            DrawRoundedRect(tex, 40f, 68f, 12f, 16f, 2f, CardPhoto);
            DrawRoundedRect(tex, 68f, 44f, 38f, 5f, 1f, CardStripe);
        });

        private static void DrawKey(Texture2D tex, Color color)
        {
            DrawCircle(tex, 46f, 64f, 22f, color);
            DrawCircle(tex, 46f, 64f, 11f, Color.clear); // punches the bow's hole back to transparent
            DrawRoundedRect(tex, 78f, 64f, 34f, 6f, 2f, color);
            DrawRoundedRect(tex, 104f, 54f, 4f, 10f, 1f, color);
            DrawRoundedRect(tex, 96f, 54f, 4f, 8f, 1f, color);
        }

        private static Sprite GetOrCreate(string name, System.Action<Texture2D> draw)
        {
            string path = $"{IconsFolder}/{name}.png";
            var existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var clear = new Color[Size * Size];
            for (int i = 0; i < clear.Length; i++)
            {
                clear[i] = Color.clear;
            }

            tex.SetPixels(clear);
            draw(tex);
            tex.Apply();

            if (!AssetDatabase.IsValidFolder(IconsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/TheEndlessRooms/Art/Textures"))
                {
                    AssetDatabase.CreateFolder("Assets/TheEndlessRooms/Art", "Textures");
                }

                AssetDatabase.CreateFolder("Assets/TheEndlessRooms/Art/Textures", "Icons");
            }

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            // EditorSpriteImportUtility already solved "make LoadAssetAtPath<Sprite>
            // actually return something" for this exact headless -nographics
            // environment (jump-scare/heartbeat-vignette sprites) — the piece this
            // method was missing on its own was spriteImportMode = Single, which
            // LoadOrImportSprite sets and a plain textureType assignment doesn't imply.
            return EditorSpriteImportUtility.LoadOrImportSprite(path, 100f, SpriteAlignment.Center);
        }

        /// <summary>Rounded-box SDF (dist &lt;= 0 is inside) — cx/cy in pixel space, halfWidth/halfHeight/cornerRadius likewise.</summary>
        private static void DrawRoundedRect(Texture2D tex, float cx, float cy, float halfWidth, float halfHeight, float cornerRadius, Color color)
        {
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float qx = Mathf.Max(Mathf.Abs(x + 0.5f - cx) - (halfWidth - cornerRadius), 0f);
                    float qy = Mathf.Max(Mathf.Abs(y + 0.5f - cy) - (halfHeight - cornerRadius), 0f);
                    float dist = Mathf.Sqrt(qx * qx + qy * qy) - cornerRadius;
                    if (dist <= 0f)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        /// <summary>Sets every pixel inside the circle to <paramref name="color"/> — including Color.clear, useful for punching a hole in a previously-drawn shape.</summary>
        private static void DrawCircle(Texture2D tex, float cx, float cy, float radius, Color color)
        {
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - radius;
                    if (dist <= 0f)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }
    }
}
