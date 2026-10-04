#nullable enable
namespace DemonFighter.Simulation.Progression
{
    /// <summary>Where character XP came from (GAME_DESIGN, "The three progression layers").</summary>
    public enum XpSource
    {
        /// <summary>A kill, scaled by the tier gap.</summary>
        Kill,

        /// <summary>The share of skill XP that feeds character XP.</summary>
        SkillUse,
    }
}
