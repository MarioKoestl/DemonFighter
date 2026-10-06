#nullable enable
namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// The ways to hurt a body. Demons hurt each other three ways (D-022): Bite pierces, Claw cuts, Lunge and Tail
    /// Swing bludgeon. Fire comes only from the world, lava and glowing fissures (D-086), never from a skill. Fixed by
    /// design; the numbers behind them are tuning data.
    /// </summary>
    public enum DamageType
    {
        Pierce,
        Cut,
        Blunt,
        Fire,
    }
}
