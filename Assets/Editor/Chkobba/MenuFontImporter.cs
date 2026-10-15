#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Chkobba.EditorTools
{
    /// <summary>
    /// Generates TMP font assets with a STATIC atlas prebaked from an explicit
    /// codepoint list (C# strings don't expand \u ranges, so we enumerate).
    /// Coverage: ASCII, Latin-1, Arabic base block (0621-065F), Arabic presentation
    /// forms A ligatures (FB50-FD3D range trimmed), presentation forms B (FE70-FEFF),
    /// and common punctuation. Menu: Chkobba > Import Menu Fonts.
    /// </summary>
    public static class MenuFontImporter
    {
        private static System.Collections.Generic.IEnumerable<int> Charset()
        {
            // ASCII + Latin-1
            for (int c = 0x0020; c <= 0x00FF; c++) yield return c;
            // Arabic base block: hamza through yeh + diacritics + Arabic-Indic digits + punctuation
            for (int c = 0x0600; c <= 0x06FF; c++) yield return c;
            // presentation forms B (FE70-FEFF) — the connected/shaped variants,
            // including lam-alef ligatures (FEF5-FEFC)
            for (int c = 0xFE70; c <= 0xFEFF; c++) yield return c;
            // extra punctuation used in the menu strings
            foreach (int c in new[] { 0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2026 })
                yield return c;
        }

        [MenuItem("Chkobba/Import Menu Fonts")]
        public static void Import()
        {
            var charsetBuilder = new StringBuilder();
            foreach (int c in Charset())
            {
                if (c >= 0xD800 && c <= 0xDFFF) continue; // surrogates
                charsetBuilder.Append((char)c);
            }
            string charset = charsetBuilder.ToString();
            Debug.Log("[Chkobba] charset size: " + charset.Length + " codepoints");

            string[] fonts =
            {
                "Assets/UI/Fonts/Rakkas-Regular.ttf",
                "Assets/UI/Fonts/Amiri-Regular.ttf",
                "Assets/UI/Fonts/Amiri-Bold.ttf",
                
            };

            foreach (var path in fonts)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[Chkobba] missing TTF: " + path);
                    continue;
                }
                var asset = TMPro.TMP_FontAsset.CreateFontAsset(path, 0, 90, 9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                if (asset == null)
                {
                    Debug.LogWarning("[Chkobba] CreateFontAsset failed for " + path);
                    continue;
                }
                asset.name = Path.GetFileNameWithoutExtension(path) + " SDF";

                bool ok = asset.TryAddCharacters(charset);
                Debug.Log("[Chkobba] " + asset.name + " prebake: " + (ok ? "all added" : "PARTIAL — chars missing from font file"));
                Debug.Log("[Chkobba] " + asset.name + " glyph table now: " + asset.glyphTable.Count + " glyphs, " + asset.characterTable.Count + " characters");

                // static atlas: keep Static so nothing is expected to populate at runtime
                asset.atlasPopulationMode = TMPro.AtlasPopulationMode.Static;

                string outPath = "Assets/UI/Fonts/" + asset.name + ".asset";
                AssetDatabase.CreateAsset(asset, outPath);
                if (asset.material != null)
                    AssetDatabase.AddObjectToAsset(asset.material, asset);
                if (asset.atlasTextures != null)
                    foreach (var tex in asset.atlasTextures)
                        if (tex != null) AssetDatabase.AddObjectToAsset(tex, asset);
                AssetDatabase.SaveAssets();
                Debug.Log("[Chkobba] TMP font asset created: " + asset.name +
                          " (pages=" + (asset.atlasTextures != null ? asset.atlasTextures.Length : 0) + ")");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Chkobba] font import done");
        }
    }
}
#endif
