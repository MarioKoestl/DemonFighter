#nullable enable
using DemonFighter.Simulation.Commands;

namespace DemonFighter.Simulation.Evolution
{
    /// <summary>A confirmed evolution option from the Evolve tab.</summary>
    public readonly struct EvolveCommand : ICommand
    {
        public EvolveCommand(DemonId actor, string evolutionId)
        {
            Actor = actor;
            EvolutionId = evolutionId;
        }

        /// <inheritdoc />
        public DemonId Actor { get; }

        /// <summary>Content id of the chosen evolution.</summary>
        public string EvolutionId { get; }
    }
}
