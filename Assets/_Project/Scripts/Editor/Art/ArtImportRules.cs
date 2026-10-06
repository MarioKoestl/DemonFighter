#nullable enable
using System;
using System.IO;
using UnityEditor;

namespace DemonFighter.Editor.Art
{
    /// <summary>
    /// Applies the import settings of ASSET_PIPELINE ("Import settings") to everything under the art folder as it
    /// is imported, so a dropped model or texture is right without Inspector clicks: models at scale 1 without
    /// colliders, read/write or animation, with their materials extracted next to them under the model's name; textures sRGB only for base
    /// color, normal maps marked as such, sizes capped by the folder budget, BC7 for color and BC5 for normals.
    /// Files under the animations folder import as legacy clips for the clip slot of a part. Raising the version
    /// re-imports everything with the new rules.
    /// </summary>
    internal sealed class ArtImportRules : AssetPostprocessor
    {
        private const uint RulesVersion = 5;
        private const float FleshSmoothingAngle = 80f;
        private const string StandalonePlatform = "Standalone";
        private static readonly string[] NormalHints = { "normal", "_nrm", "_nor", "_n" };
        private static readonly string[] DataHints = { "rough", "metal", "_ao", "occlusion", "orm", "mask", "height", "displace", "smooth", "gloss" };

        public override uint GetVersion()
        {
            return RulesVersion;
        }

        private void OnPreprocessModel()
        {
            if (!(assetImporter is ModelImporter importer))
            {
                return;
            }

            if (ArtFolders.IsAnimationPath(assetPath))
            {
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.isReadable = false;
                importer.addCollider = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
                return;
            }

            if (!ArtFolders.IsModelPath(assetPath))
            {
                return;
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.isReadable = ArtFolders.ReadableMeshes(assetPath);
            importer.addCollider = false;
            importer.meshCompression = ArtFolders.CompressMeshes(assetPath) ? ModelImporterMeshCompression.Medium : ModelImporterMeshCompression.Off;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            // Generated flesh often comes with a normal per triangle, which shades a body like crumpled paper; bodies and
            // parts get smooth normals up to a crease angle that keeps claws and teeth sharp (D-097).
            if (ArtFolders.SmoothNormals(assetPath))
            {
                importer.importNormals = ModelImporterNormals.Calculate;
                importer.normalSmoothingSource = ModelImporterNormalSmoothingSource.PreferSmoothingGroups;
                importer.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
                importer.normalSmoothingAngle = FleshSmoothingAngle;
            }
            else
            {
                importer.importNormals = ModelImporterNormals.Import;
            }
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;

            // Generated models often name their material the same way (Material_0); prefixing the model name keeps
            // two models from sharing, and overwriting, one extracted material.
            importer.materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
        }

        private void OnPreprocessTexture()
        {
            if (!ArtFolders.IsArtPath(assetPath) || !(assetImporter is TextureImporter importer))
            {
                return;
            }

            string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            bool normal = Matches(name, NormalHints);
            bool color = !normal && !Matches(name, DataHints);
            int budget = ArtFolders.TextureBudget(assetPath);

            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = color;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.maxTextureSize = budget;

            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings(StandalonePlatform);
            standalone.overridden = true;
            standalone.maxTextureSize = budget;
            standalone.format = normal ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
            standalone.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(standalone);
        }

        private static bool Matches(string name, string[] hints)
        {
            foreach (string hint in hints)
            {
                if (name.IndexOf(hint, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
