#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// A command was refused. The HUD can tell the player why, and tests and logs can see what the AI tried.
    /// </summary>
    public readonly struct CommandRejected : ISimulationEvent
    {
        public CommandRejected(DemonId actor, string commandName, string reason)
        {
            Actor = actor;
            CommandName = commandName;
            Reason = reason;
        }

        /// <summary>The demon whose command was refused.</summary>
        public DemonId Actor { get; }

        /// <summary>Type name of the refused command.</summary>
        public string CommandName { get; }

        /// <summary>The reason the handler gave.</summary>
        public string Reason { get; }
    }
}
