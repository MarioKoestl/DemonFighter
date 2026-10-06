#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace DemonFighter.Presentation.Combat
{
    /// <summary>
    /// Placeholder viscera built from code (ASSET_PIPELINE, "Gore assets"): lumps of flesh and strands of gut about
    /// one unit across, kneaded by deterministic noise so every seed gives a different piece. Real viscera meshes
    /// replace them later through the pipeline; nothing else depends on their shape.
    /// </summary>
    public static class VisceraMeshes
    {
        private const float LumpRadius = 0.5f;
        private const float LumpSquash = 0.72f;
        private const float LumpRoughness = 0.5f;
        private const float StrandLength = 1f;
        private const float StrandRadius = 0.07f;
        private const float StrandBend = 0.18f;
        private static readonly Vector3[] FaceNormals =
        {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back,
        };

        /// <summary>A lump about one unit across: a cube sphere displaced by noise and squashed a little.</summary>
        public static Mesh CreateLump(int seed, int resolution = 6)
        {
            resolution = Mathf.Max(2, resolution);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var offset = new Vector3(seed * 7.31f, seed * 3.17f, seed * 11.9f);
            for (int face = 0; face < FaceNormals.Length; face++)
            {
                Vector3 normal = FaceNormals[face];
                Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
                Vector3 axisB = Vector3.Cross(normal, axisA);
                int first = vertices.Count;
                for (int y = 0; y <= resolution; y++)
                {
                    for (int x = 0; x <= resolution; x++)
                    {
                        float u = x / (float)resolution * 2f - 1f;
                        float v = y / (float)resolution * 2f - 1f;
                        Vector3 direction = (normal + axisA * u + axisB * v).normalized;
                        float bumps = Noise(direction * 1.7f + offset);
                        float radius = LumpRadius * (1f - LumpRoughness * 0.5f + LumpRoughness * bumps);
                        Vector3 point = direction * radius;
                        point.y *= LumpSquash;
                        vertices.Add(point);
                    }
                }

                AddGrid(triangles, first, resolution);
            }

            return Build("Viscera Lump " + seed, vertices, triangles);
        }

        /// <summary>A strand of gut about one unit long: a tube along a bent curve with a wobbling radius.</summary>
        public static Mesh CreateStrand(int seed, int segments = 10, int sides = 6)
        {
            segments = Mathf.Max(2, segments);
            sides = Mathf.Max(3, sides);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var offset = new Vector3(seed * 5.13f, seed * 2.71f, seed * 9.4f);
            for (int segment = 0; segment <= segments; segment++)
            {
                float t = segment / (float)segments;
                Vector3 center = CurvePoint(t, offset);
                Vector3 tangent = (CurvePoint(Mathf.Min(1f, t + 0.01f), offset) - CurvePoint(Mathf.Max(0f, t - 0.01f), offset)).normalized;
                Vector3 side = Vector3.Cross(tangent, Vector3.up);
                if (side.sqrMagnitude < 0.001f)
                {
                    side = Vector3.Cross(tangent, Vector3.right);
                }

                side.Normalize();
                Vector3 up = Vector3.Cross(side, tangent);
                float radius = StrandRadius * (0.7f + 0.6f * Noise(new Vector3(t * 6f, 0f, 0f) + offset));
                for (int ring = 0; ring < sides; ring++)
                {
                    float angle = ring / (float)sides * Mathf.PI * 2f;
                    vertices.Add(center + (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius);
                }
            }

            for (int segment = 0; segment < segments; segment++)
            {
                int ringStart = segment * sides;
                int nextRing = ringStart + sides;
                for (int ring = 0; ring < sides; ring++)
                {
                    int next = (ring + 1) % sides;
                    triangles.Add(ringStart + ring);
                    triangles.Add(nextRing + ring);
                    triangles.Add(nextRing + next);
                    triangles.Add(ringStart + ring);
                    triangles.Add(nextRing + next);
                    triangles.Add(ringStart + next);
                }
            }

            return Build("Viscera Strand " + seed, vertices, triangles);
        }

        /// <summary>Deterministic value noise in [0, 1]; the same point gives the same value on every machine.</summary>
        public static float Noise(Vector3 point)
        {
            Vector3 cell = new Vector3(Mathf.Floor(point.x), Mathf.Floor(point.y), Mathf.Floor(point.z));
            Vector3 fraction = point - cell;
            fraction = new Vector3(Smooth(fraction.x), Smooth(fraction.y), Smooth(fraction.z));
            float x00 = Mathf.Lerp(Hash(cell), Hash(cell + Vector3.right), fraction.x);
            float x10 = Mathf.Lerp(Hash(cell + Vector3.up), Hash(cell + Vector3.up + Vector3.right), fraction.x);
            float x01 = Mathf.Lerp(Hash(cell + Vector3.forward), Hash(cell + Vector3.forward + Vector3.right), fraction.x);
            float x11 = Mathf.Lerp(Hash(cell + Vector3.forward + Vector3.up), Hash(cell + Vector3.one), fraction.x);
            float y0 = Mathf.Lerp(x00, x10, fraction.y);
            float y1 = Mathf.Lerp(x01, x11, fraction.y);
            return Mathf.Lerp(y0, y1, fraction.z);
        }

        private static Vector3 CurvePoint(float t, Vector3 offset)
        {
            float wobbleX = (Noise(new Vector3(t * 3f, 1f, 0f) + offset) - 0.5f) * StrandBend;
            float wobbleY = (Noise(new Vector3(t * 3f, 5f, 0f) + offset) - 0.5f) * StrandBend;
            return new Vector3(Mathf.Sin(t * Mathf.PI) * StrandBend + wobbleX, wobbleY, (t - 0.5f) * StrandLength);
        }

        private static void AddGrid(List<int> triangles, int first, int resolution)
        {
            int stride = resolution + 1;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int corner = first + y * stride + x;
                    triangles.Add(corner);
                    triangles.Add(corner + stride);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + stride);
                    triangles.Add(corner + stride + 1);
                }
            }
        }

        private static Mesh Build(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float Hash(Vector3 cell)
        {
            float value = Mathf.Sin(Vector3.Dot(cell, new Vector3(12.9898f, 78.233f, 37.719f))) * 43758.5453f;
            return value - Mathf.Floor(value);
        }
    }
}
