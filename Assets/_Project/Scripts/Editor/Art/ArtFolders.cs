#nullable enable
using System;

namespace DemonFighter.Editor.Art
{
    /// <summary>Where art lives and what each folder may cost (ASSET_PIPELINE, "Budgets" and "Import settings").</summary>
    internal static class ArtFolders
    {
        public const string Root = "Assets/_Project/Art";
        public const string Models = Root + "/Models";
        public const string Core = Models + "/Core";
        public const string BodyParts = Models + "/BodyParts";
        public const string Elders = Models + "/Elders";
        public const string World = Models + "/World";
        public const string Animations = Root + "/Animations";
        public const string Textures = Root + "/Textures";
        public const string Decals = Textures + "/Decals";
        public const string Effects = Textures + "/Effects";
        public const string Surfaces = Textures + "/Surfaces";

        private const int CoreTriangles = 8500;
        private const int BodyPartTriangles = 5000;
        private const int ElderTriangles = 40000;
        private const int WorldTriangles = 3000;
        private const int CoreTextureSize = 2048;
        private const int ElderTextureSize = 4096;
        private const int DefaultTextureSize = 1024;

        /// <summary>The folders the generator creates so there is a place to drop files into.</summary>
        public static readonly string[] All = { Core, BodyParts, Elders, World, Animations, Decals, Surfaces, Effects };

        public static bool IsArtPath(string path) => IsUnder(path, Root);

        public static bool IsModelPath(string path) => IsUnder(path, Models);

        /// <summary>Hand-made clips for parts (D-082) live apart from the models, so their import keeps the animation.</summary>
        public static bool IsAnimationPath(string path) => IsUnder(path, Animations);

        /// <summary>Triangle budget of a model by folder; a model outside the known folders counts as a body part.</summary>
        public static int TriangleBudget(string path)
        {
            if (IsUnder(path, Core))
            {
                return CoreTriangles;
            }

            if (IsUnder(path, Elders))
            {
                return ElderTriangles;
            }

            if (IsUnder(path, World))
            {
                return WorldTriangles;
            }

            return BodyPartTriangles;
        }

        /// <summary>Largest texture side by folder.</summary>
        public static int TextureBudget(string path)
        {
            if (IsUnder(path, Core))
            {
                return CoreTextureSize;
            }

            return IsUnder(path, Elders) ? ElderTextureSize : DefaultTextureSize;
        }

        /// <summary>World features are rigid and may be compressed; bodies and parts keep their exact vertices.</summary>
        public static bool CompressMeshes(string path) => IsUnder(path, World);

        /// <summary>Body part meshes stay readable, so the view can mirror a part for the left flank (D-088).</summary>
        public static bool ReadableMeshes(string path) => IsUnder(path, BodyParts);

        private static bool IsUnder(string path, string folder)
        {
            return path.StartsWith(folder + "/", StringComparison.Ordinal);
        }
    }
}
