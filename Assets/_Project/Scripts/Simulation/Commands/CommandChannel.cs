#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Queue and handler for one command type. Commands stay unboxed in a typed list, so submitting allocates
    /// nothing in the tick path. Double-buffered like the event channels.
    /// </summary>
    internal sealed class CommandChannel<TCommand> : ICommandChannel
        where TCommand : struct, ICommand
    {
        private ICommandHandler<TCommand>? _handler;
        private List<TCommand> _submitted = new List<TCommand>();
        private List<TCommand> _applying = new List<TCommand>();

        /// <summary>Installs the single handler for this type; installing a second one is a wiring bug.</summary>
        public void SetHandler(ICommandHandler<TCommand> handler)
        {
            if (_handler != null)
            {
                throw new InvalidOperationException("A handler for " + typeof(TCommand).Name + " is already registered.");
            }

            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>Queues a command and returns its index within the next pass.</summary>
        public int Enqueue(in TCommand command)
        {
            _submitted.Add(command);
            return _submitted.Count - 1;
        }

        /// <inheritdoc />
        public void BeginApply()
        {
            List<TCommand> swap = _applying;
            _applying = _submitted;
            _submitted = swap;
        }

        /// <inheritdoc />
        public void Apply(int index, RunState state, SimulationEvents events)
        {
            if (_handler == null)
            {
                throw new InvalidOperationException("No handler registered for " + typeof(TCommand).Name + ".");
            }

            TCommand command = _applying[index];
            CommandResult result = _handler.Handle(in command, state);
            if (!result.IsAccepted)
            {
                events.Publish(new CommandRejected(command.Actor, typeof(TCommand).Name, result.Reason ?? string.Empty));
            }
        }

        /// <inheritdoc />
        public void EndApply()
        {
            _applying.Clear();
        }
    }
}
