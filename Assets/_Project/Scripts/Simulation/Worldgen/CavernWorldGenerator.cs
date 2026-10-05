#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DemonFighter.Simulation.Worldgen
{
    /// <summary>
    /// The v1 generator: a gently rolling cavern floor from two octaves of value noise, a spawn cluster in the middle
    /// third, an elder loop around the center and features scattered by rejection sampling so they keep clear of each
    /// other, the spawn and the route. Pools sit in basins carved into the floor (D-086), so the ground never covers
    /// the lava that burns. All randomness comes from one generator seeded with the world seed.
    /// </summary>
    public sealed class CavernWorldGenerator : IWorldGenerator
    {
        private const int PlacementAttempts = 200;
        private const float SpawnPointSpacing = 2.5f;
        private const float SpawnFeatureClearance = 2f;
        private const float FissureHeight = 0.3f;
        private const float PoolDepth = 0.4f;
        private const float BonePileHeight = 1f;
        private const float SpawnAreaFraction = 0.4f;
        private const float BasinDepth = 0.3f;
        private const float BasinShoreCells = 1.5f;
        private const float BasinBankSlope = 0.45f;
        private const float BasinBankReach = 12f;
        private const float RimSampleSpacing = 1f;

        /// <inheritdoc />
        public WorldLayout Generate(int seed, BiomeSpec biome)
        {
            if (biome == null)
            {
                throw new ArgumentNullException(nameof(biome));
            }

            biome.Validate();
            var rng = new Rng(seed);

            Heightfield heights = GenerateHeights(rng, biome);
            GroundBounds bounds = GroundBounds.CenteredSquare(biome.SizeMeters).Shrink(biome.WallInset);
            GroundBounds featureArea = bounds.Shrink(biome.FeatureMargin);
            Vector3 spawnCenter = PickSpawnCenter(rng, biome, heights);
            List<Vector3> route = BuildElderRoute(rng, biome, featureArea, heights, spawnCenter);
            List<FeaturePlacement> features = PlaceFeatures(rng, biome, featureArea, heights, spawnCenter, route);
            heights = CarveBasins(heights, features);
            List<Vector3> spawns = PlaceSpawnPoints(rng, biome, heights, spawnCenter, features);

            return new WorldLayout(seed, biome, bounds, heights, features, spawns, route);
        }

        /// <summary>
        /// Sinks the floor under every pool (D-086): the surface of a pool lies at the lowest point of the ground it covers,
        /// the floor under it and a shore of one and a half cells around it drop below that surface, and beyond the shore
        /// a bank rises at a gentle, walkable slope until it meets the terrain. A flat pool on a slope was half buried before, and the buried half still burned.
        /// Pools move to their surface height; every other feature is set onto the carved ground.
        /// </summary>
        internal static Heightfield CarveBasins(Heightfield field, List<FeaturePlacement> features)
        {
            var heights = new float[field.VertexCountX * field.VertexCountZ];
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = field.Heights[i];
            }

            var current = new Heightfield(field.VertexCountX, field.VertexCountZ, field.CellSize, field.OriginX, field.OriginZ, heights);
            float shore = field.CellSize * BasinShoreCells;
            for (int f = 0; f < features.Count; f++)
            {
                FeaturePlacement pool = features[f];
                if (pool.Kind != FeatureKind.LavaPool && pool.Kind != FeatureKind.WaterPool)
                {
                    continue;
                }

                float radius = pool.Size.X * 0.5f;
                float level = LowestWithin(current, pool.Position, radius);
                float floor = level - BasinDepth;
                float reach = radius + shore + BasinBankReach;
                int minX = Math.Max(0, (int)MathF.Floor((pool.Position.X - reach - field.OriginX) / field.CellSize));
                int maxX = Math.Min(field.VertexCountX - 1, (int)MathF.Ceiling((pool.Position.X + reach - field.OriginX) / field.CellSize));
                int minZ = Math.Max(0, (int)MathF.Floor((pool.Position.Z - reach - field.OriginZ) / field.CellSize));
                int maxZ = Math.Min(field.VertexCountZ - 1, (int)MathF.Ceiling((pool.Position.Z + reach - field.OriginZ) / field.CellSize));
                for (int iz = minZ; iz <= maxZ; iz++)
                {
                    for (int ix = minX; ix <= maxX; ix++)
                    {
                        float dx = field.OriginX + ix * field.CellSize - pool.Position.X;
                        float dz = field.OriginZ + iz * field.CellSize - pool.Position.Z;
                        float distance = MathF.Sqrt(dx * dx + dz * dz);
                        if (distance > reach)
                        {
                            continue;
                        }

                        int index = iz * field.VertexCountX + ix;
                        // A cone around the basin: never higher than the terrain, never steeper than the bank slope.
                        float bank = MathF.Max(0f, distance - radius - shore);
                        heights[index] = MathF.Min(heights[index], floor + bank * BasinBankSlope);
                    }
                }

                features[f] = new FeaturePlacement(pool.Kind, new Vector3(pool.Position.X, level, pool.Position.Z), pool.Yaw, pool.Size);
            }

            for (int f = 0; f < features.Count; f++)
            {
                FeaturePlacement feature = features[f];
                if (feature.Kind != FeatureKind.LavaPool && feature.Kind != FeatureKind.WaterPool)
                {
                    float ground = current.SampleHeight(feature.Position.X, feature.Position.Z);
                    features[f] = new FeaturePlacement(feature.Kind, new Vector3(feature.Position.X, ground, feature.Position.Z), feature.Yaw, feature.Size);
                }
            }

            return current;
        }

        // The lowest ground under a disc: every vertex inside it and points along its rim, where the slope may dip lowest.
        private static float LowestWithin(Heightfield field, Vector3 center, float radius)
        {
            float lowest = field.SampleHeight(center.X, center.Z);
            int rimSamples = Math.Max(8, (int)MathF.Ceiling(MathF.PI * 2f * radius / RimSampleSpacing));
            for (int i = 0; i < rimSamples; i++)
            {
                float angle = i * MathF.PI * 2f / rimSamples;
                lowest = MathF.Min(lowest, field.SampleHeight(center.X + MathF.Sin(angle) * radius, center.Z + MathF.Cos(angle) * radius));
            }

            int minX = Math.Max(0, (int)MathF.Floor((center.X - radius - field.OriginX) / field.CellSize));
            int maxX = Math.Min(field.VertexCountX - 1, (int)MathF.Ceiling((center.X + radius - field.OriginX) / field.CellSize));
            int minZ = Math.Max(0, (int)MathF.Floor((center.Z - radius - field.OriginZ) / field.CellSize));
            int maxZ = Math.Min(field.VertexCountZ - 1, (int)MathF.Ceiling((center.Z + radius - field.OriginZ) / field.CellSize));
            for (int iz = minZ; iz <= maxZ; iz++)
            {
                for (int ix = minX; ix <= maxX; ix++)
                {
                    float dx = field.OriginX + ix * field.CellSize - center.X;
                    float dz = field.OriginZ + iz * field.CellSize - center.Z;
                    if (dx * dx + dz * dz <= radius * radius)
                    {
                        lowest = MathF.Min(lowest, field.HeightAt(ix, iz));
                    }
                }
            }

            return lowest;
        }

        /// <summary>Shortest distance on the ground from a point to a closed polyline.</summary>
        internal static float DistanceToLoop(Vector3 point, IReadOnlyList<Vector3> loop)
        {
            float best = float.MaxValue;
            for (int i = 0; i < loop.Count; i++)
            {
                Vector3 a = loop[i];
                Vector3 b = loop[(i + 1) % loop.Count];
                best = MathF.Min(best, DistanceToSegment(point, a, b));
            }

            return best;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var p = new Vector2(point.X, point.Z);
            var start = new Vector2(a.X, a.Z);
            var end = new Vector2(b.X, b.Z);
            Vector2 segment = end - start;
            float lengthSquared = segment.LengthSquared();
            float t = lengthSquared > 0f ? Math.Clamp(Vector2.Dot(p - start, segment) / lengthSquared, 0f, 1f) : 0f;
            return Vector2.Distance(p, start + segment * t);
        }

        private static Heightfield GenerateHeights(Rng rng, BiomeSpec biome)
        {
            int cells = Math.Max(1, (int)MathF.Round(biome.SizeMeters / biome.CellSize));
            int vertices = cells + 1;
            float origin = -cells * biome.CellSize * 0.5f;

            var coarse = new ValueNoiseLayer(rng, biome.SizeMeters, biome.HeightWavelength, biome.HeightAmplitude);
            var fine = new ValueNoiseLayer(rng, biome.SizeMeters, biome.HeightWavelength * 0.5f, biome.HeightAmplitude * 0.5f);

            var heights = new float[vertices * vertices];
            for (int iz = 0; iz < vertices; iz++)
            {
                float z = origin + iz * biome.CellSize;
                for (int ix = 0; ix < vertices; ix++)
                {
                    float x = origin + ix * biome.CellSize;
                    heights[iz * vertices + ix] = coarse.Sample(x, z) + fine.Sample(x, z);
                }
            }

            return new Heightfield(vertices, vertices, biome.CellSize, origin, origin, heights);
        }

        private static Vector3 PickSpawnCenter(Rng rng, BiomeSpec biome, Heightfield heights)
        {
            float half = biome.SizeMeters * SpawnAreaFraction * 0.5f;
            float x = rng.NextFloat(-half, half);
            float z = rng.NextFloat(-half, half);
            return new Vector3(x, heights.SampleHeight(x, z), z);
        }

        private static List<Vector3> BuildElderRoute(
            Rng rng, BiomeSpec biome, GroundBounds area, Heightfield heights, Vector3 spawnCenter)
        {
            var waypoints = new List<Vector3>(biome.ElderRouteWaypoints);
            float step = MathF.PI * 2f / biome.ElderRouteWaypoints;
            for (int i = 0; i < biome.ElderRouteWaypoints; i++)
            {
                float angle = i * step + rng.NextFloat(-step * 0.25f, step * 0.25f);
                float radius = biome.ElderRouteRadius + rng.NextFloat(-biome.ElderRouteJitter, biome.ElderRouteJitter);
                Vector3 point = area.Clamp(new Vector3(MathF.Sin(angle) * radius, 0f, MathF.Cos(angle) * radius));
                waypoints.Add(new Vector3(point.X, heights.SampleHeight(point.X, point.Z), point.Z));
            }

            int start = NearestWaypointBeyond(waypoints, spawnCenter, biome.ElderMinSpawnDistance);
            var rotated = new List<Vector3>(waypoints.Count);
            for (int i = 0; i < waypoints.Count; i++)
            {
                rotated.Add(waypoints[(start + i) % waypoints.Count]);
            }

            return rotated;
        }

        private static int NearestWaypointBeyond(List<Vector3> waypoints, Vector3 origin, float minDistance)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            int nearestAny = 0;
            float nearestAnyDistance = float.MaxValue;
            for (int i = 0; i < waypoints.Count; i++)
            {
                float distance = GroundDistance(waypoints[i], origin);
                if (distance < nearestAnyDistance)
                {
                    nearestAnyDistance = distance;
                    nearestAny = i;
                }

                if (distance >= minDistance && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best >= 0 ? best : nearestAny;
        }

        private static List<FeaturePlacement> PlaceFeatures(
            Rng rng, BiomeSpec biome, GroundBounds area, Heightfield heights, Vector3 spawnCenter, List<Vector3> route)
        {
            var features = new List<FeaturePlacement>();
            PlaceKind(rng, features, FeatureKind.Rock, rng.NextInt(biome.RockCountMin, biome.RockCountMax + 1),
                () => RockSize(rng, biome), biome, area, heights, spawnCenter, route);
            PlaceKind(rng, features, FeatureKind.Fissure, rng.NextInt(biome.FissureCountMin, biome.FissureCountMax + 1),
                () => new Vector3(biome.FissureWidth, FissureHeight, rng.NextFloat(biome.FissureLengthMin, biome.FissureLengthMax)),
                biome, area, heights, spawnCenter, route);
            PlaceKind(rng, features, FeatureKind.LavaPool, biome.LavaPoolCount,
                () => Disc(rng.NextFloat(biome.LavaPoolRadiusMin, biome.LavaPoolRadiusMax), PoolDepth),
                biome, area, heights, spawnCenter, route);
            PlaceKind(rng, features, FeatureKind.WaterPool, biome.WaterPoolCount,
                () => Disc(rng.NextFloat(biome.WaterPoolRadiusMin, biome.WaterPoolRadiusMax), PoolDepth),
                biome, area, heights, spawnCenter, route);
            PlaceKind(rng, features, FeatureKind.BonePile, rng.NextInt(biome.BonePileCountMin, biome.BonePileCountMax + 1),
                () => Disc(rng.NextFloat(biome.BonePileRadiusMin, biome.BonePileRadiusMax), BonePileHeight),
                biome, area, heights, spawnCenter, route);
            return features;
        }

        private static Vector3 RockSize(Rng rng, BiomeSpec biome)
        {
            float width = rng.NextFloat(biome.RockSizeMin, biome.RockSizeMax);
            float depth = rng.NextFloat(biome.RockSizeMin, biome.RockSizeMax);
            float height = rng.NextFloat(biome.RockSizeMin, biome.RockSizeMax);
            return new Vector3(width, height, depth);
        }

        private static Vector3 Disc(float radius, float height)
        {
            return new Vector3(radius * 2f, height, radius * 2f);
        }

        private static void PlaceKind(
            Rng rng,
            List<FeaturePlacement> features,
            FeatureKind kind,
            int count,
            Func<Vector3> sizeFactory,
            BiomeSpec biome,
            GroundBounds area,
            Heightfield heights,
            Vector3 spawnCenter,
            List<Vector3> route)
        {
            for (int n = 0; n < count; n++)
            {
                Vector3 size = sizeFactory();
                float footprint = MathF.Max(size.X, size.Z) * 0.5f;
                for (int attempt = 0; attempt < PlacementAttempts; attempt++)
                {
                    float x = rng.NextFloat(area.MinX + footprint, area.MaxX - footprint);
                    float z = rng.NextFloat(area.MinZ + footprint, area.MaxZ - footprint);
                    var candidate = new Vector3(x, heights.SampleHeight(x, z), z);
                    if (!IsClear(candidate, footprint, features, biome, spawnCenter, route))
                    {
                        continue;
                    }

                    float yaw = rng.NextFloat(0f, MathF.PI * 2f);
                    features.Add(new FeaturePlacement(kind, candidate, yaw, size));
                    break;
                }
            }
        }

        private static bool IsClear(
            Vector3 candidate, float footprint, List<FeaturePlacement> features, BiomeSpec biome, Vector3 spawnCenter, List<Vector3> route)
        {
            if (GroundDistance(candidate, spawnCenter) < biome.SpawnClearRadius + footprint)
            {
                return false;
            }

            if (DistanceToLoop(candidate, route) < biome.ElderRouteClearance + footprint)
            {
                return false;
            }

            for (int i = 0; i < features.Count; i++)
            {
                float required = features[i].FootprintRadius + footprint + biome.FeatureSpacing;
                if (GroundDistance(candidate, features[i].Position) < required)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<Vector3> PlaceSpawnPoints(
            Rng rng, BiomeSpec biome, Heightfield heights, Vector3 spawnCenter, List<FeaturePlacement> features)
        {
            var points = new List<Vector3>(biome.InitialBlobs + 1) { spawnCenter };
            for (int n = 0; n < biome.InitialBlobs; n++)
            {
                for (int attempt = 0; attempt < PlacementAttempts; attempt++)
                {
                    float angle = rng.NextFloat(0f, MathF.PI * 2f);
                    float distance = rng.NextFloat(SpawnPointSpacing, biome.SpawnClusterRadius);
                    float x = spawnCenter.X + MathF.Sin(angle) * distance;
                    float z = spawnCenter.Z + MathF.Cos(angle) * distance;
                    var candidate = new Vector3(x, heights.SampleHeight(x, z), z);
                    if (IsSpawnClear(candidate, points, features))
                    {
                        points.Add(candidate);
                        break;
                    }
                }
            }

            return points;
        }

        private static bool IsSpawnClear(Vector3 candidate, List<Vector3> points, List<FeaturePlacement> features)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (GroundDistance(candidate, points[i]) < SpawnPointSpacing)
                {
                    return false;
                }
            }

            for (int i = 0; i < features.Count; i++)
            {
                if (GroundDistance(candidate, features[i].Position) < features[i].FootprintRadius + SpawnFeatureClearance)
                {
                    return false;
                }
            }

            return true;
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>One octave of value noise: random lattice values blended with a smoothstep, so slopes stay bounded.</summary>
        private sealed class ValueNoiseLayer
        {
            private readonly float[] _lattice;
            private readonly int _latticeSize;
            private readonly float _spacing;
            private readonly float _origin;

            public ValueNoiseLayer(Rng rng, float worldSize, float wavelength, float amplitude)
            {
                _spacing = wavelength;
                _latticeSize = (int)MathF.Ceiling(worldSize / wavelength) + 3;
                _origin = -worldSize * 0.5f - wavelength;
                _lattice = new float[_latticeSize * _latticeSize];
                for (int i = 0; i < _lattice.Length; i++)
                {
                    _lattice[i] = rng.NextFloat(-amplitude, amplitude);
                }
            }

            public float Sample(float x, float z)
            {
                float fx = (x - _origin) / _spacing;
                float fz = (z - _origin) / _spacing;
                int ix = Math.Clamp((int)MathF.Floor(fx), 0, _latticeSize - 2);
                int iz = Math.Clamp((int)MathF.Floor(fz), 0, _latticeSize - 2);
                float tx = Smooth(Math.Clamp(fx - ix, 0f, 1f));
                float tz = Smooth(Math.Clamp(fz - iz, 0f, 1f));

                float v00 = _lattice[iz * _latticeSize + ix];
                float v10 = _lattice[iz * _latticeSize + ix + 1];
                float v01 = _lattice[(iz + 1) * _latticeSize + ix];
                float v11 = _lattice[(iz + 1) * _latticeSize + ix + 1];
                float bottom = v00 + (v10 - v00) * tx;
                float top = v01 + (v11 - v01) * tx;
                return bottom + (top - bottom) * tz;
            }

            private static float Smooth(float t)
            {
                return t * t * (3f - 2f * t);
            }
        }
    }
}
