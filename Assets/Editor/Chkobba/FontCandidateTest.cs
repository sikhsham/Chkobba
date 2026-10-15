#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Chkobba.EditorTools
{
    /// <summary>Test harness: imports candidate fonts and reports shaping coverage.</summary>
    public static class FontCandidateTest
    {
        [MenuItem("Chkobba/Test Candidate Fonts")]
        public static void Run()
        {
            string[] candidates =
            {
                "Assets/UI/Fonts/Candidates/Amiri-Regular.ttf",
                "Assets/UI/Fonts/Candidates/Scheherazade-New.ttf",
                "Assets/UI/Fonts/Candidates/Harmattan.ttf",
                "Assets/UI/Fonts/Candidates/Lateef-Regular.ttf",
                "Assets/UI/Fonts/Candidates/Reem-Kufi.ttf",
            };

            // full presentation forms B + lam-alef + arabic base
            var charset = new System.Text.StringBuilder();
            for (int c = 0x0600; c <= 0x06FF; c++) charset.Append((char)c);
            for (int c = 0xFE70; c <= 0xFEFF; c++) charset.Append((char)c);
            for (int c = 0x0020; c <= 0x00FF; c++) charset.Append((char)c);
            charset.Append("\u2013\u2014\u2018\u2019\u201C\u201D\u2026");
            string charsetStr = charset.ToString();

            var report = new System.Text.StringBuilder();
            foreach (var path in candidates)
            {
                if (!System.IO.File.Exists(path)) { report.Append(path).Append(": MISSING; "); continue; }
                var asset = TMPro.TMP_FontAsset.CreateFontAsset(path, 0, 90, 9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                if (asset == null) { report.Append(System.IO.Path.GetFileName(path)).Append(": CREATE FAILED; "); continue; }
                asset.TryAddCharacters(charsetStr);

                var table = new System.Collections.Generic.HashSet<int>();
                foreach (var ch in asset.characterTable) table.Add((int)ch.unicode);

                // count how many of the 140 presentation forms we care about landed
                int presCount = 0;
                for (int c = 0xFE70; c <= 0xFEFF; c++) if (table.Contains(c)) presCount++;
                int lamAlef = 0;
                foreach (int c in new[] { 0xFEF5, 0xFEF6, 0xFEF7, 0xFEF8, 0xFEF9, 0xFEFA, 0xFEFB, 0xFEFC })
                    if (table.Contains(c)) lamAlef++;

                report.Append(System.IO.Path.GetFileName(path));
                report.Append(": presB=").Append(presCount.ToString()).Append("/140 lamAlef=").Append(lamAlef.ToString()).Append("/8; ");

                UnityEngine.Object.DestroyImmediate(asset);
            }
            Debug.Log("[Chkobba] candidate report: " + report.ToString());
        }
    }
}
#endif
