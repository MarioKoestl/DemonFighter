#nullable enable
namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Turns a move command into the demon's movement intent. Unknown and dead actors are refused; stamina and
    /// stagger rules for movement join when sprinting costs stamina.
    /// </summary>
    internal sealed class MoveCommandHandler : ICommandHandler<MoveCommand>
    {
        internal const string UnknownActor = "Unknown actor";
        internal const string ActorDead = "Actor is dead";
        internal const string Transforming = "Transforming";
        internal const string Held = "Held";

        /// <inheritdoc />
        public CommandResult Handle(in MoveCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? demon))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            if (!demon.IsAlive)
            {
                return CommandResult.Rejected(ActorDead);
            }

            if (demon.IsTransforming(state.Tick))
            {
                return CommandResult.Rejected(Transforming);
            }

            if (demon.IsHeld(state.Tick))
            {
                return CommandResult.Rejected(Held);
            }

            demon.SetIntent(new MovementIntent(command.Direction, command.Sprint, command.Facing));
            return CommandResult.Accepted;
        }
    }
}
