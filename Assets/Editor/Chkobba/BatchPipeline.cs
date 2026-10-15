#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Chkobba.EditorTools
{
    /// <summary>
    /// Batch entry: refreshes assets, imports fonts, builds the MainMenu scene,
    /// configures build settings, then exits. Also: Chkobba > Configure Import Settings
    /// (point filtering + uncompressed RGBA for UI assets — kills gradient banding).
    /// </summary>
    public static class BatchPipeline
    {
        public static void BuildMainMenu()
        {
            Debug.Log("[Chkobba] batch: start");
            AssetDatabase.Refresh();

            ConfigureImportSettings();
            MenuFontImporter.Import();
            MainMenuSceneBuilder.Build();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("[Chkobba] batch: done");
        }

        [MenuItem("Chkobba/Configure Import Settings")]
        public static void ConfigureImportSettings()
        {
            string[] uiFolders = { "Assets/UI/MainMenu", "Assets/UI/MainMenu/icons" };
            int configured = 0;

            foreach (var folder in uiFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    // skip the painting: it's photographic, keep default compression
                    if (path.EndsWith("cafe-scene.jpg")) continue;

                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    if (importer == null) continue;
                    bool changed = false;

                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                    if (importer.filterMode != FilterMode.Point)
                    { importer.filterMode = FilterMode.Point; changed = true; }
                    if (importer.mipmapEnabled)
                    { importer.mipmapEnabled = false; changed = true; }
                    if (importer.alphaIsTransparency != true)
                    { importer.alphaIsTransparency = true; changed = true; }

                    if (changed) { importer.SaveAndReimport(); configured++; }
                }
            }
            Debug.Log("[Chkobba] import settings configured on " + configured + " textures (uncompressed, point, no mipmaps)");
        }

        [MenuItem("Chkobba/Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            var menuGuid = AssetDatabase.AssetPathToGUID("Assets/Scenes/MainMenu.unity");
            var gameGuid = AssetDatabase.AssetPathToGUID("Assets/Scenes/GameScene.unity");

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (!string.IsNullOrEmpty(menuGuid))
                scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true));
            if (!string.IsNullOrEmpty(gameGuid))
                scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true));

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Chkobba] build settings: " + string.Join(" -> ", scenes.Select(s => s.path).ToArray()));
        }

        /// <summary>Editor diagnostics: lists every scene script GUID and whether it resolves.</summary>
        [MenuItem("Chkobba/Diagnose Missing Scripts")]
        public static void DiagnoseMissingScripts()
        {
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                CheckTree(go);
            }
            Debug.Log("[Chkobba] missing-script scan complete");
        }

        private static void CheckTree(GameObject go)
        {
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                    Debug.LogWarning("[Chkobba] MISSING SCRIPT on '" + go.name + "' (component index " + i + ")", go);
            }
            foreach (Transform child in go.transform) CheckTree(child.gameObject);
        }
    }
}
#endif
