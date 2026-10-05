#nullable enable
namespace DemonFighter.Simulation.Hazards
{
    /// <summary>What part of the world burns a demon standing in it (GAME_DESIGN, "World"; D-086).</summary>
    public enum HazardKind
    {
        None,

        /// <summary>A glowing crack in the ground; burns a little.</summary>
        Fissure,

        /// <summary>A lava pool; burns hard. Wins over a fissure where both overlap.</summary>
        Lava,
    }
}
