#nullable enable
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Type-erased view of one <see cref="CommandChannel{TCommand}"/>, so the queue can apply commands of every type
    /// in one global submission order.
    /// </summary>
    internal interface ICommandChannel
    {
        /// <summary>Makes the submitted commands the ones being applied; later submissions wait for the next pass.</summary>
        void BeginApply();

        /// <summary>Applies the command at the given index of the current pass and reports rejections as events.</summary>
        void Apply(int index, RunState state, SimulationEvents events);

        /// <summary>Forgets the applied commands.</summary>
        void EndApply();
    }
}
