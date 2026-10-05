#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.Demons;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class MeshBoundsTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Transform_Identity_KeepsTheBounds()
        {
            var bounds = new Bounds(new Vector3(1f, 2f, 3f), new Vector3(1f, 2f, 3f));

            Bounds result = MeshBounds.Transform(bounds, Matrix4x4.identity);

            result.center.Should().Be(bounds.center);
            result.size.Should().Be(bounds.size);
        }

        [Test]
        public void Transform_QuarterTurnAboutY_SwapsWidthAndDepth()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(1f, 2f, 3f));

            Bounds result = MeshBounds.Transform(bounds, Matrix4x4.Rotate(Quaternion.Euler(0f, 90f, 0f)));

            result.size.x.Should().BeApproximately(3f, Tolerance);
            result.size.y.Should().BeApproximately(2f, Tolerance);
            result.size.z.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void Transform_TranslationAndScale_MoveAndGrowTheBox()
        {
            var bounds = new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);

            Bounds result = MeshBounds.Transform(bounds, Matrix4x4.TRS(new Vector3(0f, 1f, 0f), Quaternion.identity, Vector3.one * 2f));

            result.min.y.Should().BeApproximately(1f, Tolerance);
            result.max.y.Should().BeApproximately(3f, Tolerance);
            result.size.x.Should().BeApproximately(2f, Tolerance);
        }
    }
}
