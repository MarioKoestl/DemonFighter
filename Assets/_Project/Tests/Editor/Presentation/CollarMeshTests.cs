#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Presentation.Demons;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    /// <summary>The flesh collar that grows a part out of the body (D-097), on a ball of a core and a rod of a part.</summary>
    public sealed class CollarMeshTests
    {
        private const float CoreRadius = 0.4f;
        private const float RodRadius = 0.06f;
        private static readonly Vector3 CoreCenter = new Vector3(0f, 0.5f, 0f);

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
        public void Build_RodLeavingTheBall_RunsAlongTheRod()
        {
            CollarShape shape = Build(Rod(0.2f, 1f), new Vector3(CoreRadius, 0.5f, 0f));

            Vector3.Dot(shape.Axis, Vector3.right).Should().BeGreaterThan(0.9f);
            shape.TopRadius.Should().BeInRange(RodRadius, RodRadius * 1.2f);
            shape.BaseRadius.Should().BeGreaterThan(shape.TopRadius, "the collar flares where it meets the body");
        }

        [Test]
        public void Build_BaseRing_LiesOnTheCore()
        {
            CollarShape shape = Build(Rod(0.2f, 1f), new Vector3(CoreRadius, 0.5f, 0f));

            Vector3[] vertices = shape.Mesh.vertices;
            for (int i = 0; i < CollarMesh.Segments; i++)
            {
                Vector3.Distance(vertices[i], CoreCenter).Should().BeApproximately(CoreRadius, 0.03f, "base vertex " + i + " hugs the surface");
            }
        }

        // The last ring tucks under the skin of the part, so no cuff shows where the sleeve ends.
        [Test]
        public void Build_LastRing_TucksIntoThePart()
        {
            CollarShape shape = Build(Rod(0.2f, 1f), new Vector3(CoreRadius, 0.5f, 0f));

            Vector3[] vertices = shape.Mesh.vertices;
            int last = (CollarMesh.Rings - 1) * CollarMesh.Segments;
            for (int i = 0; i < CollarMesh.Segments; i++)
            {
                Vector3 vertex = vertices[last + i];
                new Vector2(vertex.y - 0.5f, vertex.z).magnitude.Should().BeLessThan(RodRadius, "lip vertex " + i + " is inside the rod");
            }
        }

        // The body holds the base and the part holds the end, so the sleeve stretches when the part swings.
        [Test]
        public void Build_Weights_RunFromTheBodyToThePart()
        {
            CollarShape shape = Build(Rod(0.2f, 1f), new Vector3(CoreRadius, 0.5f, 0f));

            BoneWeight[] weights = shape.Mesh.boneWeights;
            weights[0].weight0.Should().BeApproximately(1f, 0.001f);
            weights[((CollarMesh.Rings - 1) * CollarMesh.Segments) + 3].weight1.Should().BeApproximately(1f, 0.001f);
            shape.Mesh.bindposes.Should().HaveCount(2);
        }

        [Test]
        public void Build_Sleeve_FacesAwayFromTheRod()
        {
            CollarShape shape = Build(Rod(0.2f, 1f), new Vector3(CoreRadius, 0.5f, 0f));

            Vector3[] vertices = shape.Mesh.vertices;
            Vector3[] normals = shape.Mesh.normals;
            int middle = (CollarMesh.Rings / 2) * CollarMesh.Segments;
            for (int i = 0; i < CollarMesh.Segments; i++)
            {
                Vector3 radial = vertices[middle + i] - new Vector3(vertices[middle + i].x, 0.5f, 0f);
                Vector3.Dot(normals[middle + i], radial).Should().BeGreaterThan(0f, "middle vertex " + i + " faces out");
            }
        }

        [Test]
        public void Build_PartAwayFromTheBody_HasNoCollar()
        {
            CoreSurface core = Ball();

            CollarShape? shape = CollarMesh.Build(core, Rod(0.9f, 1.5f), new Vector3(0.9f, 0.5f, 0f), 1f, Matrix4x4.identity);

            shape.Should().BeNull();
        }

        [Test]
        public void Build_NoCollarWanted_HasNoCollar()
        {
            CollarMesh.Build(Ball(), Rod(0.2f, 1f), new Vector3(CoreRadius, 0.5f, 0f), 0f, Matrix4x4.identity).Should().BeNull();
        }

        [Test]
        public void CoreSurface_Nearest_MatchesASearchOfEveryPoint()
        {
            CoreSurface core = Ball();
            var random = new System.Random(7);
            for (int n = 0; n < 50; n++)
            {
                var point = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble(), (float)random.NextDouble() - 0.5f) * 1.6f;
                int best = 0;
                for (int i = 1; i < core.Count; i++)
                {
                    if ((core.Point(i) - point).sqrMagnitude < (core.Point(best) - point).sqrMagnitude)
                    {
                        best = i;
                    }
                }

                (core.Point(core.Nearest(point)) - point).magnitude.Should().BeApproximately((core.Point(best) - point).magnitude, 0.0001f);
            }
        }

        [Test]
        public void CoreSurface_SignedDistance_IsNegativeInsideAndPositiveOutside()
        {
            CoreSurface core = Ball();

            core.SignedDistance(CoreCenter).Should().BeNegative();
            core.SignedDistance(CoreCenter + (Vector3.up * 0.6f)).Should().BePositive();
        }

        private CollarShape Build(Vector3[] part, Vector3 pivot)
        {
            CollarShape? shape = CollarMesh.Build(Ball(), part, pivot, 1f, Matrix4x4.identity);
            shape.Should().NotBeNull();
            _meshes.Add(shape!.Mesh);
            return shape;
        }

        // Points spread evenly over a ball: the Fibonacci lattice.
        private static CoreSurface Ball()
        {
            const int count = 3000;
            var points = new Vector3[count];
            var normals = new Vector3[count];
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < count; i++)
            {
                float y = 1f - (2f * (i + 0.5f) / count);
                float ring = Mathf.Sqrt(1f - (y * y));
                var normal = new Vector3(Mathf.Cos(golden * i) * ring, y, Mathf.Sin(golden * i) * ring);
                normals[i] = normal;
                points[i] = CoreCenter + (normal * CoreRadius);
            }

            return CoreSurface.FromPoints(points, normals);
        }

        // A rod along X at the height of the ball's centre, from one end to the other.
        private static Vector3[] Rod(float from, float to)
        {
            var points = new List<Vector3>();
            for (float x = from; x <= to; x += 0.01f)
            {
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI / 8f;
                    points.Add(new Vector3(x, 0.5f + (Mathf.Cos(angle) * RodRadius), Mathf.Sin(angle) * RodRadius));
                }
            }

            return points.ToArray();
        }
    }
}
