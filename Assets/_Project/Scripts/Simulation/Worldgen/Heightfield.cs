#nullable enable
using System;
using System.Collections.Generic;

namespace DemonFighter.Simulation.Worldgen
{
    /// <summary>
    /// Terrain heights on a regular grid, data only. Presentation turns it into meshes and colliders; the simulation
    /// samples it to put feet, features and waypoints on the ground.
    /// </summary>
    public sealed class Heightfield
    {
        private readonly float[] _heights;

        /// <summary>Wraps a row-major height array of vertexCountX times vertexCountZ values; Z is the row.</summary>
        public Heightfield(int vertexCountX, int vertexCountZ, float cellSize, float originX, float originZ, float[] heights)
        {
            if (vertexCountX < 2 || vertexCountZ < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexCountX), "A heightfield needs at least two vertices per axis.");
            }

            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "Cell size must be positive.");
            }

            if (heights == null)
            {
                throw new ArgumentNullException(nameof(heights));
            }

            if (heights.Length != vertexCountX * vertexCountZ)
            {
                throw new ArgumentException("Height array does not match the vertex counts.", nameof(heights));
            }

            VertexCountX = vertexCountX;
            VertexCountZ = vertexCountZ;
            CellSize = cellSize;
            OriginX = originX;
            OriginZ = originZ;
            _heights = heights;
        }

        /// <summary>Vertices along X.</summary>
        public int VertexCountX { get; }

        /// <summary>Vertices along Z.</summary>
        public int VertexCountZ { get; }

        /// <summary>Distance between neighbouring vertices in meters.</summary>
        public float CellSize { get; }

        /// <summary>World X of the first vertex column.</summary>
        public float OriginX { get; }

        /// <summary>World Z of the first vertex row.</summary>
        public float OriginZ { get; }

        /// <summary>Extent along X in meters.</summary>
        public float SizeX => (VertexCountX - 1) * CellSize;

        /// <summary>Extent along Z in meters.</summary>
        public float SizeZ => (VertexCountZ - 1) * CellSize;

        /// <summary>All heights, row-major with Z as the row; the mesh builder reads this directly.</summary>
        public IReadOnlyList<float> Heights => _heights;

        /// <summary>Height of one vertex.</summary>
        public float HeightAt(int ix, int iz)
        {
            return _heights[iz * VertexCountX + ix];
        }

        /// <summary>Bilinear height at a world position; positions outside the field clamp to the edge.</summary>
        public float SampleHeight(float x, float z)
        {
            float fx = Math.Clamp((x - OriginX) / CellSize, 0f, VertexCountX - 1);
            float fz = Math.Clamp((z - OriginZ) / CellSize, 0f, VertexCountZ - 1);
            int ix = Math.Min((int)fx, VertexCountX - 2);
            int iz = Math.Min((int)fz, VertexCountZ - 2);
            float tx = fx - ix;
            float tz = fz - iz;

            float h00 = HeightAt(ix, iz);
            float h10 = HeightAt(ix + 1, iz);
            float h01 = HeightAt(ix, iz + 1);
            float h11 = HeightAt(ix + 1, iz + 1);
            float bottom = h00 + (h10 - h00) * tx;
            float top = h01 + (h11 - h01) * tx;
            return bottom + (top - bottom) * tz;
        }

        /// <summary>Steepest rise between neighbouring vertices as a tangent; 1 is 45 degrees.</summary>
        public float MaxSlopeTangent()
        {
            float steepest = 0f;
            for (int iz = 0; iz < VertexCountZ; iz++)
            {
                for (int ix = 0; ix < VertexCountX; ix++)
                {
                    float here = HeightAt(ix, iz);
                    if (ix + 1 < VertexCountX)
                    {
                        steepest = MathF.Max(steepest, MathF.Abs(HeightAt(ix + 1, iz) - here) / CellSize);
                    }

                    if (iz + 1 < VertexCountZ)
                    {
                        steepest = MathF.Max(steepest, MathF.Abs(HeightAt(ix, iz + 1) - here) / CellSize);
                    }
                }
            }

            return steepest;
        }
    }
}
