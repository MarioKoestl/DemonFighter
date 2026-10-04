#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Commands;

namespace DemonFighter.Simulation.Tests.Commands
{
    /// <summary>Fake handler that records what it saw and rejects every value below zero.</summary>
    internal sealed class RecordingHandler : ICommandHandler<TestCommand>
    {
        public const string NegativeReason = "Negative values are refused";

        public List<int> Seen { get; } = new List<int>();

        public CommandResult Handle(in TestCommand command, RunState state)
        {
            Seen.Add(command.Value);
            return command.Value < 0 ? CommandResult.Rejected(NegativeReason) : CommandResult.Accepted;
        }
    }
}
