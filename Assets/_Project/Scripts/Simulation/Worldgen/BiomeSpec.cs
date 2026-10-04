#nullable enable
using System;
using DemonFighter.Simulation.Ai;

namespace DemonFighter.Simulation.Worldgen
{
    /// <summary>
    /// Everything the generator needs to build one kind of world, plus what spawns in it (GAME_DESIGN, "World
    /// generation" and D-015). The defaults describe the v1 ash cavern; the BiomeDefinition asset overrides them.
    /// Immutable content; validate once after loading.
    /// </summary>
    public sealed record BiomeSpec
    {
        /// <summary>The v1 cavern with every default.</summary>
        public static BiomeSpec AshCavern { get; } = new BiomeSpec();

        /// <summary>Stable content id, lowercase and dotted (CODING_GUIDELINES, "ScriptableObjects").</summary>
        public string Id { get; init; } = "biome.ash.cavern";

        public string Name { get; init; } = "Ash Cavern";

        /// <summary>Side length of the square world in meters, walls included.</summary>
        public float SizeMeters { get; init; } = 300f;

        /// <summary>Heightfield resolution in meters.</summary>
        public float CellSize { get; init; } = 2f;

        /// <summary>Largest rise or dip of the ground in meters; small, nothing needs jumping (D-017).</summary>
        public float HeightAmplitude { get; init; } = 3f;

        /// <summary>Distance between hills in meters; long, so slopes stay gentle.</summary>
        public float HeightWavelength { get; init; } = 60f;

        /// <summary>How far the walkable area stays inside the outer edge, which is where the walls stand.</summary>
        public float WallInset { get; init; } = 4f;

        /// <summary>How far features stay away from the walls.</summary>
        public float FeatureMargin { get; init; } = 10f;

        /// <summary>Gap between feature footprints, so demons fit between them.</summary>
        public float FeatureSpacing { get; init; } = 4f;

        public int RockCountMin { get; init; } = 40;

        public int RockCountMax { get; init; } = 80;

        public float RockSizeMin { get; init; } = 1.5f;

        public float RockSizeMax { get; init; } = 6f;

        public int FissureCountMin { get; init; } = 5;

        public int FissureCountMax { get; init; } = 10;

        public float FissureLengthMin { get; init; } = 8f;

        public float FissureLengthMax { get; init; } = 20f;

        public float FissureWidth { get; init; } = 1.5f;

        public int LavaPoolCount { get; init; } = 2;

        public float LavaPoolRadiusMin { get; init; } = 6f;

        public float LavaPoolRadiusMax { get; init; } = 12f;

        public int WaterPoolCount { get; init; } = 3;

        public float WaterPoolRadiusMin { get; init; } = 5f;

        public float WaterPoolRadiusMax { get; init; } = 9f;

        public int BonePileCountMin { get; init; } = 8;

        public int BonePileCountMax { get; init; } = 14;

        public float BonePileRadiusMin { get; init; } = 1.5f;

        public float BonePileRadiusMax { get; init; } = 3f;

        /// <summary>Tier 0 demons spawned around the player at run start (D-029).</summary>
        public int InitialBlobs { get; init; } = 10;

        /// <summary>Radius of the circle the start spawns are scattered in.</summary>
        public float SpawnClusterRadius { get; init; } = 15f;

        /// <summary>No feature inside this distance of the spawn center, so the first minute is open ground.</summary>
        public float SpawnClearRadius { get; init; } = 30f;

        /// <summary>Radius of the elder loop around the map center.</summary>
        public float ElderRouteRadius { get; init; } = 100f;

        /// <summary>Waypoints on the loop.</summary>
        public int ElderRouteWaypoints { get; init; } = 12;

        /// <summary>Random displacement of each waypoint, so the loop is not a perfect circle.</summary>
        public float ElderRouteJitter { get; init; } = 12f;

        /// <summary>Features keep this distance from the route, so the elder walks through nothing.</summary>
        public float ElderRouteClearance { get; init; } = 12f;

