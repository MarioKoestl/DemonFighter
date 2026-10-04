#nullable enable
using System;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Outcome of validating a command. Invalid player or AI actions are rejected with a reason instead of throwing;
    /// exceptions are reserved for programming errors (CODING_GUIDELINES, "Error handling").
    /// </summary>
    public readonly struct CommandResult
    {
        /// <summary>The command was applied.</summary>
        public static readonly CommandResult Accepted = new CommandResult(true, null);

        private CommandResult(bool accepted, string? reason)
        {
            IsAccepted = accepted;
            Reason = reason;
        }

        /// <summary>True when the command changed the state.</summary>
        public bool IsAccepted { get; }

        /// <summary>Why the command was refused; null when accepted.</summary>
        public string? Reason { get; }

        /// <summary>Refuses a command with a reason the player or a log can show.</summary>
        public static CommandResult Rejected(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A rejection needs a reason.", nameof(reason));
            }

            return new CommandResult(false, reason);
        }
    }
}
