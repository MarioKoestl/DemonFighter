#nullable enable
using System.Collections.Generic;
using DemonFighter.Common;
using UnityEditor;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Deletes the leftovers of the Universal 3D template through the AssetDatabase, so their .meta files go with them
    /// (CLAUDE.md rule 5). Safe to run again: assets that are already gone are skipped.
    /// Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class TemplateCleanup
    {
        // Order matters: the Readme asset goes before the scripts that define its type.
        private static readonly string[] TemplateAssets =
        {
            "Assets/Readme.asset",
            "Assets/TutorialInfo",
            "Assets/Scenes/SampleScene.unity",
            "Assets/InputSystem_Actions.inputactions",
        };

        private static readonly string[] FoldersToRemoveWhenEmpty =
        {
            "Assets/Scenes",
        };

        [MenuItem("Demon Fighter/Setup/Remove Template Assets")]
        public static void Remove()
        {
            var removed = new List<string>();
            foreach (string path in TemplateAssets)
            {
                if (Exists(path))
                {
                    Delete(path, removed);
                }
            }

            foreach (string folder in FoldersToRemoveWhenEmpty)
            {
                if (AssetDatabase.IsValidFolder(folder) && AssetDatabase.FindAssets(string.Empty, new[] { folder }).Length == 0)
                {
                    Delete(folder, removed);
                }
            }

            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Removed " + removed.Count + " template asset(s): " + string.Join(", ", removed));
        }

        private static bool Exists(string path)
        {
            return AssetDatabase.IsValidFolder(path) ||
                   AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets).Length > 0;
        }

        private static void Delete(string path, List<string> removed)
        {
            if (AssetDatabase.DeleteAsset(path))
            {
                removed.Add(path);
            }
            else
            {
                Log.Warn(LogCategory.Editor, "Could not delete " + path + ".");
            }
        }
    }
}