        /// <summary>The elder starts at the waypoint nearest to the spawn cluster but at least this far away.</summary>
        public float ElderMinSpawnDistance { get; init; } = 40f;

        public DemonTemplate BlobTemplate { get; init; } = new DemonTemplate("Blob", 0, 1.2f, 4f, 1.6f);

        public DemonTemplate ElderTemplate { get; init; } = new DemonTemplate("Elder", 6, 15f, 3f, 1f);

        public ArchetypeSpec BlobArchetype { get; init; } = new ArchetypeSpec(
            "Blob", wanderWeight: 1f, restWeight: 0.6f, patrolWeight: 0f, wanderRadius: 20f,
            restSecondsMin: 2f, restSecondsMax: 5f, decisionIntervalTicks: 10, arriveDistance: 1f);

        public ArchetypeSpec ElderArchetype { get; init; } = new ArchetypeSpec(
            "Elder", wanderWeight: 0f, restWeight: 0.15f, patrolWeight: 1f, wanderRadius: 30f,
            restSecondsMin: 3f, restSecondsMax: 6f, decisionIntervalTicks: 10, arriveDistance: 4f);

        /// <summary>Throws with the first content error found; called by the generator and the asset's OnValidate.</summary>
        public void Validate()
        {
            Require(!string.IsNullOrWhiteSpace(Id), "Id is required.");
            Require(SizeMeters > 0f, "SizeMeters must be positive.");
            Require(CellSize > 0f && CellSize < SizeMeters, "CellSize must be positive and smaller than the world.");
            Require(HeightAmplitude >= 0f, "HeightAmplitude is never negative.");
            Require(HeightWavelength >= CellSize * 2f, "HeightWavelength must span at least two cells.");
            Require(WallInset >= 0f && FeatureMargin >= 0f && FeatureSpacing >= 0f, "Margins are never negative.");
            Require(WallInset + FeatureMargin < SizeMeters * 0.5f, "Margins leave no room for features.");
            RequireRange(RockCountMin, RockCountMax, "Rock count");
            RequireRange(FissureCountMin, FissureCountMax, "Fissure count");
            RequireRange(BonePileCountMin, BonePileCountMax, "Bone pile count");
            Require(LavaPoolCount >= 0 && WaterPoolCount >= 0, "Pool counts are never negative.");
            RequireRange(RockSizeMin, RockSizeMax, "Rock size");
            RequireRange(FissureLengthMin, FissureLengthMax, "Fissure length");
            Require(FissureWidth > 0f, "FissureWidth must be positive.");
            RequireRange(LavaPoolRadiusMin, LavaPoolRadiusMax, "Lava pool radius");
            RequireRange(WaterPoolRadiusMin, WaterPoolRadiusMax, "Water pool radius");
            RequireRange(BonePileRadiusMin, BonePileRadiusMax, "Bone pile radius");
            Require(InitialBlobs >= 0, "InitialBlobs is never negative.");
            Require(SpawnClusterRadius > 0f && SpawnClearRadius >= SpawnClusterRadius, "Spawn clear radius must cover the cluster.");
            Require(ElderRouteRadius > 0f && ElderRouteRadius + ElderRouteJitter < SizeMeters * 0.5f - WallInset - FeatureMargin, "Elder route must fit inside the feature area.");
            Require(ElderRouteWaypoints >= 3, "An elder route needs at least three waypoints.");
            Require(ElderRouteJitter >= 0f && ElderRouteClearance >= 0f && ElderMinSpawnDistance >= 0f, "Elder route distances are never negative.");
            Require(BlobTemplate != null && ElderTemplate != null, "Both demon templates are required.");
            Require(BlobArchetype != null && ElderArchetype != null, "Both archetypes are required.");
        }

        private void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Biome " + Id + ": " + message);
            }
        }

        private void RequireRange(float min, float max, string what)
        {
            Require(min > 0f && max >= min, what + " range must be positive and ordered.");
        }

        private void RequireRange(int min, int max, string what)
        {
            Require(min >= 0 && max >= min, what + " range must be non-negative and ordered.");
        }
    }
}
