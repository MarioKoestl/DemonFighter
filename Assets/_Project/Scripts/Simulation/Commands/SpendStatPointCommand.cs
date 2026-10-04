#nullable enable
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>Puts one unspent stat point into a base stat; the Stats tab sends it.</summary>
    public readonly struct SpendStatPointCommand : ICommand
    {
        public SpendStatPointCommand(DemonId actor, StatId stat)
        {
            Actor = actor;
            Stat = stat;
        }

        /// <inheritdoc />
        public DemonId Actor { get; }

        /// <summary>The stat to raise by one.</summary>
        public StatId Stat { get; }
    }
}
