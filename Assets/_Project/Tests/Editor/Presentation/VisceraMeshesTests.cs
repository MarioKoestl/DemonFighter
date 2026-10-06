#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Presentation.Combat;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class VisceraMeshesTests
    {
        private readonly List<Mesh> _meshes = new List<Mesh>();

        [TearDown]
        public void DestroyMeshes()
        {
            foreach (Mesh mesh in _meshes)
            {
                Object.DestroyImmediate(mesh);
            }

            _meshes.Clear();
        }

        [Test]
        public void CreateLump_IsAClosedCubeSphereAboutOneUnitAcross()
        {
            Mesh lump = Track(VisceraMeshes.CreateLump(1, 6));

            lump.vertexCount.Should().Be(6 * 7 * 7);
            lump.triangles.Length.Should().Be(6 * 6 * 6 * 6);
            lump.bounds.size.x.Should().BeInRange(0.6f, 1.1f);
            lump.bounds.size.z.Should().BeInRange(0.6f, 1.1f);
            lump.bounds.size.y.Should().BeLessThan(lump.bounds.size.x);
            lump.normals.Length.Should().Be(lump.vertexCount);
        }

        [Test]
        public void CreateLump_DifferentSeeds_GiveDifferentShapes()
        {
            Mesh first = Track(VisceraMeshes.CreateLump(1));
            Mesh second = Track(VisceraMeshes.CreateLump(2));

            first.vertices.Should().NotEqual(second.vertices);
        }

        [Test]
        public void CreateLump_SameSeed_IsDeterministic()
        {
            Mesh first = Track(VisceraMeshes.CreateLump(5));
            Mesh second = Track(VisceraMeshes.CreateLump(5));

            first.vertices.Should().Equal(second.vertices);
        }

        [Test]
        public void CreateStrand_IsATubeAboutOneUnitLong()
        {
            Mesh strand = Track(VisceraMeshes.CreateStrand(1, 10, 6));

            strand.vertexCount.Should().Be(11 * 6);
            strand.triangles.Length.Should().Be(10 * 6 * 6);
            strand.bounds.size.z.Should().BeInRange(0.9f, 1.15f);
            strand.bounds.size.x.Should().BeLessThan(0.5f);
        }

        [Test]
        public void CreateLump_HasNoBrokenVertices()
        {
            Mesh lump = Track(VisceraMeshes.CreateLump(3));

            foreach (Vector3 vertex in lump.vertices)
            {
                float.IsNaN(vertex.x + vertex.y + vertex.z).Should().BeFalse();
            }
        }

        [Test]
        public void Noise_StaysInUnitRangeAndRepeats()
        {
            var point = new Vector3(1.37f, -2.1f, 0.42f);

            float first = VisceraMeshes.Noise(point);
            float second = VisceraMeshes.Noise(point);

            first.Should().BeInRange(0f, 1f);
            second.Should().Be(first);
        }

        private Mesh Track(Mesh mesh)
        {
            _meshes.Add(mesh);
            return mesh;
        }
    }
}
