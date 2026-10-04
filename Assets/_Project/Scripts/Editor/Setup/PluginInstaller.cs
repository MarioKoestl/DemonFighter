#nullable enable
using System;
using System.IO;
using DemonFighter.Common;
using UnityEditor;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Copies a DLL into the project and imports it while <see cref="PluginImportRules"/> is already compiled, so the
    /// very first import gets the right settings. A Roslyn analyzer dropped into Assets by hand is imported as a normal
    /// plugin and breaks the compilation of every assembly before any editor code could fix its settings.
    /// Command line: -executeMethod DemonFighter.Editor.Setup.PluginInstaller.InstallFromCommandLine
    /// -pluginSource "path/to/file.dll" -pluginTarget "Assets/_Project/Analyzers/file.dll".
    /// Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class PluginInstaller
    {
        private const string SourceArgument = "-pluginSource";
        private const string TargetArgument = "-pluginTarget";

        public static void InstallFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            Install(RequireArgument(args, SourceArgument), RequireArgument(args, TargetArgument));
        }

        internal static void Install(string sourceFile, string targetAssetPath)
        {
            if (!File.Exists(sourceFile))
            {
                throw new FileNotFoundException("Plugin source file not found.", sourceFile);
            }

            if (!targetAssetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Target must be an asset path below Assets/.", nameof(targetAssetPath));
            }

            string? targetFolder = Path.GetDirectoryName(targetAssetPath)?.Replace('\\', '/');
            if (targetFolder == null || !AssetDatabase.IsValidFolder(targetFolder))
            {
                throw new ArgumentException("Target folder is not an imported asset folder: " + targetFolder);
            }

            File.Copy(sourceFile, targetAssetPath, overwrite: true);
            AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Installed " + targetAssetPath + " from " + sourceFile + ".");
        }

        private static string RequireArgument(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length)
            {
                throw new ArgumentException("Missing command line argument " + name + " <value>.");
            }

            return args[index + 1];
        }
    }
}
