#nullable enable
namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Validates and applies one command type against the run state. One handler per command type keeps the rules
    /// for an action in one place.
    /// </summary>
    internal interface ICommandHandler<TCommand>
        where TCommand : struct, ICommand
    {
        /// <summary>Applies the command or returns why it is refused; never throws for invalid player or AI input.</summary>
        CommandResult Handle(in TCommand command, RunState state);
    }
}
