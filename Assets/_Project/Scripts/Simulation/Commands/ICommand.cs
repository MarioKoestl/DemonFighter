#nullable enable
namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Everything that changes the simulation enters as a command naming the demon that acts (ARCHITECTURE,
    /// "Commands in, events out"). Player input and AI produce the same commands; the simulation cannot tell them apart.
    /// </summary>
    public interface ICommand
    {
        /// <summary>The demon this command is about.</summary>
        DemonId Actor { get; }
    }
}
