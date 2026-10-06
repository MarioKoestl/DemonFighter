#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// The surface of a core in rig space (body units): its vertices and normals with a uniform grid for nearest-point
    /// queries. Collars hug it where a part leaves the body (D-097). Built once per core mesh and pose and shared by
    /// every body that wears it, since body units do not depend on the size.
    /// </summary>
    public sealed class CoreSurface
    {
        private const float CellSize = 0.04f;
        private const int MaxRings = 40;

        private readonly Vector3[] _points;
        private readonly Vector3[] _normals;
        private readonly Dictionary<Vector3Int, List<int>> _cells = new Dictionary<Vector3Int, List<int>>();

        private CoreSurface(Vector3[] points, Vector3[] normals)
        {
            _points = points;
            _normals = normals;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3Int cell = CellOf(points[i]);
                if (!_cells.TryGetValue(cell, out List<int>? members))
                {
                    members = new List<int>();
                    _cells.Add(cell, members);
                }

                members.Add(i);
            }
        }

        /// <summary>How many surface points there are.</summary>
        public int Count => _points.Length;

        /// <summary>The surface of a mesh placed in rig space by the matrix; null when the mesh cannot be read.</summary>
        public static CoreSurface? From(Mesh mesh, Matrix4x4 meshToRig)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            if (!mesh.isReadable)
            {
                return null;
            }

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            if (vertices.Length == 0)
            {
                return null;
            }

            var points = new Vector3[vertices.Length];
            var outward = new Vector3[vertices.Length];
            Matrix4x4 normalMatrix = meshToRig.inverse.transpose;
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = meshToRig.MultiplyPoint3x4(vertices[i]);
                outward[i] = i < normals.Length ? normalMatrix.MultiplyVector(normals[i]).normalized : Vector3.up;
            }

            return new CoreSurface(points, outward);
        }

        /// <summary>A surface from points and outward normals already in rig space; for tests and generated cores.</summary>
        public static CoreSurface FromPoints(Vector3[] points, Vector3[] normals)
        {
            if (points == null || normals == null || points.Length == 0 || points.Length != normals.Length)
            {
                throw new ArgumentException("A surface needs as many normals as points, and at least one point.");
            }

            return new CoreSurface((Vector3[])points.Clone(), (Vector3[])normals.Clone());
        }

        public Vector3 Point(int index) => _points[index];

        public Vector3 Normal(int index) => _normals[index];

        /// <summary>The index of the surface point nearest to a point in rig space.</summary>
        public int Nearest(Vector3 point)
        {
            Vector3Int center = CellOf(point);
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int ring = 0; ring <= MaxRings; ring++)
            {
                // Anything in a farther ring is at least this far away; stop once the best beats it.
                float ringDistance = (ring - 1) * CellSize;
                if (best >= 0 && ringDistance * ringDistance > bestDistance)
                {
                    break;
                }

                for (int x = -ring; x <= ring; x++)
                {
                    for (int y = -ring; y <= ring; y++)
                    {
                        for (int z = -ring; z <= ring; z++)
                        {
                            if (Mathf.Max(Mathf.Abs(x), Mathf.Max(Mathf.Abs(y), Mathf.Abs(z))) != ring)
                            {
                                continue;
                            }

                            if (!_cells.TryGetValue(center + new Vector3Int(x, y, z), out List<int>? members))
                            {
                                continue;
                            }

                            for (int m = 0; m < members.Count; m++)
                            {
                                float distance = (_points[members[m]] - point).sqrMagnitude;
                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    best = members[m];
                                }
                            }
                        }
                    }
                }
            }

            return best >= 0 ? best : NearestBruteForce(point);
        }

        /// <summary>How far a point lies outside the surface, measured along the normal of the nearest surface point; negative inside.</summary>
        public float SignedDistance(Vector3 point)
        {
            int nearest = Nearest(point);
            return Vector3.Dot(point - _points[nearest], _normals[nearest]);
        }

        private static Vector3Int CellOf(Vector3 point)
        {
            return new Vector3Int(Mathf.FloorToInt(point.x / CellSize), Mathf.FloorToInt(point.y / CellSize), Mathf.FloorToInt(point.z / CellSize));
        }

        // Far from every cell searched, as for a point well outside a small core.
        private int NearestBruteForce(Vector3 point)
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _points.Length; i++)
            {
                float distance = (_points[i] - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }
    }
}
