#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Immutable description of one skill (ARCHITECTURE, "Specs"): its damage, cost, reach, timing, side effects and
    /// how it grows with use. The behaviour id names the small class that carries it out; the defaults describe Bite.
    /// </summary>
    public sealed record SkillSpec
    {
        /// <summary>Stable content id, lowercase and dotted, for example skill.bite.</summary>
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        /// <summary>Key of the behaviour class found by attribute; melee-strike is the data-driven melee hit.</summary>
        public string BehaviourId { get; init; } = "melee-strike";

        public DamageType DamageType { get; init; } = DamageType.Pierce;

        /// <summary>Damage of one hit at skill level 1 before stats and armor.</summary>
        public float BaseDamage { get; init; } = 12f;

        /// <summary>Stamina taken when the skill starts, at skill level 1.</summary>
        public float StaminaCost { get; init; } = 15f;

        /// <summary>Reach in meters per meter of body size.</summary>
        public float ReachPerMeter { get; init; } = 1.4f;

        /// <summary>Total width of the cone in front of the demon that a hit may land in.</summary>
        public float ArcDegrees { get; init; } = 70f;

        public float WindupSeconds { get; init; } = 0.15f;

        public float ActiveSeconds { get; init; } = 0.2f;

        public float RecoverySeconds { get; init; } = 0.35f;

        /// <summary>Time from one activation to the next, counted from the start.</summary>
        public float CooldownSeconds { get; init; } = 0.6f;

        /// <summary>How long a hit bleeds; zero for no bleeding.</summary>
        public float BleedSeconds { get; init; } = 3f;

        public float BleedDamagePerSecond { get; init; } = 2f;

        /// <summary>How long a hit staggers the target; zero for none. Blunt skills stagger.</summary>
        public float StaggerSeconds { get; init; }

        /// <summary>Skill XP every landed hit grants.</summary>
        public float SkillXpPerHit { get; init; } = 10f;

        public SkillLevelCurve LevelCurve { get; init; } = new SkillLevelCurve();

        /// <summary>Damage bonus per skill level above 1, as a fraction.</summary>
        public float DamageBonusPerLevel { get; init; } = 0.05f;

        /// <summary>Stamina cost reduction per skill level above 1, as a fraction.</summary>
        public float StaminaCostReductionPerLevel { get; init; } = 0.03f;

        /// <summary>Stamina cost never drops below this fraction of the base cost.</summary>
        public float MinStaminaCostFraction { get; init; } = 0.5f;

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            Require(!string.IsNullOrWhiteSpace(Id), "Id is required.");
            Require(!string.IsNullOrWhiteSpace(Name), "Name is required.");
            Require(!string.IsNullOrWhiteSpace(BehaviourId), "BehaviourId is required.");
            Require(BaseDamage >= 0f, "BaseDamage is never negative.");
            Require(StaminaCost >= 0f, "StaminaCost is never negative.");
            Require(ReachPerMeter > 0f, "ReachPerMeter must be positive.");
            Require(ArcDegrees > 0f && ArcDegrees <= 360f, "ArcDegrees must be in (0, 360].");
            Require(WindupSeconds >= 0f && RecoverySeconds >= 0f, "Windup and recovery are never negative.");
            Require(ActiveSeconds > 0f, "ActiveSeconds must be positive.");
            Require(CooldownSeconds >= 0f, "CooldownSeconds is never negative.");
            Require(BleedSeconds >= 0f && BleedDamagePerSecond >= 0f, "Bleeding values are never negative.");
            Require(StaggerSeconds >= 0f, "StaggerSeconds is never negative.");
            Require(SkillXpPerHit >= 0f, "SkillXpPerHit is never negative.");
            Require(DamageBonusPerLevel >= 0f, "DamageBonusPerLevel is never negative.");
            Require(StaminaCostReductionPerLevel >= 0f, "StaminaCostReductionPerLevel is never negative.");
            Require(MinStaminaCostFraction >= 0f && MinStaminaCostFraction <= 1f, "MinStaminaCostFraction must be in [0, 1].");
            Require(LevelCurve != null, "LevelCurve is required.");
            LevelCurve.Validate();
        }

        private void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new ContentException("Skill " + Id + ": " + message);
            }
        }
    }
}
