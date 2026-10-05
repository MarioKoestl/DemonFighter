#nullable enable
using AwesomeAssertions;
using DemonFighter.Editor.Generate;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Art
{
    public sealed class BloodTextureTests
    {
        private const int Size = 64;

        [Test]
        public void PaintSplat_IsOpaqueInTheMiddleAndClearInTheCorners()
        {
            Texture2D splat = TextureGenerator.PaintSplat(Size, 1);
            try
            {
                Color center = splat.GetPixel(Size / 2, Size / 2);
                Color corner = splat.GetPixel(0, 0);

                center.a.Should().BeGreaterThan(0.7f);
                center.r.Should().BeGreaterThan(center.g * 5f);
                corner.a.Should().Be(0f);
            }
            finally
            {
                Object.DestroyImmediate(splat);
            }
        }

        [Test]
        public void PaintSplat_DifferentSeeds_DifferInShape()
        {
            Texture2D first = TextureGenerator.PaintSplat(Size, 1);
            Texture2D second = TextureGenerator.PaintSplat(Size, 2);
            try
            {
                Color[] a = first.GetPixels();
                Color[] b = second.GetPixels();
                int differing = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    if (Mathf.Abs(a[i].a - b[i].a) > 0.05f)
                    {
                        differing++;
                    }
                }

                differing.Should().BeGreaterThan(a.Length / 50);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void PaintPool_IsDenseInsideAndClearOutside()
        {
            Texture2D pool = TextureGenerator.PaintPool(Size, 11);
            try
            {
                Color center = pool.GetPixel(Size / 2, Size / 2);
                Color halfway = pool.GetPixel(Size / 2 + Size / 5, Size / 2);
                Color corner = pool.GetPixel(0, Size - 1);

                center.a.Should().BeGreaterThan(0.85f);
                halfway.a.Should().BeGreaterThan(0.85f);
                corner.a.Should().Be(0f);
            }
            finally
            {
                Object.DestroyImmediate(pool);
            }
        }

        [Test]
        public void PaintEmber_IsAWhiteDotThatFadesToNothing()
        {
            Texture2D ember = TextureGenerator.PaintEmber(Size);
            try
            {
                Color center = ember.GetPixel(Size / 2, Size / 2);
                Color edge = ember.GetPixel(0, Size / 2);

                center.a.Should().BeGreaterThan(0.8f);
                center.r.Should().Be(1f);
                center.g.Should().Be(1f);
                edge.a.Should().BeLessThan(0.05f);
            }
            finally
            {
                Object.DestroyImmediate(ember);
            }
        }

        [Test]
        public void Fbm_StaysInUnitRange()
        {
            for (int i = 0; i < 50; i++)
            {
                float value = TextureGenerator.Fbm(i * 0.37f, i * 1.91f, 3);

                value.Should().BeInRange(0f, 1f);
            }
        }
    }
}
