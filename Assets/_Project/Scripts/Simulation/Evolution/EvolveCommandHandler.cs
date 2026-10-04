#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Evolution
{
    /// <summary>
    /// Applies a confirmed evolution when the rules allow it: the package lands at once (stat points, caps, unlocks,
    /// free parts where a socket is free, extra skills), the tier and the size grow one step, and the demon
    /// transforms like after a mutation (D-014).
    /// </summary>
    internal sealed class EvolveCommandHandler : ICommandHandler<EvolveCommand>
    {
        internal const string UnknownActor = "Unknown actor";
        internal const string UnknownEvolution = "Unknown evolution";

        private readonly SimulationEvents _events;

        public EvolveCommandHandler(SimulationEvents events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc />
        public CommandResult Handle(in EvolveCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? demon))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            if (!state.Catalog.TryGetEvolution(command.EvolutionId, out EvolutionSpec? spec))
            {
                return CommandResult.Rejected(UnknownEvolution);
            }

            if (!EvolutionRules.CanEvolve(demon, spec, state, out string reason))
            {
                return CommandResult.Rejected(reason);
            }

            EvolutionPackage.Apply(demon, spec, state.Catalog);
            long untilTick = state.Tick + state.Config.TicksFor(state.Catalog.Tuning.TransformationSeconds);
            demon.StartTransformation(state.Tick, untilTick);
            _events.Publish(new Evolved(demon.Id, spec.Id, spec.Stage, untilTick));
            return CommandResult.Accepted;
        }
    }
}
