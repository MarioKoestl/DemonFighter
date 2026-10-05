#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.World
{
    /// <summary>
    /// The flat, round surface of a pool (D-086): one unit across, facing up, with UVs spanning the disc from 0 to 1
    /// like the cap of Unity's cylinder. Sixty-four sides keep the edge round where the ground meets it; the
    /// twenty-sided cylinder showed its corners. Built once and shared.
    /// </summary>
    public static class DiscMesh
    {
        /// <summary>Corners along the rim.</summary>
        public const int Sides = 64;

        private static Mesh? _shared;

        /// <summary>The shared disc; scale it to the pool's diameter.</summary>
        public static Mesh Shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = Build();
                }

                return _shared;
            }
        }

        internal static Mesh Build()
        {
            var vertices = new Vector3[Sides + 1];
            var normals = new Vector3[Sides + 1];
            var uvs = new Vector2[Sides + 1];
            var triangles = new int[Sides * 3];
            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < Sides; i++)
            {
                float angle = i * Mathf.PI * 2f / Sides;
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;
                vertices[i + 1] = new Vector3(x, 0f, z);
                normals[i + 1] = Vector3.up;
                uvs[i + 1] = new Vector2(x + 0.5f, z + 0.5f);

                // Clockwise seen from above, so the face points up.
                triangles[(i * 3) + 0] = 0;
                triangles[(i * 3) + 1] = ((i + 1) % Sides) + 1;
                triangles[(i * 3) + 2] = i + 1;
            }

            var mesh = new Mesh { name = "Pool Disc", vertices = vertices, normals = normals, uv = uvs, triangles = triangles };
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
