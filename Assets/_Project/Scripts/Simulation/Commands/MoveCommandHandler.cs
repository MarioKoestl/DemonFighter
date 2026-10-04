#nullable enable
namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Turns a move command into the demon's movement intent. The only validation in M1 is that the actor exists;
    /// stamina and stagger rules join in M2.
    /// </summary>
    internal sealed class MoveCommandHandler : ICommandHandler<MoveCommand>
    {
        internal const string UnknownActor = "Unknown actor";

        /// <inheritdoc />
        public CommandResult Handle(in MoveCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? demon))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            demon.SetIntent(new MovementIntent(command.Direction, command.Sprint));
            return CommandResult.Accepted;
        }
    }
}
