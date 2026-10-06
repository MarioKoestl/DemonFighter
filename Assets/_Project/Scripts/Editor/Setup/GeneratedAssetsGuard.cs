#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Editor.Generate;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Reruns Demon Fighter > Generate > Placeholder Assets once when the generated assets are older than the code
    /// (D-087): after a pull or a change that adds materials, textures, sounds or migrations, the editor brings them up
    /// to date by itself instead of waiting for someone to remember the menu. Never in batch mode, never in Play Mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class GeneratedAssetsGuard
    {
        static GeneratedAssetsGuard()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.delayCall += Check;
            }
        }

        private static void Check()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            string? reason = PlaceholderAssetGenerator.OutdatedReason();
            if (reason == null)
            {
                return;
            }

            Log.Info(LogCategory.Editor, "Generated assets are out of date (" + reason + "); running Demon Fighter > Generate > Placeholder Assets.");
            try
            {
                PlaceholderAssetGenerator.Generate();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.Editor, "Updating the generated assets failed; run Demon Fighter > Generate > Placeholder Assets by hand.", exception);
            }
        }
    }
}
