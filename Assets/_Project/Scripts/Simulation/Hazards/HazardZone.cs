#nullable enable
using System;
using System.Numerics;

namespace DemonFighter.Simulation.Hazards
{
    /// <summary>
    /// The footprint of one hazard on the ground plane (X and Z of the world as X and Y here): a disc for a lava pool,
    /// a turned rectangle for a fissure. Distances are signed, negative inside, so one number answers "am I in it" and
    /// "how far is the edge".
    /// </summary>
    public readonly struct HazardZone
    {
        private const float GradientStep = 0.05f;

        private HazardZone(HazardKind kind, Vector2 center, float yaw, float halfWidth, float halfLength, bool round)
        {
            Kind = kind;
            Center = center;
            Yaw = yaw;
            HalfWidth = halfWidth;
            HalfLength = halfLength;
            IsRound = round;
        }

        public HazardKind Kind { get; }

        /// <summary>Center on the ground plane.</summary>
        public Vector2 Center { get; }

        /// <summary>Turn of a rectangle in radians, the same convention as a demon's yaw; unused for discs.</summary>
        public float Yaw { get; }

        /// <summary>Half the extent across (local X); the radius of a disc.</summary>
        public float HalfWidth { get; }

        /// <summary>Half the extent along (local Z); the radius of a disc.</summary>
        public float HalfLength { get; }

        public bool IsRound { get; }

        /// <summary>The distance the zone reaches from its center at most.</summary>
        public float Reach => IsRound ? HalfWidth : MathF.Sqrt(HalfWidth * HalfWidth + HalfLength * HalfLength);

        public static HazardZone Disc(HazardKind kind, Vector2 center, float radius)
        {
            if (radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "A hazard needs a positive size.");
            }

            return new HazardZone(kind, center, 0f, radius, radius, true);
        }

        public static HazardZone Rectangle(HazardKind kind, Vector2 center, float yaw, float width, float length)
        {
            if (width <= 0f || length <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "A hazard needs a positive size.");
            }

            return new HazardZone(kind, center, yaw, width * 0.5f, length * 0.5f, false);
        }

        /// <summary>Distance from the point to the edge: negative inside, zero on the edge, positive outside.</summary>
        public float SignedDistance(Vector2 point)
        {
            Vector2 offset = point - Center;
            if (IsRound)
            {
                return offset.Length() - HalfWidth;
            }

            // Into the rectangle's own frame: its local +Z points along the yaw, as a turned body's forward does.
            float sin = MathF.Sin(Yaw);
            float cos = MathF.Cos(Yaw);
            float localX = offset.X * cos - offset.Y * sin;
            float localZ = offset.X * sin + offset.Y * cos;
            float qx = MathF.Abs(localX) - HalfWidth;
            float qz = MathF.Abs(localZ) - HalfLength;
            float outside = new Vector2(MathF.Max(qx, 0f), MathF.Max(qz, 0f)).Length();
            float inside = MathF.Min(MathF.Max(qx, qz), 0f);
            return outside + inside;
        }

        /// <summary>The direction out of the zone at the point, the way the edge distance grows fastest.</summary>
        public Vector2 Outward(Vector2 point)
        {
            float dx = SignedDistance(point + new Vector2(GradientStep, 0f)) - SignedDistance(point - new Vector2(GradientStep, 0f));
            float dy = SignedDistance(point + new Vector2(0f, GradientStep)) - SignedDistance(point - new Vector2(0f, GradientStep));
            var gradient = new Vector2(dx, dy);
            if (gradient.LengthSquared() > 1e-8f)
            {
                return Vector2.Normalize(gradient);
            }

            Vector2 fromCenter = point - Center;
            return fromCenter.LengthSquared() > 1e-8f ? Vector2.Normalize(fromCenter) : Vector2.UnitX;
        }
    }
}
