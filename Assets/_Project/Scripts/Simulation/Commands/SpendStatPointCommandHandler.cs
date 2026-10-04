#nullable enable
using System;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Puts one unspent stat point into a base stat (GAME_DESIGN, "The three progression layers": the Stats tab) and
    /// recomputes what the stats mean. Allowed in combat; only mutations wait for calm.
    /// </summary>
    internal sealed class SpendStatPointCommandHandler : ICommandHandler<SpendStatPointCommand>
    {
        internal const string UnknownActor = "Unknown actor";
        internal const string ActorDead = "Actor is dead";
        internal const string UnknownStat = "Unknown stat";
        internal const string NoPoints = "No unspent stat points";
        internal const string AtCap = "Stat is at its cap";

        private readonly SimulationEvents _events;

        public SpendStatPointCommandHandler(SimulationEvents events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc />
        public CommandResult Handle(in SpendStatPointCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? demon))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            if (!demon.IsAlive)
            {
                return CommandResult.Rejected(ActorDead);
            }

            if (!demon.Stats.Has(command.Stat))
            {
                return CommandResult.Rejected(UnknownStat);
            }

            if (demon.Stats.Get(command.Stat) >= demon.StatCap(command.Stat))
            {
                return CommandResult.Rejected(AtCap);
            }

            if (!demon.Stats.TrySpendPoint(command.Stat))
            {
                return CommandResult.Rejected(NoPoints);
            }

            demon.RecomputeDerived(state.Catalog.Tuning);
            _events.Publish(new StatPointSpent(demon.Id, command.Stat, demon.Stats.Get(command.Stat), demon.Stats.UnspentPoints));
            return CommandResult.Accepted;
        }
    }
}
