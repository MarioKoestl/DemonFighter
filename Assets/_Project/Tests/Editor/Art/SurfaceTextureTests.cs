#nullable enable
using AwesomeAssertions;
using DemonFighter.Editor.Generate;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Art
{
    public sealed class SurfaceTextureTests
    {
        private const int Size = 32;
        private const float Tolerance = 0.0001f;

        [Test]
        public void TiledFbm_RepeatsEveryPeriod()
        {
            for (int i = 0; i < 20; i++)
            {
                float x = i * 0.37f;
                float y = i * 1.13f;

                float here = TextureGenerator.TiledFbm(x, y, 8, 4, 3);
                float oneTileOver = TextureGenerator.TiledFbm(x + 8f, y, 8, 4, 3);
                float oneTileUp = TextureGenerator.TiledFbm(x, y + 8f, 8, 4, 3);

                here.Should().BeInRange(0f, 1f);
                oneTileOver.Should().BeApproximately(here, Tolerance);
                oneTileUp.Should().BeApproximately(here, Tolerance);
            }
        }

        [Test]
        public void TiledFbm_DifferentSeeds_DifferentNoise()
        {
            float first = TextureGenerator.TiledFbm(1.3f, 2.7f, 8, 3, 1);
            float second = TextureGenerator.TiledFbm(1.3f, 2.7f, 8, 3, 2);

            first.Should().NotBeApproximately(second, 0.001f);
        }

        [Test]
        public void SurfaceHeight_WrapsAroundTheTile()
        {
            foreach (TextureGenerator.SurfaceStyle style in System.Enum.GetValues(typeof(TextureGenerator.SurfaceStyle)))
            {
                float left = TextureGenerator.SurfaceHeight(0.001f, 0.4f, style, 5);
                float right = TextureGenerator.SurfaceHeight(1.001f, 0.4f, style, 5);

                left.Should().BeInRange(0f, 1f);
                right.Should().BeApproximately(left, Tolerance);
            }
        }

        // A scaled noise that wrapped later than the tile left a seam at every tile edge of the lava, rock and wall (D-087).
        [Test]
        public void SurfaceHeight_IsContinuousAcrossTheTileEdge()
        {
            const int Samples = 64;
            const float Step = 0.0002f;
            foreach (TextureGenerator.SurfaceStyle style in System.Enum.GetValues(typeof(TextureGenerator.SurfaceStyle)))
            {
                float jumpU = 0f;
                float jumpV = 0f;
                for (int i = 0; i < Samples; i++)
                {
                    float along = (i + 0.5f) / Samples;
                    jumpU += Mathf.Abs(TextureGenerator.SurfaceHeight(1f - Step, along, style, 5) - TextureGenerator.SurfaceHeight(Step, along, style, 5));
                    jumpV += Mathf.Abs(TextureGenerator.SurfaceHeight(along, 1f - Step, style, 5) - TextureGenerator.SurfaceHeight(along, Step, style, 5));
                }

                (jumpU / Samples).Should().BeLessThan(0.01f, style + " must not jump across the left and right tile edge");
                (jumpV / Samples).Should().BeLessThan(0.01f, style + " must not jump across the top and bottom tile edge");
            }
        }

        [Test]
        public void EncodeNormal_Flat_PointsStraightUp()
        {
            Color flat = TextureGenerator.EncodeNormal(0f, 0f);

            flat.r.Should().BeApproximately(0.5f, Tolerance);
            flat.g.Should().BeApproximately(0.5f, Tolerance);
            flat.b.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void EncodeNormal_RiseTowardX_TiltsTheNormalAgainstIt()
        {
            Color rising = TextureGenerator.EncodeNormal(1f, 0f);
            Color falling = TextureGenerator.EncodeNormal(-1f, 0f);

            rising.r.Should().BeLessThan(0.5f);
            falling.r.Should().BeGreaterThan(0.5f);
            rising.g.Should().BeApproximately(0.5f, Tolerance);
            rising.b.Should().BeLessThan(1f);
        }

        // Near-black albedo left the light nothing to show, and the cave read as black (D-087).
        [TestCase(TextureGenerator.SurfaceStyle.Ash)]
        [TestCase(TextureGenerator.SurfaceStyle.Rock)]
        [TestCase(TextureGenerator.SurfaceStyle.DarkRock)]
        public void PaintSurfaceColor_IsOpaqueGrayOfRealisticBrightness(TextureGenerator.SurfaceStyle style)
        {
            Texture2D texture = TextureGenerator.PaintSurfaceColor(Size, style, 1);
            try
            {
                Color[] pixels = texture.GetPixels();
                float sum = 0f;
                foreach (Color pixel in pixels)
                {
                    pixel.a.Should().Be(1f);
                    pixel.maxColorComponent.Should().BeLessThan(0.7f, "ash and rock are gray, not white");
                    sum += pixel.grayscale;
                }

                (sum / pixels.Length).Should().BeInRange(0.2f, 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void PaintLavaEmission_GlowsSomewhereAndNotEverywhere()
        {
            Texture2D texture = TextureGenerator.PaintLavaEmission(Size, 57);
            try
            {
                Color[] pixels = texture.GetPixels();
                int glowing = 0;
                foreach (Color pixel in pixels)
                {
                    if (pixel.r > 0.3f)
                    {
                        glowing++;
                    }
                }

                glowing.Should().BeGreaterThan(0);
                glowing.Should().BeLessThan(pixels.Length / 2);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void PaintSurfaceNormal_IsMostlyUpright()
        {
            Texture2D texture = TextureGenerator.PaintSurfaceNormal(Size, TextureGenerator.SurfaceStyle.Rock, 33);
            try
            {
                Color[] pixels = texture.GetPixels();
                float averageBlue = 0f;
                foreach (Color pixel in pixels)
                {
                    averageBlue += pixel.b;
                }

                averageBlue /= pixels.Length;
                averageBlue.Should().BeGreaterThan(0.75f);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
