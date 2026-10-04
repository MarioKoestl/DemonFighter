#nullable enable
using System;
using System.Numerics;

namespace DemonFighter.Simulation.Worldgen
{
    /// <summary>
    /// One feature in the world: what it is, where it stands on the terrain, how it is turned and how big it is.
    /// Size is the full extent in meters: X and Z on the ground before the yaw is applied, Y the height.
    /// </summary>
    public readonly struct FeaturePlacement : IEquatable<FeaturePlacement>
    {
        public FeaturePlacement(FeatureKind kind, Vector3 position, float yaw, Vector3 size)
        {
            if (size.X <= 0f || size.Y <= 0f || size.Z <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Feature size must be positive on every axis.");
            }

            Kind = kind;
            Position = position;
            Yaw = yaw;
            Size = size;
        }

        public FeatureKind Kind { get; }

        /// <summary>Base center on the terrain surface.</summary>
        public Vector3 Position { get; }

        /// <summary>Rotation around the up axis in radians.</summary>
        public float Yaw { get; }

        /// <summary>Full extent in meters.</summary>
        public Vector3 Size { get; }

        /// <summary>Radius of the circle on the ground that contains the feature, used for spacing.</summary>
        public float FootprintRadius => MathF.Max(Size.X, Size.Z) * 0.5f;

        /// <inheritdoc />
        public bool Equals(FeaturePlacement other)
        {
            return Kind == other.Kind && Position.Equals(other.Position) && Yaw.Equals(other.Yaw) && Size.Equals(other.Size);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is FeaturePlacement other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Kind, Position, Yaw, Size);

        /// <summary>Two placements are equal when every field matches exactly; used by determinism tests.</summary>
        public static bool operator ==(FeaturePlacement left, FeaturePlacement right) => left.Equals(right);

        /// <summary>Negation of equality.</summary>
        public static bool operator !=(FeaturePlacement left, FeaturePlacement right) => !left.Equals(right);
    }
}
