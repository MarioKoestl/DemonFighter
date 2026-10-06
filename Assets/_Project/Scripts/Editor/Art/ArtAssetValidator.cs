#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Setup;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Art
{
    /// <summary>
    /// Checks every model and texture under the art folder against ASSET_PIPELINE (menu, or -executeMethod in batch
    /// mode) and writes the findings to the Console and to TestResults/art-validation.txt. Errors mean the asset
    /// cannot ship as it is; warnings point at things worth a look. Public because the command line has to find it.
    /// </summary>
    public static class ArtAssetValidator
    {
        internal const string MenuPath = "Demon Fighter/Art/Validate Art Assets";
        internal const string LicensesPath = "docs/ASSET_LICENSES.md";
        internal const string ReportFolder = "TestResults";
        internal const string ReportPath = ReportFolder + "/art-validation.txt";

        private const string TextureFilter = "t:Texture2D";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            ArtValidationReport report = Validate();
            Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(ReportPath, report.ToText());
            if (report.Errors.Count > 0)
            {
                Log.Error(LogCategory.Editor, report.Summary + " See " + ReportPath + ".\n" + report.ToText());
            }
            else
            {
                Log.Info(LogCategory.Editor, report.Summary + " See " + ReportPath + ".");
            }
        }

        /// <summary>Gathers the facts from the asset database and runs the rules.</summary>
        internal static ArtValidationReport Validate()
        {
            string licenses = File.Exists(LicensesPath) ? File.ReadAllText(LicensesPath) : string.Empty;
            var models = new List<ModelFacts>();
            foreach (string path in ArtModelAssets.FindModelPaths())
            {
                ArtModelAssets.ModelContents? contents = ArtModelAssets.Load(path);
                if (contents == null)
                {
                    continue;
                }

                var states = new List<MeshState>(contents.States.Keys);
                var sockets = new List<string>(contents.Sockets.Count);
                foreach (ArtModelAssets.ModelSocket socket in contents.Sockets)
                {
                    sockets.Add(socket.Name);
                }

                models.Add(new ModelFacts
                {
                    Path = path,
                    Name = contents.Name,
                    Triangles = contents.Triangles,
                    TriangleBudget = ArtFolders.TriangleBudget(path),
                    States = states,
                    Sockets = sockets,
                    HasLicenseEntry = HasLicenseEntry(licenses, path),
                });
            }

            var textures = new List<TextureFacts>();
            if (AssetDatabase.IsValidFolder(ArtFolders.Root))
            {
                foreach (string guid in AssetDatabase.FindAssets(TextureFilter, new[] { ArtFolders.Root }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                    {
                        textures.Add(new TextureFacts { Path = path, MaxSize = importer.maxTextureSize, Budget = ArtFolders.TextureBudget(path) });
                    }
                }
            }

            var parts = new List<PartFacts>();
            foreach (BodyPartDefinition definition in EditorAssets.FindAll<BodyPartDefinition>())
            {
                parts.Add(new PartFacts { Id = definition.Id, Fate = definition.Fate, IsCore = definition.IsCore });
            }

            return ArtValidationRules.Check(models, textures, parts);
        }

        /// <summary>An entry counts when the license log names the file, its folder or its name without extension.</summary>
        internal static bool HasLicenseEntry(string licenses, string assetPath)
        {
            if (string.IsNullOrEmpty(licenses))
            {
                return false;
            }

            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            return licenses.IndexOf(assetPath, StringComparison.OrdinalIgnoreCase) >= 0
                || (folder.Length > ArtFolders.Models.Length && licenses.IndexOf(folder, StringComparison.OrdinalIgnoreCase) >= 0)
                || licenses.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
