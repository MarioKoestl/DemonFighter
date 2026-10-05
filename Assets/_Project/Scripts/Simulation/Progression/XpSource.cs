#nullable enable
namespace DemonFighter.Simulation.Progression
{
    /// <summary>Where character XP came from (GAME_DESIGN, "The three progression layers").</summary>
    public enum XpSource
    {
        /// <summary>A kill, scaled by the victim's tier and level and the tier gap; the only source since D-090.</summary>
        Kill,
    }
}
