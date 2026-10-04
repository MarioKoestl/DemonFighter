#nullable enable
using System;
using System.Numerics;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// The walkable rectangle of a world on the ground plane, inside the cavern walls (D-015). AI targets and spawn
    /// points are clamped into it so nobody plans a path into a wall.
    /// </summary>
    public readonly struct GroundBounds
    {
        public GroundBounds(float minX, float minZ, float maxX, float maxZ)
        {
            if (maxX <= minX || maxZ <= minZ)
            {
                throw new ArgumentException("Bounds need a positive extent on both axes.");
            }

            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public float MinX { get; }

        public float MinZ { get; }

        public float MaxX { get; }

        public float MaxZ { get; }

        /// <summary>Center of the rectangle at ground height zero.</summary>
        public Vector3 Center => new Vector3((MinX + MaxX) * 0.5f, 0f, (MinZ + MaxZ) * 0.5f);

        /// <summary>A square of the given side length centered on the origin.</summary>
        public static GroundBounds CenteredSquare(float sideLength)
        {
            float half = sideLength * 0.5f;
            return new GroundBounds(-half, -half, half, half);
        }

        /// <summary>True when the point lies inside or on the edge, ignoring height.</summary>
        public bool Contains(Vector3 point)
        {
            return point.X >= MinX && point.X <= MaxX && point.Z >= MinZ && point.Z <= MaxZ;
        }

        /// <summary>Moves the point onto the nearest edge when it lies outside, keeping its height.</summary>
        public Vector3 Clamp(Vector3 point)
        {
            return new Vector3(
                Math.Clamp(point.X, MinX, MaxX),
                point.Y,
                Math.Clamp(point.Z, MinZ, MaxZ));
        }

        /// <summary>The same bounds pulled in by a margin on every side; the margin must leave room.</summary>
        public GroundBounds Shrink(float margin)
        {
            return new GroundBounds(MinX + margin, MinZ + margin, MaxX - margin, MaxZ - margin);
        }
    }
}
