#nullable enable
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Tests.Events
{
    /// <summary>Stand-in for a real simulation event; public so NSubstitute can build delegates over it.</summary>
    public readonly struct TestEvent : ISimulationEvent
    {
        public TestEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
