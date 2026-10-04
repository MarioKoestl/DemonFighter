#nullable enable
using System.Collections.Generic;

namespace DemonFighter.Simulation.Persistence
{
    /// <summary>
    /// A whole run as plain data (D-073): no behaviour, no Unity type, nothing a JSON library cannot read and write.
    /// <see cref="RunPersistence.Capture"/> fills it and <see cref="RunPersistence.Restore"/> rebuilds a run from it;
    /// the App layer only moves it to and from a file. Ids are the simulation ids as plain integers, enums as integers.
    /// </summary>
    public sealed class RunSnapshot
    {
        /// <summary>The layout this code writes; a file with another version is refused.</summary>
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;

        public int Seed { get; set; }

        public int TicksPerSecond { get; set; }

        public long Tick { get; set; }

        public ulong RngState { get; set; }

        public int LastDemonId { get; set; }

        public int LastFoodId { get; set; }

        public long NextSpawnTick { get; set; }

        public List<DemonSnapshot> Demons { get; set; } = new List<DemonSnapshot>();

        public List<FoodSnapshot> Food { get; set; } = new List<FoodSnapshot>();
    }

    /// <summary>One demon, alive or dead, with everything the simulation keeps about it.</summary>
    public sealed class DemonSnapshot
    {
        public int Id { get; set; }

        public int Controller { get; set; }

        public string SpecId { get; set; } = string.Empty;

        /// <summary>Name of the archetype that drove this demon; empty for the player. Resolved against the biome on restore.</summary>
        public string ArchetypeName { get; set; } = string.Empty;

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float Yaw { get; set; }

        public int Level { get; set; }

        public float Xp { get; set; }

        public float Biomass { get; set; }

        public float BiomassEaten { get; set; }

        public float Stamina { get; set; }

        public int Kills { get; set; }

        public int Evolutions { get; set; }

        public int HighestTier { get; set; }

        public List<string> EvolutionIds { get; set; } = new List<string>();

        public int UnspentPoints { get; set; }

        public List<StatValueSnapshot> Stats { get; set; } = new List<StatValueSnapshot>();

        public List<StatValueSnapshot> CapBonuses { get; set; } = new List<StatValueSnapshot>();

        public List<string> UnlockedPartIds { get; set; } = new List<string>();

        public List<PartSnapshot> Parts { get; set; } = new List<PartSnapshot>();

        public List<SkillSnapshot> Skills { get; set; } = new List<SkillSnapshot>();

        public long StaggeredUntilTick { get; set; }

        public long LastCombatTick { get; set; }

        public int LastAttackedBy { get; set; }

        public long LastAttackedTick { get; set; }

        public long TransformingUntilTick { get; set; }

        public long TransformationStartedTick { get; set; }

        public long HeldUntilTick { get; set; }

        public int HeldBy { get; set; }

        public float HeldOffsetX { get; set; }

        public float HeldOffsetY { get; set; }

        public float ExternalVelocityX { get; set; }

        public float ExternalVelocityY { get; set; }

        public long ExternalVelocityUntilTick { get; set; }

        public int EatingFoodId { get; set; }

        public long EatRequestTick { get; set; }

        public float SprintXpBuffer { get; set; }
    }

    /// <summary>Points of one stat, or the cap bonus of one stat.</summary>
    public sealed class StatValueSnapshot
    {
        public string StatId { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    /// <summary>One body part in list order; the first is the core.</summary>
    public sealed class PartSnapshot
    {
        public string SpecId { get; set; } = string.Empty;

        public int UpgradeLevel { get; set; }

        public float MaxHp { get; set; }

        public float Hp { get; set; }

        public bool IsLost { get; set; }

        public float BleedSecondsLeft { get; set; }

        public float BleedDamagePerSecond { get; set; }

        public int BleedType { get; set; }
    }

    /// <summary>One skill of a demon with its progress.</summary>
    public sealed class SkillSnapshot
    {
        public string SpecId { get; set; } = string.Empty;

        public bool GrantedByEvolution { get; set; }

        public int Level { get; set; }

        public float Xp { get; set; }

        public long CooldownUntilTick { get; set; }
    }

    /// <summary>One corpse or severed part lying in the world.</summary>
    public sealed class FoodSnapshot
    {
        public int Id { get; set; }

        public int Kind { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float BiomassRemaining { get; set; }

        public int Source { get; set; }

        public int SourceTier { get; set; }

        public long DecayTicksLeft { get; set; }
    }
}
