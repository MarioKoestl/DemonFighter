#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using UnityEditor;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Re-imports the analyzer and test library DLLs so that <see cref="PluginImportRules"/> applies to them.
    /// Needed once after the DLLs are added or updated; later imports pick the rules up on their own.
    /// Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class PluginImportSetup
    {
        private static readonly string[] Folders =
        {
            PluginImportRules.AnalyzersFolder,
            PluginImportRules.TestPluginsFolder,
        };

        [MenuItem("Demon Fighter/Setup/Configure Plugin Imports")]
        public static void Configure()
        {
            var reimported = new List<string>();
            foreach (string folder in Folders)
            {
                string folderWithoutSlash = folder.TrimEnd('/');
                if (!AssetDatabase.IsValidFolder(folderWithoutSlash))
                {
                    continue;
                }

                foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { folderWithoutSlash }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    reimported.Add(path);
                }
            }

            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Configured " + reimported.Count + " plugin import(s): " + string.Join(", ", reimported));
        }
    }
}
