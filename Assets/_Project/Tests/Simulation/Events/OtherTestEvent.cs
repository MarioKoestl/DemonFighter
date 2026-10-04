#nullable enable
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Tests.Events
{
    /// <summary>A second event type, to test routing and ordering across types.</summary>
    public readonly struct OtherTestEvent : ISimulationEvent
    {
        public OtherTestEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
