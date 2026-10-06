#nullable enable
using DemonFighter.Simulation.Commands;

namespace DemonFighter.Simulation.Playtest
{
    /// <summary>
    /// Switches test mode (D-089) for one demon. The refill follows right after the commands of the pass, so the
    /// Biomass and stat points are there in the same pass.
    /// </summary>
    internal sealed class SetTestModeCommandHandler : ICommandHandler<SetTestModeCommand>
    {
        internal const string UnknownActor = "Unknown actor";

        /// <inheritdoc />
        public CommandResult Handle(in SetTestModeCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? demon))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            demon.SetTestMode(command.Enabled);
            return CommandResult.Accepted;
        }
    }
}
