#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;

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

        /// <summary>Share of the touching part's health lava burns per second (D-086); 0.2 burns a part away in five seconds.</summary>
        public float LavaBurnFractionPerSecond { get; init; } = 0.2f;

        /// <summary>Share of the touching part's health a glowing fissure burns per second (D-086).</summary>
        public float FissureBurnFractionPerSecond { get; init; } = 0.05f;

        /// <summary>How far AI demons keep their bodies from the edge of lava and fissures, in meters (D-086).</summary>
        public float HazardAvoidMarginMeters { get; init; } = 1.5f;

        public int WaterPoolCount { get; init; } = 3;

        public float WaterPoolRadiusMin { get; init; } = 5f;

        public float WaterPoolRadiusMax { get; init; } = 9f;

        public int BonePileCountMin { get; init; } = 8;

        public int BonePileCountMax { get; init; } = 14;

        public float BonePileRadiusMin { get; init; } = 1.5f;

        public float BonePileRadiusMax { get; init; } = 3f;

        /// <summary>Tier 0 demons spawned around the player at run start (D-029; raised for a busier world, D-078).</summary>
        public int InitialBlobs { get; init; } = 16;

        /// <summary>Radius of the circle the start spawns are scattered in.</summary>
        public float SpawnClusterRadius { get; init; } = 30f;

        /// <summary>No feature inside this distance of the spawn center, so the first minute is open ground.</summary>
        public float SpawnClearRadius { get; init; } = 30f;

        /// <summary>Seconds between top-up spawns while fewer Tier 0 demons live than InitialBlobs; zero disables them (D-053).</summary>
        public float RespawnSeconds { get; init; } = 6f;

        /// <summary>A top-up spawn lands at least this far from the player, in meters.</summary>
        public float RespawnMinDistance { get; init; } = 25f;

        /// <summary>A top-up spawn lands at most this far from the player, in meters.</summary>
        public float RespawnMaxDistance { get; init; } = 55f;

        /// <summary>Threat levels gained per simulated minute (D-069); the only clock of a run (D-016).</summary>
        public float ThreatPerMinute { get; init; } = 0.5f;

        /// <summary>The threat never rises past this level.</summary>
        public int ThreatMaxLevel { get; init; } = 10;

        /// <summary>More AI demons allowed alive per threat level on top of InitialBlobs (D-070).</summary>
        public int BlobsPerThreatLevel { get; init; } = 2;

        /// <summary>The respawn interval is divided by one plus this times the threat level (D-070).</summary>
        public float RespawnSpeedupPerThreat { get; init; } = 0.2f;

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

        /// <summary>The Tier 0 kind the player and the start spawns are born as.</summary>
        public DemonSpec BlobDemon { get; init; } = new DemonSpec { Id = "demon.blob", Name = "Blob" };

        /// <summary>A blob born with an arm, the first stronger spawn (D-070).</summary>
        public DemonSpec HunterDemon { get; init; } = new DemonSpec
        {
            Id = "demon.hunter",
            Name = "Hunter",
            StartingPartIds = new[] { "part.arm" },
            StartingBiomass = 30f,
            StartingLevel = 2,
        };

        /// <summary>A blob born with legs and eyes.</summary>
        public DemonSpec StalkerDemon { get; init; } = new DemonSpec
        {
            Id = "demon.stalker",
            Name = "Stalker",
            StartingPartIds = new[] { "part.legs", "part.eyes" },
            StartingBiomass = 40f,
            StartingLevel = 3,
        };

        /// <summary>A blob born as a Brute: arm, jaws, thick hide and the first Brute evolution.</summary>
        public DemonSpec BruteDemon { get; init; } = new DemonSpec
        {
            Id = "demon.brute",
            Name = "Brute",
            StartingPartIds = new[] { "part.arm", "part.jaws", "part.hide.thick" },
            StartingBiomass = 60f,
            StartingLevel = 5,
            StartingEvolutionId = "evolution.brute.1",
        };

        /// <summary>
        /// The high-tier kind that walks the elder loop: born Tier 4 at 7.5 m and evolved twice along a random line, so it
        /// walks as Tier 6 at 15 m, with a random body at its highest upgrades (D-095).
        /// </summary>
        public DemonSpec ElderDemon { get; init; } = new DemonSpec
        {
            Id = "demon.elder",
            Name = "Elder",
            Tier = 4,
            SizeMeters = 7.5f,
            MoveSpeed = 3f,
            SprintMultiplier = 1f,
            StartingStats = new[] { new StatValue(StatIds.Strength, 10), new StatValue(StatIds.Constitution, 20) },
            RandomPartChance = 0.75f,
            StartingPartsAtMaxUpgrade = true,
            RandomEvolutionStages = 2,
        };

        /// <summary>Fleeing is implemented but switched off in the M2 content (threshold 0): fights are hard to test when prey runs.</summary>
        public ArchetypeSpec BlobArchetype { get; init; } = new ArchetypeSpec(
            "Blob", wanderWeight: 1f, restWeight: 0.8f, patrolWeight: 0f, wanderRadius: 20f,
            restSecondsMin: 2f, restSecondsMax: 5f, decisionIntervalTicks: 10, arriveDistance: 1f,
            huntWeight: 0.8f, eatWeight: 2.5f, fleeHealthFraction: 0f, perceptionRadius: 18f);

        /// <summary>The elder walks its loop, pulled toward the player by a tenth per threat level, at most seven tenths (D-070).</summary>
        public ArchetypeSpec ElderArchetype { get; init; } = new ArchetypeSpec(
            "Elder", wanderWeight: 0f, restWeight: 0.15f, patrolWeight: 1f, wanderRadius: 30f,
            restSecondsMin: 3f, restSecondsMax: 6f, decisionIntervalTicks: 10, arriveDistance: 4f,
            huntWeight: 1.5f, eatWeight: 1f, fleeHealthFraction: 0f, perceptionRadius: 60f,
            routePullPerThreat: 0.1f, routePullMax: 0.7f);

        /// <summary>Which kinds spawn from which threat level (D-070); empty means only the blob.</summary>
        public IReadOnlyList<SpawnEntry> SpawnTable { get; init; } = Array.Empty<SpawnEntry>();

        /// <summary>The Ash Cavern table: blobs always, hunters from threat 2, stalkers from 4, brutes from 6.</summary>
        public static IReadOnlyList<SpawnEntry> DefaultSpawnTable(BiomeSpec biome)
        {
            return new[]
            {
                new SpawnEntry(biome.BlobDemon, 0, 1f),
                new SpawnEntry(biome.HunterDemon, 2, 0.6f),
                new SpawnEntry(biome.StalkerDemon, 4, 0.5f),
                new SpawnEntry(biome.BruteDemon, 6, 0.4f),
            };
        }

        /// <summary>The hunter among the blobs (D-071): most likely to attack, slowest to flee, builds toward the Brute line.</summary>
        public ArchetypeSpec AggressiveArchetype { get; init; } = new ArchetypeSpec(
            "Aggressive", wanderWeight: 0.6f, restWeight: 0.3f, patrolWeight: 0f, wanderRadius: 20f,
            restSecondsMin: 2f, restSecondsMax: 4f, decisionIntervalTicks: 10, arriveDistance: 1f,
            huntWeight: 2f, eatWeight: 1.5f, fleeHealthFraction: 0.15f, perceptionRadius: 20f)
        {
            PreferredPartIds = new[] { "part.jaws", "part.arm", "part.legs" },
            PreferredEvolutionStat = StatIds.Strength,
            PreferredStat = StatIds.Strength,
        };

        /// <summary>Eats and rests more, flees early, armors up toward the Bulwark line.</summary>
        public ArchetypeSpec CautiousArchetype { get; init; } = new ArchetypeSpec(
            "Cautious", wanderWeight: 0.8f, restWeight: 1f, patrolWeight: 0f, wanderRadius: 20f,
            restSecondsMin: 2f, restSecondsMax: 5f, decisionIntervalTicks: 10, arriveDistance: 1f,
            huntWeight: 0.6f, eatWeight: 2f, fleeHealthFraction: 0.4f, perceptionRadius: 22f)
        {
            PreferredPartIds = new[] { "part.hide.thick", "part.eyes", "part.legs" },
            PreferredEvolutionStat = StatIds.Constitution,
            PreferredStat = StatIds.Constitution,
        };

        /// <summary>Lives off corpses, hunts only sure things, runs at half health, grows fast and sharp toward the Stalker line.</summary>
        public ArchetypeSpec ScavengerArchetype { get; init; } = new ArchetypeSpec(
            "Scavenger", wanderWeight: 1f, restWeight: 0.6f, patrolWeight: 0f, wanderRadius: 24f,
            restSecondsMin: 1f, restSecondsMax: 3f, decisionIntervalTicks: 10, arriveDistance: 1f,
            huntWeight: 0.4f, eatWeight: 3f, fleeHealthFraction: 0.5f, perceptionRadius: 24f)
        {
            PreferredPartIds = new[] { "part.legs", "part.eyes", "part.jaws" },
            PreferredEvolutionStat = StatIds.Agility,
            PreferredStat = StatIds.Agility,
        };

        /// <summary>Personalities new blobs draw from by weight (D-071); empty means every blob gets BlobArchetype.</summary>
        public IReadOnlyList<ArchetypeChoice> BlobArchetypes { get; init; } = Array.Empty<ArchetypeChoice>();

        /// <summary>The Ash Cavern mix: half aggressive, three in ten cautious, two in ten scavengers.</summary>
        public static IReadOnlyList<ArchetypeChoice> DefaultBlobArchetypes(BiomeSpec biome)
        {
            return new[]
            {
                new ArchetypeChoice(biome.AggressiveArchetype, 50f),
                new ArchetypeChoice(biome.CautiousArchetype, 30f),
                new ArchetypeChoice(biome.ScavengerArchetype, 20f),
            };
        }

        /// <summary>Draws the personality of a new blob by weight from the run Rng; the plain blob archetype when none are listed.</summary>
        public ArchetypeSpec PickBlobArchetype(Rng rng)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            if (BlobArchetypes.Count == 0)
            {
                return BlobArchetype;
            }

            float total = 0f;
            for (int i = 0; i < BlobArchetypes.Count; i++)
            {
                total += BlobArchetypes[i].Weight;
            }

            float roll = rng.NextFloat(0f, total);
            for (int i = 0; i < BlobArchetypes.Count; i++)
            {
                if (roll < BlobArchetypes[i].Weight)
                {
                    return BlobArchetypes[i].Archetype;
                }

                roll -= BlobArchetypes[i].Weight;
            }

            return BlobArchetypes[BlobArchetypes.Count - 1].Archetype;
        }

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
            Require(LavaBurnFractionPerSecond >= 0f && FissureBurnFractionPerSecond >= 0f && HazardAvoidMarginMeters >= 0f, "Hazard burn and margin are never negative.");
            RequireRange(RockSizeMin, RockSizeMax, "Rock size");
            RequireRange(FissureLengthMin, FissureLengthMax, "Fissure length");
            Require(FissureWidth > 0f, "FissureWidth must be positive.");
            RequireRange(LavaPoolRadiusMin, LavaPoolRadiusMax, "Lava pool radius");
            RequireRange(WaterPoolRadiusMin, WaterPoolRadiusMax, "Water pool radius");
            RequireRange(BonePileRadiusMin, BonePileRadiusMax, "Bone pile radius");
            Require(InitialBlobs >= 0, "InitialBlobs is never negative.");
            Require(SpawnClusterRadius > 0f && SpawnClearRadius >= SpawnClusterRadius, "Spawn clear radius must cover the cluster.");
            Require(RespawnSeconds >= 0f, "RespawnSeconds is never negative.");
            Require(RespawnMinDistance > 0f && RespawnMaxDistance >= RespawnMinDistance, "Respawn distances must be positive and ordered.");
            Require(ThreatPerMinute >= 0f && ThreatMaxLevel >= 0, "Threat pace and cap are never negative.");
            Require(BlobsPerThreatLevel >= 0 && RespawnSpeedupPerThreat >= 0f, "Threat scaling of spawns is never negative.");
            Require(SpawnTable != null, "SpawnTable must not be null.");
            Require(BlobArchetypes != null, "BlobArchetypes must not be null.");
            Require(ElderRouteRadius > 0f && ElderRouteRadius + ElderRouteJitter < SizeMeters * 0.5f - WallInset - FeatureMargin, "Elder route must fit inside the feature area.");
            Require(ElderRouteWaypoints >= 3, "An elder route needs at least three waypoints.");
            Require(ElderRouteJitter >= 0f && ElderRouteClearance >= 0f && ElderMinSpawnDistance >= 0f, "Elder route distances are never negative.");
            Require(BlobDemon != null && ElderDemon != null, "Both demon kinds are required.");
            Require(BlobArchetype != null && ElderArchetype != null, "Both archetypes are required.");
            BlobDemon.Validate();
            ElderDemon.Validate();
            for (int i = 0; i < SpawnTable.Count; i++)
            {
                SpawnTable[i].Demon.Validate();
            }
        }

        private void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string message)
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
