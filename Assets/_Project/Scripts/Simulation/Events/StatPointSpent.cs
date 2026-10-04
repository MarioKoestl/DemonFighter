#nullable enable
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Events
{
    /// <summary>A stat point went into a base stat; the Stats tab refreshes its numbers.</summary>
    public readonly struct StatPointSpent : ISimulationEvent
    {
        public StatPointSpent(DemonId demon, StatId stat, int newValue, int unspentStatPoints)
        {
            Demon = demon;
            Stat = stat;
            NewValue = newValue;
            UnspentStatPoints = unspentStatPoints;
        }

        public DemonId Demon { get; }

        public StatId Stat { get; }

        public int NewValue { get; }

        public int UnspentStatPoints { get; }
    }
}
