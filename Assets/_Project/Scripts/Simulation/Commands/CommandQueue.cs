#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Collects the commands of one tick from input and AI and applies them in submission order at the start of the
    /// next step (ARCHITECTURE, "Tick", stage 1). Rejections leave as <see cref="CommandRejected"/> events.
    /// </summary>
    public sealed class CommandQueue
    {
        private readonly Dictionary<Type, ICommandChannel> _channelsByType = new Dictionary<Type, ICommandChannel>();
        private readonly List<ICommandChannel> _channels = new List<ICommandChannel>();
        private List<PendingCommand> _pending = new List<PendingCommand>();
        private List<PendingCommand> _applying = new List<PendingCommand>();

        /// <summary>Commands waiting for the next pass.</summary>
        public int PendingCount => _pending.Count;

        /// <summary>Queues a command for the next pass; never applies it immediately.</summary>
        public void Submit<TCommand>(in TCommand command)
            where TCommand : struct, ICommand
        {
            CommandChannel<TCommand> channel = GetOrCreateChannel<TCommand>();
            int index = channel.Enqueue(in command);
            _pending.Add(new PendingCommand(channel, index));
        }

        /// <summary>Installs the handler for a command type; the ticker does this once per run.</summary>
        internal void RegisterHandler<TCommand>(ICommandHandler<TCommand> handler)
            where TCommand : struct, ICommand
        {
            GetOrCreateChannel<TCommand>().SetHandler(handler);
        }

        /// <summary>Applies everything queued, in submission order; a command type without a handler is a wiring bug.</summary>
        internal void ApplyAll(RunState state, SimulationEvents events)
        {
            if (_pending.Count == 0)
            {
                return;
            }

            List<PendingCommand> applying = _pending;
            _pending = _applying;
            _applying = applying;

            int channelCount = _channels.Count;
            for (int i = 0; i < channelCount; i++)
            {
                _channels[i].BeginApply();
            }

            try
            {
                for (int i = 0; i < applying.Count; i++)
                {
                    applying[i].Channel.Apply(applying[i].Index, state, events);
                }
            }
            finally
            {
                applying.Clear();
                for (int i = 0; i < channelCount; i++)
                {
                    _channels[i].EndApply();
                }
            }
        }

        private CommandChannel<TCommand> GetOrCreateChannel<TCommand>()
            where TCommand : struct, ICommand
        {
            if (_channelsByType.TryGetValue(typeof(TCommand), out ICommandChannel existing))
            {
                return (CommandChannel<TCommand>)existing;
            }

            var channel = new CommandChannel<TCommand>();
            _channelsByType.Add(typeof(TCommand), channel);
            _channels.Add(channel);
            return channel;
        }

        private readonly struct PendingCommand
        {
            public PendingCommand(ICommandChannel channel, int index)
            {
                Channel = channel;
                Index = index;
            }

            public ICommandChannel Channel { get; }

            public int Index { get; }
        }
    }
}
