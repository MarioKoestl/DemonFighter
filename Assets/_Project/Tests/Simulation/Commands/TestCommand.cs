#nullable enable
using DemonFighter.Simulation.Commands;

namespace DemonFighter.Simulation.Tests.Commands
{
    /// <summary>Stand-in command carrying a value, so queue tests can check order and payload.</summary>
    public readonly struct TestCommand : ICommand
    {
        public TestCommand(DemonId actor, int value)
        {
            Actor = actor;
            Value = value;
        }

        public DemonId Actor { get; }

        public int Value { get; }
    }
}
