#nullable enable
namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// The three ways to hurt a body (D-022): Bite pierces, Claw cuts, Lunge and Tail Swing bludgeon. Fixed by design;
    /// the numbers behind them are tuning data.
    /// </summary>
    public enum DamageType
    {
        Pierce,
        Cut,
        Blunt,
    }
}
