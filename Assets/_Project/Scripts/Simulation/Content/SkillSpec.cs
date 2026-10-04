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

        /// <summary>Key slot the skill is bound to; several granted skills in one slot resolve by priority.</summary>
        public SkillSlot InputSlot { get; init; } = SkillSlot.Primary;

        /// <summary>Higher wins the slot: Claw replaces Bite on the primary attack.</summary>
        public int SlotPriority { get; init; }

        /// <summary>Passive skills are never started by a command; Sprint changes how movement works instead.</summary>
        public bool IsPassive { get; init; }

        /// <summary>True for the passive skill that lets its owner sprint (Legs).</summary>
        public bool EnablesSprint { get; init; }

        /// <summary>Stamina per second a passive skill drains while in use, at skill level 1; Sprint.</summary>
        public float StaminaCostPerSecond { get; init; }

        /// <summary>Skill XP per second a passive skill earns while in use.</summary>
        public float SkillXpPerSecond { get; init; }

        /// <summary>How long a landed hit holds a smaller demon in place; Grab.</summary>
        public float HoldSeconds { get; init; }

        /// <summary>Meters the user leaps while the skill runs; Lunge.</summary>
        public float DashMeters { get; init; }

        /// <summary>Meters a landed hit pushes the target away; Lunge.</summary>
        public float KnockbackMeters { get; init; }

        /// <summary>Cooldown reduction per skill level above 1, as a fraction, floored by MinCooldownFraction.</summary>
        public float CooldownReductionPerLevel { get; init; } = 0.02f;

        public float MinCooldownFraction { get; init; } = 0.5f;

        /// <summary>Speed-up per skill level above 1: windup, active window and recovery shrink by this fraction.</summary>
        public float SpeedBonusPerLevel { get; init; } = 0.02f;

        /// <summary>Reach bonus per skill level above 1, as a fraction.</summary>
        public float ReachBonusPerLevel { get; init; } = 0.01f;

        /// <summary>The one perk of v1 (O-011), or null; content decides what the threshold brings.</summary>
        public SkillPerkSpec? Perk { get; init; }

        /// <summary>The perk in effect at a skill level, or null below its threshold.</summary>
        public SkillPerkSpec? PerkAt(int level)
        {
            return Perk != null && level >= Perk.Level ? Perk : null;
        }

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            Require(!string.IsNullOrWhiteSpace(Id), "Id is required.");
            Require(!string.IsNullOrWhiteSpace(Name), "Name is required.");
            Require(IsPassive || !string.IsNullOrWhiteSpace(BehaviourId), "BehaviourId is required for active skills.");
            Require(StaminaCostPerSecond >= 0f && SkillXpPerSecond >= 0f && HoldSeconds >= 0f && DashMeters >= 0f && KnockbackMeters >= 0f, "Effect values are never negative.");
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
            Require(CooldownReductionPerLevel >= 0f && SpeedBonusPerLevel >= 0f && ReachBonusPerLevel >= 0f, "Level rates are never negative.");
            Require(MinCooldownFraction >= 0f && MinCooldownFraction <= 1f, "MinCooldownFraction must be in [0, 1].");
            Require(LevelCurve != null, "LevelCurve is required.");
            LevelCurve.Validate();
            Perk?.Validate();
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
