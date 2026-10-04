#nullable enable
using System;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Mutation
{
    /// <summary>
    /// Applies a confirmed mutation when the rules allow it: the Biomass is spent, the body changes at once, and the
    /// demon transforms for the tuning time, invulnerable and unable to act (D-014). The view grows the part during
    /// the transformation and the menu shows the same reasons this handler refuses with.
    /// </summary>
    internal sealed class MutateCommandHandler : ICommandHandler<MutateCommand>
    {
        internal const string UnknownActor = "Unknown actor";
        internal const string UnknownPart = "Unknown part";

        private readonly SimulationEvents _events;

        public MutateCommandHandler(SimulationEvents events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc />
        public CommandResult Handle(in MutateCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? demon))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            switch (command.Kind)
            {
                case MutationKind.Attach:
                    return Attach(demon, command.PartId, state);
                case MutationKind.Upgrade:
                    return Upgrade(demon, command.PartIndex, state);
                case MutationKind.Regrow:
                    return Regrow(demon, command.PartIndex, state);
                default:
                    throw new InvalidOperationException("Unknown mutation kind " + command.Kind + ".");
            }
        }

        private CommandResult Attach(Demon demon, string partId, RunState state)
        {
            if (!state.Catalog.TryGetBodyPart(partId, out BodyPartSpec? spec))
            {
                return CommandResult.Rejected(UnknownPart);
            }

            if (!MutationRules.CanAttach(demon, spec, state, out string reason, out float cost))
            {
                return CommandResult.Rejected(reason);
            }

            demon.SpendBiomass(cost);
            BodyPart part = demon.AttachPart(spec);
            return Transform(demon, MutationKind.Attach, part, cost, state);
        }

        private CommandResult Upgrade(Demon demon, int partIndex, RunState state)
        {
            if (!demon.Body.HasPart(partIndex))
            {
                return CommandResult.Rejected(UnknownPart);
            }

            BodyPart part = demon.Body.GetPart(partIndex);
            if (!MutationRules.CanUpgrade(demon, part, state, out string reason, out float cost))
            {
                return CommandResult.Rejected(reason);
            }

            demon.SpendBiomass(cost);
            demon.UpgradePart(part);
            return Transform(demon, MutationKind.Upgrade, part, cost, state);
        }

        private CommandResult Regrow(Demon demon, int partIndex, RunState state)
        {
            if (!demon.Body.HasPart(partIndex))
            {
                return CommandResult.Rejected(UnknownPart);
            }

            BodyPart part = demon.Body.GetPart(partIndex);
            if (!MutationRules.CanRegrow(demon, part, state, out string reason, out float cost))
            {
                return CommandResult.Rejected(reason);
            }

            demon.SpendBiomass(cost);
            demon.RegrowPart(part);
            return Transform(demon, MutationKind.Regrow, part, cost, state);
        }

        private CommandResult Transform(Demon demon, MutationKind kind, BodyPart part, float cost, RunState state)
        {
            long untilTick = state.Tick + state.Config.TicksFor(state.Catalog.Tuning.TransformationSeconds);
            demon.StartTransformation(state.Tick, untilTick);
            _events.Publish(new MutationStarted(demon.Id, kind, part.Index, part.Spec.Id, cost, untilTick));
            return CommandResult.Accepted;
        }
    }
}
