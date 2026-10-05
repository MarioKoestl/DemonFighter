#nullable enable
using DemonFighter.Simulation.Commands;

namespace DemonFighter.Simulation.Playtest
{
    /// <summary>Switches test mode (D-089) on or off for one demon; the run sends it for the player from the settings.</summary>
    public readonly struct SetTestModeCommand : ICommand
    {
        public SetTestModeCommand(DemonId actor, bool enabled)
        {
            Actor = actor;
            Enabled = enabled;
        }

        /// <inheritdoc />
        public DemonId Actor { get; }

        /// <summary>True to make the demon invulnerable and keep its Biomass and stat points at 1000.</summary>
        public bool Enabled { get; }
    }
}
