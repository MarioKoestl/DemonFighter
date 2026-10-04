#nullable enable
using System.IO;
using DemonFighter.Common;
using UnityEditor;
using UnityEngine.InputSystem;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Makes the project input actions asset the project-wide one, so the player input adapter and UI Toolkit read
    /// from it (ARCHITECTURE, "Input"). Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class ProjectWideInputActions
    {
        internal const string AssetPath = "Assets/_Project/Settings/DemonFighter.inputactions";

        [MenuItem("Demon Fighter/Setup/Assign Project-Wide Input Actions")]
        public static void Assign()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (asset == null)
            {
                throw new FileNotFoundException("Input actions asset not found.", AssetPath);
            }

            InputSystem.actions = asset;
            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Project-wide input actions set to " + AssetPath + ".");
        }
    }
}
