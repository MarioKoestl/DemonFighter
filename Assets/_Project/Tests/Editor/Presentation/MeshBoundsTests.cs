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

        // A diamond turned by 45 degrees: its lowest vertex is 0.71 down, the turned corner of its box 1.41.
        [Test]
        public void LowestY_ReadableMesh_IsExactFromTheVertices()
        {
            Mesh diamond = Diamond();
            try
            {
                MeshBounds.LowestY(diamond, Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 45f))).Should().BeApproximately(-0.7071f, Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(diamond);
            }
        }

        [Test]
        public void LowestY_UnreadableMesh_FallsBackToTheCornersOfItsBounds()
        {
            Mesh diamond = Diamond();
            diamond.UploadMeshData(true);
            try
            {
                MeshBounds.LowestY(diamond, Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 45f))).Should().BeApproximately(-1.4142f, Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(diamond);
            }
        }

        private static Mesh Diamond()
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, -1f, 0f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
