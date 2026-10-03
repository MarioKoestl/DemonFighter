#nullable enable
using System;
using System.Text;
using DemonFighter.Common;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Enforces the import settings of the DLLs under _Project whenever they are imported, so they are right after a
    /// fresh clone or a Library wipe without Inspector clicks. Analyzer DLLs must never be referenced or loaded as
    /// plugins, they only need the RoslynAnalyzer label. Test library DLLs are editor-only and referenced explicitly
    /// by the test assemblies. Settings are written to the .meta file and the DLL is re-imported once when they differ.
    /// </summary>
    internal sealed class PluginImportRules : AssetPostprocessor
    {
        internal const string AnalyzersFolder = "Assets/_Project/Analyzers/";
        internal const string TestPluginsFolder = "Assets/_Project/Tests/Plugins/";
        internal const string RoslynAnalyzerLabel = "RoslynAnalyzer";

        private const string DllExtension = ".dll";
        private const string ExplicitlyReferencedProperty = "m_IsExplicitlyReferenced";
        private const string ValidateReferencesProperty = "m_ValidateReferences";

        // Castle.Core references System.Diagnostics.EventLog, which Unity does not ship and NSubstitute never calls.
        // With validation on, Unity refuses to load it (and NSubstitute with it) on every domain reload.
        private static readonly string[] PluginsWithoutReferenceValidation = { TestPluginsFolder + "Castle.Core.dll" };

        private void OnPreprocessAsset()
        {
            // On its very first import no persisted settings exist yet, and the compile that follows would reference
            // the analyzer like any plugin; its UnityEngine type stubs then collide with the real types in every
            // assembly. Marking it incompatible in memory keeps it out of that compile; the postprocess persists it.
            if (assetImporter is PluginImporter importer && IsDll(assetPath) &&
                assetPath.StartsWith(AnalyzersFolder, StringComparison.Ordinal))
            {
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(false);
                importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
            }
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (!IsDll(path))
                {
                    continue;
                }

                if (path.StartsWith(AnalyzersFolder, StringComparison.Ordinal))
                {
                    ApplyAnalyzerRules(path);
                }
                else if (path.StartsWith(TestPluginsFolder, StringComparison.Ordinal))
                {
                    ApplyTestPluginRules(path);
                }
            }
        }

        private static bool IsDll(string path)
        {
            return path.EndsWith(DllExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyAnalyzerRules(string path)
        {
            PluginImporter? importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            changed |= SetAnyPlatform(importer, false);
            changed |= SetEditor(importer, false);
            changed |= SetPlatform(importer, BuildTarget.StandaloneWindows64, false);
            changed |= EnsureLabel(path, RoslynAnalyzerLabel);
            if (changed)
            {
                importer.SaveAndReimport();
                Log.Info(LogCategory.Editor, "Applied analyzer import settings to " + path + ".");
            }
        }

        private static void ApplyTestPluginRules(string path)
        {
            PluginImporter? importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null)
            {
                return;
            }

            bool validateReferences = Array.IndexOf(PluginsWithoutReferenceValidation, path) < 0;
            bool changed = false;
            changed |= SetAnyPlatform(importer, false);
            changed |= SetEditor(importer, true);
            changed |= SetPlatform(importer, BuildTarget.StandaloneWindows64, false);
            changed |= SetSerializedBool(importer, ExplicitlyReferencedProperty, true);
            changed |= SetSerializedBool(importer, ValidateReferencesProperty, validateReferences);
            if (changed)
            {
                importer.SaveAndReimport();
                Log.Info(LogCategory.Editor, "Applied test plugin import settings to " + path + ".");
            }
        }

        private static bool SetAnyPlatform(PluginImporter importer, bool value)
        {
            if (importer.GetCompatibleWithAnyPlatform() == value)
            {
                return false;
            }

            importer.SetCompatibleWithAnyPlatform(value);
            return true;
        }

        private static bool SetEditor(PluginImporter importer, bool value)
        {
            if (importer.GetCompatibleWithEditor() == value)
            {
                return false;
            }

            importer.SetCompatibleWithEditor(value);
            return true;
        }

        private static bool SetPlatform(PluginImporter importer, BuildTarget target, bool value)
        {
            if (importer.GetCompatibleWithPlatform(target) == value)
            {
                return false;
            }

            importer.SetCompatibleWithPlatform(target, value);
            return true;
        }

        private static bool EnsureLabel(string path, string label)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                return false;
            }

            string[] labels = AssetDatabase.GetLabels(asset);
            if (Array.IndexOf(labels, label) >= 0)
            {
                return false;
            }

            var newLabels = new string[labels.Length + 1];
            Array.Copy(labels, newLabels, labels.Length);
            newLabels[labels.Length] = label;
            AssetDatabase.SetLabels(asset, newLabels);
            return true;
        }

        private static bool SetSerializedBool(PluginImporter importer, string propertyName, bool value)
        {
            using var serialized = new SerializedObject(importer);
            SerializedProperty? property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "PluginImporter has no serialized property named " + propertyName + ". Available: " +
                    ListProperties(serialized));
            }

            if (property.boolValue == value)
            {
                return false;
            }

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static string ListProperties(SerializedObject serialized)
        {
            var names = new StringBuilder();
            SerializedProperty iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.Next(enterChildren))
            {
                names.Append(iterator.propertyPath).Append(", ");
                enterChildren = false;
            }

            return names.ToString();
        }
    }
}
