#nullable enable
using System;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// How much skill XP each level of a skill needs (GAME_DESIGN, "Skill levels"): the requirement grows as a power
    /// of the current level, so early levels come fast and late ones slowly.
    /// </summary>
    public sealed record SkillLevelCurve
    {
        /// <summary>XP from level 1 to level 2.</summary>
        public float BaseXp { get; init; } = 100f;

        /// <summary>Growth of the requirement per level; 1 is linear.</summary>
        public float Exponent { get; init; } = 1.5f;

        /// <summary>Highest level a skill can reach.</summary>
        public int MaxLevel { get; init; } = 20;

        /// <summary>XP needed to advance from the given level to the next one.</summary>
        public float XpForNextLevel(int currentLevel)
        {
            if (currentLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevel), currentLevel, "Levels start at 1.");
            }

            return BaseXp * MathF.Pow(currentLevel, Exponent);
        }

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            if (BaseXp <= 0f)
            {
                throw new ContentException("Skill level curve: BaseXp must be positive.");
            }

            if (Exponent < 0f)
            {
                throw new ContentException("Skill level curve: Exponent is never negative.");
            }

            if (MaxLevel < 1)
            {
                throw new ContentException("Skill level curve: MaxLevel must be at least 1.");
            }
        }
    }
}
