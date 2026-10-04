#nullable enable
namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Something outside the simulation that has commands for the next step: the player's input adapter today, a
    /// network client later. The runner asks every source right before it ticks.
    /// </summary>
    public interface ICommandSource
    {
        /// <summary>Submits this source's commands for the coming tick.</summary>
        void SubmitCommands(CommandQueue commands);
    }
}
