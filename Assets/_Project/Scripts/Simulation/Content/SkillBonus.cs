#nullable enable
using System;

namespace DemonFighter.Simulation.Content
{
    /// <summary>A damage bonus a body part gives one skill per level while attached; Jaws make Bite hit harder.</summary>
    public readonly struct SkillBonus : IEquatable<SkillBonus>
    {
        public SkillBonus(string skillId, float damagePerLevel)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException("A skill bonus needs a skill id.", nameof(skillId));
            }

            if (damagePerLevel < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damagePerLevel), damagePerLevel, "Skill bonuses are never negative.");
            }

            SkillId = skillId;
            DamagePerLevel = damagePerLevel;
        }

        public string SkillId { get; }

        /// <summary>Fraction added to the damage of the skill per part level (upgrade level plus one).</summary>
        public float DamagePerLevel { get; }

        public bool Equals(SkillBonus other) => string.Equals(SkillId, other.SkillId, StringComparison.Ordinal) && DamagePerLevel.Equals(other.DamagePerLevel);

        public override bool Equals(object? obj) => obj is SkillBonus other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(SkillId, DamagePerLevel);

        public static bool operator ==(SkillBonus left, SkillBonus right) => left.Equals(right);

        public static bool operator !=(SkillBonus left, SkillBonus right) => !left.Equals(right);
    }
}
