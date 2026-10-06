#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Presentation.Demons;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    /// <summary>The left arm is a mirrored copy of the right arm's mesh (D-088).</summary>
    public sealed class MirroredMeshesTests
    {
        private const float Tolerance = 0.0001f;

        private Mesh _source = null!;

        [SetUp]
        public void CreateTriangle()
        {
            _source = new Mesh { name = "Claw" };
            _source.SetVertices(new List<Vector3> { new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f) });
            Vector3 normal = new Vector3(1f, 1f, 1f).normalized;
            _source.SetNormals(new List<Vector3> { normal, normal, normal });
            var tangent = new Vector4(1f, 0f, 0f, 1f);
            _source.SetTangents(new List<Vector4> { tangent, tangent, tangent });
            _source.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f) });
            _source.SetTriangles(new[] { 0, 1, 2 }, 0);
        }

        [TearDown]
        public void DestroyMeshes()
        {
            MirroredMeshes.Clear();
            Object.DestroyImmediate(_source);
        }

        [Test]
        public void For_FlipsXAndKeepsTheUvs()
        {
            Mesh mirrored = MirroredMeshes.For(_source);

            mirrored.vertices[0].Should().Be(new Vector3(-1f, 0f, 0f));
            mirrored.vertices[1].Should().Be(new Vector3(0f, 1f, 0f));
            mirrored.normals[0].x.Should().BeApproximately(-_source.normals[0].x, Tolerance);
            mirrored.normals[0].y.Should().BeApproximately(_source.normals[0].y, Tolerance);
            mirrored.tangents[0].Should().Be(new Vector4(-1f, 0f, 0f, -1f), "the tangent flips and so does its handedness");
            mirrored.uv.Should().Equal(_source.uv);
            mirrored.bounds.center.x.Should().BeApproximately(-_source.bounds.center.x, Tolerance);
        }

        // Without turning the winding, a mirrored face would point into the arm and render inside out.
        [Test]
        public void For_TurnsTheWindingSoFacesStillPointOutward()
        {
            Mesh mirrored = MirroredMeshes.For(_source);

            Vector3 original = FaceNormal(_source);
            Vector3 flipped = FaceNormal(mirrored);

            flipped.x.Should().BeApproximately(-original.x, Tolerance);
            flipped.y.Should().BeApproximately(original.y, Tolerance);
            flipped.z.Should().BeApproximately(original.z, Tolerance);
        }

        [Test]
        public void For_TheSameMeshTwice_BuildsOnce()
        {
            MirroredMeshes.For(_source).Should().BeSameAs(MirroredMeshes.For(_source));
        }

        private static Vector3 FaceNormal(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            return Vector3.Cross(vertices[triangles[1]] - vertices[triangles[0]], vertices[triangles[2]] - vertices[triangles[0]]).normalized;
        }
    }
}
