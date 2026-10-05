#nullable enable
using System;
using System.IO;
using DemonFighter.Common;
using DemonFighter.Editor.Art;
using DemonFighter.Editor.Setup;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Paints the placeholder textures the game needs before real ones exist (ASSET_PIPELINE, "Gore assets" and
    /// D-083): blood splats and a blood pool for the decals, and tileable surfaces (base color, normal, for lava an
    /// emission map) for the ground, the rock, the walls and the lava, as PNG files under the art folder, imported
    /// through the art import rules like any texture Mario drops in. A file that exists is kept, so a hand-made
    /// texture under the same name wins. Public because the generator and the command line call it.
    /// </summary>
    public static class TextureGenerator
    {
        internal const string GeneratedDecalsFolder = ArtFolders.Decals + "/Generated";
        internal const string GeneratedSurfacesFolder = ArtFolders.Surfaces + "/Generated";
        internal const string GeneratedEffectsFolder = ArtFolders.Effects + "/Generated";
        internal const string EmberName = "T_Ember";
        private const int EmberSize = 64;
        internal static readonly string[] SplatNames = { "T_Blood_Splat_01", "T_Blood_Splat_02", "T_Blood_Splat_03" };
        internal const string PoolName = "T_Blood_Pool";

        private const int SplatSize = 512;
        private const int PoolSize = 1024;
        private const int SurfaceSize = 512;
        // Noise cells across one tile. Each noise wraps after exactly the cells it spans; a scaled noise that wrapped
        // later left a seam at every tile edge, a visible grid on large surfaces (D-087).
        private const int AshCells = 8;
        private const int LavaCells = 4;
        private const int LumpCells = 6;
        private const int SeamCells = 5;
        private const int Droplets = 14;
        private const float NormalStrength = 1.5f;
        private const string PngExtension = ".png";
        private static readonly Color BloodColor = new Color(0.30f, 0.012f, 0.008f, 1f);
        private static readonly Color BloodDark = new Color(0.12f, 0.004f, 0.003f, 1f);
        private static readonly Color LavaGlow = new Color(1f, 0.38f, 0.08f, 1f);

        /// <summary>The look of a tileable surface.</summary>
        public enum SurfaceStyle
        {
            /// <summary>Ash floor: fine grain, shallow cracks, warm gray.</summary>
            Ash,

            /// <summary>Rock: coarse lumps and deep seams.</summary>
            Rock,

            /// <summary>Cavern wall: rock, darker and rougher.</summary>
            DarkRock,

            /// <summary>Lava crust: dark plates with glowing veins between them.</summary>
            Lava,
        }

        /// <summary>The textures of one surface, imported.</summary>
        public sealed class SurfaceTextures
        {
            public SurfaceTextures(Texture2D baseColor, Texture2D normal, Texture2D? emission)
            {
                BaseColor = baseColor;
                Normal = normal;
                Emission = emission;
            }

            public Texture2D BaseColor { get; }

            public Texture2D Normal { get; }

            /// <summary>Only lava glows.</summary>
            public Texture2D? Emission { get; }
        }

        /// <summary>The splat textures, painted once and imported; the index is the splat number.</summary>
        public static Texture2D[] EnsureBloodSplats()
        {
            EditorAssets.EnsureFolder(GeneratedDecalsFolder);
            var textures = new Texture2D[SplatNames.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                int seed = i + 1;
                textures[i] = Ensure(GeneratedDecalsFolder + "/" + SplatNames[i] + PngExtension, () => PaintSplat(SplatSize, seed));
            }

            return textures;
        }

        /// <summary>The pool texture, painted once and imported.</summary>
        /// <summary>The soft glowing dot of a spark or an ember (D-086), white so the particle color tints it.</summary>
        public static Texture2D EnsureEmber()
        {
            EditorAssets.EnsureFolder(GeneratedEffectsFolder);
            return Ensure(GeneratedEffectsFolder + "/" + EmberName + PngExtension, () => PaintEmber(EmberSize));
        }

        /// <summary>A white dot with a hot core and a soft falloff; the alpha carries the shape.</summary>
        internal static Texture2D PaintEmber(int size)
        {
            return Paint(size, "Ember", (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                float glow = Mathf.Clamp01(1f - r);
                float alpha = glow * glow * (0.6f + 0.4f * SmoothStep(0.45f, 0f, r));
                return new Color(1f, 1f, 1f, alpha);
            });
        }

        public static Texture2D EnsureBloodPool()
        {
            EditorAssets.EnsureFolder(GeneratedDecalsFolder);
            return Ensure(GeneratedDecalsFolder + "/" + PoolName + PngExtension, () => PaintPool(PoolSize, 11));
        }

        /// <summary>
        /// The tileable textures of a surface, painted once and imported: T_&lt;name&gt;_BaseColor, _Normal and for lava
        /// _Emission. <paramref name="repaint"/> paints them again over the files, after the painters changed.
        /// </summary>
        public static SurfaceTextures EnsureSurface(string name, SurfaceStyle style, int seed, bool repaint = false)
        {
            EditorAssets.EnsureFolder(GeneratedSurfacesFolder);
            string prefix = GeneratedSurfacesFolder + "/T_" + name;
            Texture2D baseColor = Ensure(prefix + "_BaseColor" + PngExtension, () => PaintSurfaceColor(SurfaceSize, style, seed), repaint);
            Texture2D normal = Ensure(prefix + "_Normal" + PngExtension, () => PaintSurfaceNormal(SurfaceSize, style, seed), repaint);
            Texture2D? emission = style == SurfaceStyle.Lava ? Ensure(prefix + "_Emission" + PngExtension, () => PaintLavaEmission(SurfaceSize, seed), repaint) : null;
            return new SurfaceTextures(baseColor, normal, emission);
        }

        /// <summary>A splat: a ragged disc with droplets flung around it, dark toward the center, transparent outside.</summary>
        internal static Texture2D PaintSplat(int size, int seed)
        {
            var random = new System.Random(seed);
            var droplets = new Vector3[Droplets];
            for (int i = 0; i < droplets.Length; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = 0.5f + (float)random.NextDouble() * 0.42f;
                float radius = 0.03f + (float)random.NextDouble() * 0.08f;
                droplets[i] = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, radius);
            }

            return Paint(size, "Blood Splat " + seed, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                float angle = Mathf.Atan2(y, x);
                float ragged = Fbm(Mathf.Cos(angle) * 2.3f + seed * 9f, Mathf.Sin(angle) * 2.3f, 3);
                float edge = 0.4f + 0.3f * ragged;
                float alpha = SmoothStep(edge, edge - 0.22f, r);
                for (int i = 0; i < droplets.Length; i++)
                {
                    float dx = x - droplets[i].x;
                    float dy = y - droplets[i].y;
                    float dropletAlpha = SmoothStep(droplets[i].z, droplets[i].z * 0.5f, Mathf.Sqrt(dx * dx + dy * dy));
                    alpha = Mathf.Max(alpha, dropletAlpha);
                }

                float grain = Fbm(x * 9f + seed, y * 9f, 2);
                alpha *= 0.75f + 0.25f * grain;
                float darkness = Mathf.Clamp01(1f - r) * 0.5f + grain * 0.2f;
                Color color = Color.Lerp(BloodColor, BloodDark, darkness);
                color.a = Mathf.Clamp01(alpha);
                return color;
            });
        }

        /// <summary>A pool: a nearly round disc with a soft, slightly ragged rim, dark and dense inside.</summary>
        internal static Texture2D PaintPool(int size, int seed)
        {
            return Paint(size, "Blood Pool", (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                float angle = Mathf.Atan2(y, x);
                float ragged = Fbm(Mathf.Cos(angle) * 1.7f + seed * 3f, Mathf.Sin(angle) * 1.7f, 3);
                float edge = 0.72f + 0.18f * ragged;
                float alpha = SmoothStep(edge, edge - 0.3f, r);
                float grain = Fbm(x * 5f + seed, y * 5f, 2);
                Color color = Color.Lerp(BloodColor, BloodDark, 0.35f + 0.4f * grain);
                color.a = Mathf.Clamp01(alpha * (0.9f + 0.1f * grain));
                return color;
            });
        }

        /// <summary>The base color of a surface: its palette shaded by the height, seams and cracks darker, lava veins glowing.</summary>
        internal static Texture2D PaintSurfaceColor(int size, SurfaceStyle style, int seed)
        {
            return PaintTile(size, "Surface " + style, (u, v) =>
            {
                float height = SurfaceHeight(u, v, style, seed);
                float veins = style == SurfaceStyle.Lava ? LavaVeins(u, v, seed) : 0f;
                Color color = SurfaceColor(style, height);
                color = Color.Lerp(color, LavaGlow, veins);
                color.a = 1f;
                return color;
            });
        }

        /// <summary>The tangent-space normal map of a surface, from the slope of its height.</summary>
        internal static Texture2D PaintSurfaceNormal(int size, SurfaceStyle style, int seed)
        {
            float step = 1f / size;
            return PaintTile(size, "Surface " + style + " Normal", (u, v) =>
            {
                float right = SurfaceHeight(u + step, v, style, seed);
                float left = SurfaceHeight(u - step, v, style, seed);
                float up = SurfaceHeight(u, v + step, style, seed);
                float down = SurfaceHeight(u, v - step, style, seed);
                float slopeX = (right - left) * NormalStrength * size / 256f;
                float slopeY = (up - down) * NormalStrength * size / 256f;
                return EncodeNormal(slopeX, slopeY);
            });
        }

        /// <summary>The glow of the lava: bright where the veins between the crust plates run, black on the plates.</summary>
        internal static Texture2D PaintLavaEmission(int size, int seed)
        {
            return PaintTile(size, "Lava Emission", (u, v) =>
            {
                float veins = LavaVeins(u, v, seed);
                Color color = LavaGlow * veins;
                color.a = 1f;
                return color;
            });
        }

        /// <summary>Height of a surface at tile coordinates in [0, 1), in [0, 1], the same at both ends of the tile.</summary>
        internal static float SurfaceHeight(float u, float v, SurfaceStyle style, int seed)
        {
            switch (style)
            {
                case SurfaceStyle.Ash:
                    {
                        float grain = TiledFbm(Cells(u, AshCells), Cells(v, AshCells), AshCells, 4, seed);
                        float cracks = Cracks(u, v, AshCells, seed + 7);
                        return Mathf.Clamp01(0.35f + 0.5f * grain - 0.3f * cracks);
                    }

                case SurfaceStyle.Lava:
                    {
                        float plates = TiledFbm(Cells(u, LavaCells), Cells(v, LavaCells), LavaCells, 3, seed);
                        float veins = LavaVeins(u, v, seed);
                        return Mathf.Clamp01(0.5f + 0.4f * plates - 0.6f * veins);
                    }

                default:
                    {
                        float lumps = TiledFbm(Cells(u, LumpCells), Cells(v, LumpCells), LumpCells, 5, seed);
                        float seams = Cracks(u, v, SeamCells, seed + 3);
                        return Mathf.Clamp01(0.2f + 0.8f * lumps - 0.45f * seams);
                    }
            }
        }

        /// <summary>Tileable value noise with a few octaves, in [0, 1]: the lattice wraps every period cells, so a tile repeats seamlessly.</summary>
        internal static float TiledFbm(float x, float y, int period, int octaves, int seed)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            int currentPeriod = Mathf.Max(1, period);
            for (int i = 0; i < octaves; i++)
            {
                sum += TiledValueNoise(x, y, currentPeriod, seed + i * 17) * amplitude;
                total += amplitude;
                x *= 2f;
                y *= 2f;
                currentPeriod *= 2;
                amplitude *= 0.5f;
            }

            return total > 0f ? sum / total : 0f;
        }

        /// <summary>Smooth value noise with a few octaves, in [0, 1].</summary>
        internal static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(x, y) * amplitude;
                total += amplitude;
                x = x * 2.1f + 13.7f;
                y = y * 2.1f + 7.3f;
                amplitude *= 0.5f;
            }

            return total > 0f ? sum / total : 0f;
        }

        /// <summary>Packs a surface slope into a tangent-space normal map color: flat is (0.5, 0.5, 1), a rise toward +X tilts the normal toward -X.</summary>
        internal static Color EncodeNormal(float slopeX, float slopeY)
        {
            var normal = new Vector3(-slopeX, -slopeY, 1f).normalized;
            return new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
        }

        // Thin bright ridges of a ridged noise, 0 almost everywhere and 1 along the lines; the cracks of a surface.
        // A tile coordinate in [0, 1) spread over the noise cells of one tile.
        private static float Cells(float tile, int cells)
        {
            return Mathf.Repeat(tile, 1f) * cells;
        }

        private static float Cracks(float u, float v, int cells, int seed)
        {
            float ridge = 1f - Mathf.Abs(TiledFbm(Cells(u, cells), Cells(v, cells), cells, 3, seed) * 2f - 1f);
            return Mathf.Pow(Mathf.Clamp01(ridge), 6f);
        }

        private static float LavaVeins(float u, float v, int seed)
        {
            return Cracks(u, v, LavaCells, seed + 11);
        }

        // Base colors are albedo, the share of light a surface reflects, not how dark it looks: ash and volcanic rock
        // reflect about a tenth. The cave gets its darkness from its light; near-black albedo left nothing to light (D-087).
        private static Color SurfaceColor(SurfaceStyle style, float height)
        {
            switch (style)
            {
                case SurfaceStyle.Ash:
                    return new Color(0.38f, 0.34f, 0.31f) * (0.5f + 0.9f * height);
                case SurfaceStyle.Rock:
                    return new Color(0.41f, 0.36f, 0.34f) * (0.45f + 0.9f * height);
                case SurfaceStyle.DarkRock:
                    return new Color(0.33f, 0.29f, 0.27f) * (0.45f + 0.9f * height);
                default:
                    return new Color(0.14f, 0.045f, 0.025f) * (0.4f + 0.9f * height);
            }
        }

        private static Texture2D Ensure(string path, Func<Texture2D> paint, bool repaint = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null && !repaint)
            {
                return existing;
            }

            Texture2D painted = paint();
            byte[] png = painted.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(painted);
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (imported == null)
            {
                throw new IOException("Painted " + path + " but Unity did not import it.");
            }

            Log.Info(LogCategory.Editor, (existing != null ? "Repainted" : "Painted") + " placeholder texture " + path + ".");
            return imported;
        }

        // Pixel coordinates run from -1 to 1 across the texture; the painter returns the color of one pixel.
        private static Texture2D Paint(int size, string name, Func<float, float, Color> painter)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    pixels[y * size + x] = painter(u, v);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        // Tile coordinates run from 0 to 1 across the texture, so the painter can repeat seamlessly.
        private static Texture2D PaintTile(int size, string name, Func<float, float, Color> painter)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    pixels[y * size + x] = painter(u, v);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static float SmoothStep(float from, float to, float value)
        {
            float t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        private static float ValueNoise(float x, float y)
        {
            float cellX = Mathf.Floor(x);
            float cellY = Mathf.Floor(y);
            float fx = Smooth(x - cellX);
            float fy = Smooth(y - cellY);
            float a = Hash(cellX, cellY);
            float b = Hash(cellX + 1f, cellY);
            float c = Hash(cellX, cellY + 1f);
            float d = Hash(cellX + 1f, cellY + 1f);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        // The lattice wraps at the period, so the noise at x equals the noise at x plus the period.
        private static float TiledValueNoise(float x, float y, int period, int seed)
        {
            int cellX = Mathf.FloorToInt(x);
            int cellY = Mathf.FloorToInt(y);
            float fx = Smooth(x - cellX);
            float fy = Smooth(y - cellY);
            float a = Hash(Wrap(cellX, period), Wrap(cellY, period), seed);
            float b = Hash(Wrap(cellX + 1, period), Wrap(cellY, period), seed);
            float c = Hash(Wrap(cellX, period), Wrap(cellY + 1, period), seed);
            float d = Hash(Wrap(cellX + 1, period), Wrap(cellY + 1, period), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static int Wrap(int value, int period)
        {
            int wrapped = value % period;
            return wrapped < 0 ? wrapped + period : wrapped;
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float Hash(float x, float y)
        {
            float value = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        private static float Hash(int x, int y, int seed)
        {
            uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)(seed * 2246822519);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }
}
