#nullable enable
using System;
using DemonFighter.Simulation.Worldgen;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonFighter.Presentation.World
{
    /// <summary>
    /// Turns a rectangle of heightfield cells into one mesh in world space. Normals come from the whole heightfield,
    /// not from the chunk, so chunk borders show no seam.
    /// </summary>
    internal static class TerrainMeshBuilder
    {
        private const float UvPerMeter = 0.1f;

        /// <summary>Builds the mesh for cells [startX, startX + cellsX) by [startZ, startZ + cellsZ).</summary>
        public static Mesh BuildChunk(Heightfield field, int startX, int startZ, int cellsX, int cellsZ)
        {
            int verticesX = cellsX + 1;
            int verticesZ = cellsZ + 1;
            var vertices = new Vector3[verticesX * verticesZ];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];

            for (int iz = 0; iz < verticesZ; iz++)
            {
                for (int ix = 0; ix < verticesX; ix++)
                {
                    int gx = startX + ix;
                    int gz = startZ + iz;
                    float x = field.OriginX + gx * field.CellSize;
                    float z = field.OriginZ + gz * field.CellSize;
                    int index = iz * verticesX + ix;
                    vertices[index] = new Vector3(x, field.HeightAt(gx, gz), z);
                    normals[index] = NormalAt(field, gx, gz);
                    uvs[index] = new Vector2(x, z) * UvPerMeter;
                }
            }

            var triangles = new int[cellsX * cellsZ * 6];
            int t = 0;
            for (int iz = 0; iz < cellsZ; iz++)
            {
                for (int ix = 0; ix < cellsX; ix++)
                {
                    int a = iz * verticesX + ix;
                    int b = a + 1;
                    int c = a + verticesX;
                    int d = c + 1;
                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = d;
                }
            }

            var mesh = new Mesh { name = "Terrain chunk" };
            if (vertices.Length > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 NormalAt(Heightfield field, int ix, int iz)
        {
            int x0 = Math.Max(ix - 1, 0);
            int x1 = Math.Min(ix + 1, field.VertexCountX - 1);
            int z0 = Math.Max(iz - 1, 0);
            int z1 = Math.Min(iz + 1, field.VertexCountZ - 1);
            float slopeX = (field.HeightAt(x1, iz) - field.HeightAt(x0, iz)) / ((x1 - x0) * field.CellSize);
            float slopeZ = (field.HeightAt(ix, z1) - field.HeightAt(ix, z0)) / ((z1 - z0) * field.CellSize);
            return new Vector3(-slopeX, 1f, -slopeZ).normalized;
        }
    }
}
