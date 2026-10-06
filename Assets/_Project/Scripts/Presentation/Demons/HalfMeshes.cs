#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// One side of a mesh (D-099): the triangles whose middle lies on the positive side of a plane, with only the
    /// vertices they use. A model that holds a pair of legs becomes one leg this way; its mirror image is the other.
    /// The cut face is open, which never shows: the hip sits in the body and its collar. Built once per mesh and plane.
    /// </summary>
    public static class HalfMeshes
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        /// <summary>The half of the mesh in front of the plane through the point; the mesh itself when it cannot be read.</summary>
        public static Mesh For(Mesh source, Vector3 point, Vector3 normal)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!source.isReadable)
            {
                return source;
            }

            string key = source.GetInstanceID() + "|" + point.ToString("F5") + "|" + normal.ToString("F5");
            if (Cache.TryGetValue(key, out Mesh? cached) && cached != null)
            {
                return cached;
            }

            Mesh half = Cut(source, point, normal.normalized);
            Cache[key] = half;
            return half;
        }

        private static Mesh Cut(Mesh source, Vector3 point, Vector3 normal)
        {
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            Vector2[] uvs = source.uv;
            var remap = new int[vertices.Length];
            for (int i = 0; i < remap.Length; i++)
            {
                remap[i] = -1;
            }

            var keptVertices = new List<Vector3>();
            var keptNormals = new List<Vector3>();
            var keptTangents = new List<Vector4>();
            var keptUvs = new List<Vector2>();
            var submeshes = new List<List<int>>();
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                int[] triangles = source.GetTriangles(sub);
                var kept = new List<int>(triangles.Length / 2);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 middle = (vertices[triangles[t]] + vertices[triangles[t + 1]] + vertices[triangles[t + 2]]) / 3f;
                    if (Vector3.Dot(middle - point, normal) < 0f)
                    {
                        continue;
                    }

                    for (int corner = 0; corner < 3; corner++)
                    {
                        int index = triangles[t + corner];
                        if (remap[index] < 0)
                        {
                            remap[index] = keptVertices.Count;
                            keptVertices.Add(vertices[index]);
                            if (normals.Length == vertices.Length)
                            {
                                keptNormals.Add(normals[index]);
                            }

                            if (tangents.Length == vertices.Length)
                            {
                                keptTangents.Add(tangents[index]);
                            }

                            if (uvs.Length == vertices.Length)
                            {
                                keptUvs.Add(uvs[index]);
                            }
                        }

                        kept.Add(remap[index]);
                    }
                }

                submeshes.Add(kept);
            }

            var half = new Mesh { name = source.name + " (half)" };
            if (keptVertices.Count > 65535)
            {
                half.indexFormat = IndexFormat.UInt32;
            }

            half.SetVertices(keptVertices);
            if (keptNormals.Count == keptVertices.Count)
            {
                half.SetNormals(keptNormals);
            }

            if (keptTangents.Count == keptVertices.Count)
            {
                half.SetTangents(keptTangents);
            }

            if (keptUvs.Count == keptVertices.Count)
            {
                half.SetUVs(0, keptUvs);
            }

            half.subMeshCount = submeshes.Count;
            for (int sub = 0; sub < submeshes.Count; sub++)
            {
                half.SetTriangles(submeshes[sub], sub);
            }

            half.RecalculateBounds();
            return half;
        }
    }
}
