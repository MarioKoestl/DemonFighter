#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// What a skill gains at a skill level threshold (GAME_DESIGN, "Skill levels"; O-011: one perk per skill at
    /// level 10 in v1). Every multiplier defaults to 1, so a perk only names what it changes: Bite bleeds longer,
    /// Claw recovers faster, Grab holds longer, Sprint costs less.
    /// </summary>
    public sealed record SkillPerkSpec
    {
        /// <summary>Skill level the perk arrives at.</summary>
        public int Level { get; init; } = 10;

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public float DamageMultiplier { get; init; } = 1f;

        public float BleedDurationMultiplier { get; init; } = 1f;

        public float RecoveryMultiplier { get; init; } = 1f;

        public float CooldownMultiplier { get; init; } = 1f;

        public float StaminaMultiplier { get; init; } = 1f;

        public float ReachMultiplier { get; init; } = 1f;

        /// <summary>Scales what the skill does beyond damage: hold time, dash and knockback distance, stagger.</summary>
        public float EffectMultiplier { get; init; } = 1f;

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            Require(Level >= 1, "Level starts at 1.");
            Require(!string.IsNullOrWhiteSpace(Name), "Name is required.");
            Require(
                DamageMultiplier > 0f && BleedDurationMultiplier > 0f && RecoveryMultiplier > 0f && CooldownMultiplier > 0f
                && StaminaMultiplier > 0f && ReachMultiplier > 0f && EffectMultiplier > 0f,
                "Multipliers must be positive.");
        }

        private void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new ContentException("Skill perk " + Name + ": " + message);
            }
        }
    }
}
