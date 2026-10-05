#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.World;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    /// <summary>The round surface of a pool (D-086).</summary>
    public sealed class DiscMeshTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Build_IsOneUnitAcrossFlatAndFacesUp()
        {
            Mesh disc = DiscMesh.Build();
            try
            {
                disc.vertexCount.Should().Be(DiscMesh.Sides + 1);
                disc.bounds.size.x.Should().BeApproximately(1f, Tolerance);
                disc.bounds.size.y.Should().BeApproximately(0f, Tolerance);
                disc.bounds.size.z.Should().BeApproximately(1f, 0.01f);

                Vector3[] vertices = disc.vertices;
                int[] triangles = disc.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 face = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);
                    face.y.Should().BePositive("every triangle faces up, or the pool would be invisible from above");
                }
            }
            finally
            {
                Object.DestroyImmediate(disc);
            }
        }

        // The UVs span the disc from 0 to 1 like the cylinder cap, so the lava tiling per meter still holds.
        [Test]
        public void Build_UvsSpanTheDiscFromZeroToOne()
        {
            Mesh disc = DiscMesh.Build();
            try
            {
                Vector2[] uvs = disc.uv;
                Vector3[] vertices = disc.vertices;
                for (int i = 0; i < uvs.Length; i++)
                {
                    uvs[i].x.Should().BeApproximately(vertices[i].x + 0.5f, Tolerance);
                    uvs[i].y.Should().BeApproximately(vertices[i].z + 0.5f, Tolerance);
                }
            }
            finally
            {
                Object.DestroyImmediate(disc);
            }
        }
    }
}
