#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Worldgen;

namespace DemonFighter.Simulation.Hazards
{
    /// <summary>
    /// Where the world burns (D-086): every lava pool and glowing fissure of a layout as a zone on the ground plane.
    /// Answers which hazard a point stands in, steers a walking direction around hazards for the AI, and pushes a
    /// goal point out of them. Built once when a run gets its world; plain data, no randomness.
    /// </summary>
    public sealed class HazardMap
    {
        private const int PushIterations = 3;
        private const float AwayShare = 0.35f;

        private readonly HazardZone[] _zones;

        public HazardMap(IReadOnlyList<HazardZone> zones)
        {
            if (zones == null)
            {
                throw new ArgumentNullException(nameof(zones));
            }

            _zones = new HazardZone[zones.Count];
            for (int i = 0; i < _zones.Length; i++)
            {
                _zones[i] = zones[i];
            }
        }

        /// <summary>A map without hazards, for runs without a world.</summary>
        public static HazardMap Empty { get; } = new HazardMap(Array.Empty<HazardZone>());

        public IReadOnlyList<HazardZone> Zones => _zones;

        /// <summary>The lava pools and fissures of a layout; rocks, water and bones do not burn.</summary>
        public static HazardMap From(WorldLayout world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var zones = new List<HazardZone>();
            for (int i = 0; i < world.Features.Count; i++)
            {
                FeaturePlacement feature = world.Features[i];
                var center = new Vector2(feature.Position.X, feature.Position.Z);
                if (feature.Kind == FeatureKind.LavaPool)
                {
                    zones.Add(HazardZone.Disc(HazardKind.Lava, center, feature.Size.X * 0.5f));
                }
                else if (feature.Kind == FeatureKind.Fissure)
                {
                    zones.Add(HazardZone.Rectangle(HazardKind.Fissure, center, feature.Yaw, feature.Size.X, feature.Size.Z));
                }
            }

            return new HazardMap(zones);
        }

        /// <summary>The hazard the point stands in: lava over a fissure where they overlap, None outside all.</summary>
        public HazardKind KindAt(Vector3 position)
        {
            Vector2 point = Planar(position);
            HazardKind worst = HazardKind.None;
            for (int i = 0; i < _zones.Length; i++)
            {
                if (_zones[i].Kind > worst && _zones[i].SignedDistance(point) < 0f)
                {
                    worst = _zones[i].Kind;
                }
            }

            return worst;
        }

        /// <summary>
        /// Bends a walking direction around hazards. Inside one, the way out wins. Otherwise, when the point a lookahead
        /// ahead would come closer to a hazard than the margin while heading toward it, the direction slides along the
        /// edge, a little away from it, on the side that keeps most of the wanted heading. A zero direction stays zero.
        /// </summary>
        public Vector2 Steer(Vector3 position, Vector2 direction, float lookahead, float margin)
        {
            if (direction.LengthSquared() <= 0f || _zones.Length == 0)
            {
                return direction;
            }

            Vector2 point = Planar(position);
            Vector2 wanted = Vector2.Normalize(direction);
            int inside = Deepest(point);
            if (inside >= 0)
            {
                return _zones[inside].Outward(point);
            }

            int blocking = -1;
            float closest = float.MaxValue;
            for (int i = 0; i < _zones.Length; i++)
            {
                HazardZone zone = _zones[i];
                float here = zone.SignedDistance(point);
                if (here > lookahead + margin + zone.Reach)
                {
                    continue;
                }

                float ahead = MathF.Min(zone.SignedDistance(point + wanted * lookahead), zone.SignedDistance(point + wanted * (lookahead * 0.5f)));
                bool approaching = Vector2.Dot(wanted, zone.Outward(point)) < 0f;
                if (ahead < margin && approaching && ahead < closest)
                {
                    blocking = i;
                    closest = ahead;
                }
            }

            if (blocking < 0)
            {
                return wanted;
            }

            Vector2 away = _zones[blocking].Outward(point);
            var left = new Vector2(-away.Y, away.X);
            Vector2 along = Vector2.Dot(left, wanted) >= 0f ? left : -left;
            return Vector2.Normalize(along + away * AwayShare);
        }

        /// <summary>Moves a goal point out of every hazard to at least the margin from its edge; a point outside stays.</summary>
        public Vector3 PushOut(Vector3 position, float margin)
        {
            Vector2 point = Planar(position);
            for (int iteration = 0; iteration < PushIterations; iteration++)
            {
                bool moved = false;
                for (int i = 0; i < _zones.Length; i++)
                {
                    float distance = _zones[i].SignedDistance(point);
                    if (distance < margin)
                    {
                        point += _zones[i].Outward(point) * (margin - distance);
                        moved = true;
                    }
                }

                if (!moved)
                {
                    break;
                }
            }

            return new Vector3(point.X, position.Y, point.Y);
        }

        private int Deepest(Vector2 point)
        {
            int deepest = -1;
            float depth = 0f;
            for (int i = 0; i < _zones.Length; i++)
            {
                float distance = _zones[i].SignedDistance(point);
                if (distance < depth)
                {
                    deepest = i;
                    depth = distance;
                }
            }

            return deepest;
        }

        private static Vector2 Planar(Vector3 position)
        {
            return new Vector2(position.X, position.Z);
        }
    }
}
