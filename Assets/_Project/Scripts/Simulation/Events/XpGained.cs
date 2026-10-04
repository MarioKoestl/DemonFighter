#nullable enable
using DemonFighter.Simulation.Progression;

namespace DemonFighter.Simulation.Events
{
    /// <summary>A demon gained character XP; the HUD shows the bar move.</summary>
    public readonly struct XpGained : ISimulationEvent
    {
        public XpGained(DemonId demon, float amount, XpSource source)
        {
            Demon = demon;
            Amount = amount;
            Source = source;
        }

        public DemonId Demon { get; }

        public float Amount { get; }

        public XpSource Source { get; }
    }
}
