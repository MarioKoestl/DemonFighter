#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Mirror images of part meshes across their own X axis (D-088): the second arm on the left flank is a left arm,
    /// not the right one turned around. Built once per mesh from its data and kept for the session; body part models
    /// import readable for this. A mesh that cannot be read stays as it is, with one warning.
    /// </summary>
    public static class MirroredMeshes
    {
        private const int UvChannels = 8;
        private static readonly Dictionary<Mesh, Mesh> Cache = new Dictionary<Mesh, Mesh>();
        private static readonly HashSet<Mesh> Unreadable = new HashSet<Mesh>();

        /// <summary>The mirror image of a mesh, built on first use.</summary>
        public static Mesh For(Mesh source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // A cached copy can be gone when play mode ends without a domain reload; Unity's null check sees that.
            if (Cache.TryGetValue(source, out Mesh? cached) && cached != null)
            {
                return cached;
            }

            if (!source.isReadable)
            {
                if (Unreadable.Add(source))
                {
                    Log.Warn(LogCategory.Content, source.name + " is not readable, so the copy on the left flank is not mirrored; its model must import with Read/Write on (the import rules do this under BodyParts).");
                }

                return source;
            }

            Mesh mirrored = Build(source);
            Cache[source] = mirrored;
            return mirrored;
        }

        /// <summary>Destroys the cached copies; for tests.</summary>
        internal static void Clear()
        {
            foreach (Mesh mesh in Cache.Values)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            Cache.Clear();
            Unreadable.Clear();
        }

        // X flips for positions, normals and tangents; a tangent's handedness (w) flips with it, so normal maps still
        // read right. Every triangle turns its winding around, or the mirrored faces would point inward.
        internal static Mesh Build(Mesh source)
        {
            var mirrored = new Mesh { name = source.name + " Mirrored", indexFormat = source.indexFormat };

            var vertices = new List<Vector3>();
            source.GetVertices(vertices);
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 v = vertices[i];
                vertices[i] = new Vector3(-v.x, v.y, v.z);
            }

            mirrored.SetVertices(vertices);

            if (source.HasVertexAttribute(VertexAttribute.Normal))
            {
                var normals = new List<Vector3>();
                source.GetNormals(normals);
                for (int i = 0; i < normals.Count; i++)
                {
                    Vector3 n = normals[i];
                    normals[i] = new Vector3(-n.x, n.y, n.z);
                }

                mirrored.SetNormals(normals);
            }

            if (source.HasVertexAttribute(VertexAttribute.Tangent))
            {
                var tangents = new List<Vector4>();
                source.GetTangents(tangents);
                for (int i = 0; i < tangents.Count; i++)
                {
                    Vector4 t = tangents[i];
                    tangents[i] = new Vector4(-t.x, t.y, t.z, -t.w);
                }

                mirrored.SetTangents(tangents);
            }

            if (source.HasVertexAttribute(VertexAttribute.Color))
            {
                var colors = new List<Color32>();
                source.GetColors(colors);
                mirrored.SetColors(colors);
            }

            // UVs keep their dimension, so the mirrored mesh has the same vertex layout as the source.
            var uv2 = new List<Vector2>();
            var uv3 = new List<Vector3>();
            var uv4 = new List<Vector4>();
            for (int channel = 0; channel < UvChannels; channel++)
            {
                VertexAttribute attribute = VertexAttribute.TexCoord0 + channel;
                if (!source.HasVertexAttribute(attribute))
                {
                    continue;
                }

                switch (source.GetVertexAttributeDimension(attribute))
                {
                    case 2:
                        source.GetUVs(channel, uv2);
                        mirrored.SetUVs(channel, uv2);
                        break;
                    case 3:
                        source.GetUVs(channel, uv3);
                        mirrored.SetUVs(channel, uv3);
                        break;
                    default:
                        source.GetUVs(channel, uv4);
                        mirrored.SetUVs(channel, uv4);
                        break;
                }
            }

            mirrored.subMeshCount = source.subMeshCount;
            var indices = new List<int>();
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                MeshTopology topology = source.GetTopology(sub);
                source.GetIndices(indices, sub);
                if (topology == MeshTopology.Triangles)
                {
                    for (int i = 0; i + 2 < indices.Count; i += 3)
                    {
                        (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                    }
                }

                mirrored.SetIndices(indices, topology, sub, false);
            }

            mirrored.RecalculateBounds();
            return mirrored;
        }
    }
}
