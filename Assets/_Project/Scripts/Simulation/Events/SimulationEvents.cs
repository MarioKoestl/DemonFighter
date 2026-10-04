#nullable enable
using System;
using System.Collections.Generic;

namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// Event bus of one run. Publishing queues an event; the ticker flushes the queue at the end of each tick, so
    /// observers (Presentation, UI, audio) always see a consistent state and events in publication order across all
    /// types. Nothing inside the simulation subscribes; it only publishes (ARCHITECTURE, "Commands in, events out").
    /// </summary>
    public sealed class SimulationEvents
    {
        private readonly Dictionary<Type, IEventChannel> _channelsByType = new Dictionary<Type, IEventChannel>();
        private readonly List<IEventChannel> _channels = new List<IEventChannel>();
        private List<PendingEvent> _pending = new List<PendingEvent>();
        private List<PendingEvent> _flushing = new List<PendingEvent>();

        /// <summary>Events queued since the last flush.</summary>
        public int PendingCount => _pending.Count;

        /// <summary>Queues an event for delivery at the next flush; never delivers immediately.</summary>
        public void Publish<TEvent>(in TEvent evt)
            where TEvent : struct, ISimulationEvent
        {
            EventChannel<TEvent> channel = GetOrCreateChannel<TEvent>();
            int index = channel.Enqueue(in evt);
            _pending.Add(new PendingEvent(channel, index));
        }

        /// <summary>Delivers events of one type after each flush; dispose the handle to stop listening.</summary>
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : struct, ISimulationEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return GetOrCreateChannel<TEvent>().Subscribe(handler);
        }

        /// <summary>Delivers everything queued, in publication order; called by the ticker at the end of a step.</summary>
        public void Flush()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            List<PendingEvent> flushing = _pending;
            _pending = _flushing;
            _flushing = flushing;

            int channelCount = _channels.Count;
            for (int i = 0; i < channelCount; i++)
            {
                _channels[i].BeginFlush();
            }

            try
            {
                for (int i = 0; i < flushing.Count; i++)
                {
                    flushing[i].Channel.Deliver(flushing[i].Index);
                }
            }
            finally
            {
                flushing.Clear();
                for (int i = 0; i < channelCount; i++)
                {
                    _channels[i].EndFlush();
                }
            }
        }

        private EventChannel<TEvent> GetOrCreateChannel<TEvent>()
            where TEvent : struct, ISimulationEvent
        {
            if (_channelsByType.TryGetValue(typeof(TEvent), out IEventChannel existing))
            {
                return (EventChannel<TEvent>)existing;
            }

            var channel = new EventChannel<TEvent>();
            _channelsByType.Add(typeof(TEvent), channel);
            _channels.Add(channel);
            return channel;
        }

        private readonly struct PendingEvent
        {
            public PendingEvent(IEventChannel channel, int index)
            {
                Channel = channel;
                Index = index;
            }

            public IEventChannel Channel { get; }

            public int Index { get; }
        }
    }
}
