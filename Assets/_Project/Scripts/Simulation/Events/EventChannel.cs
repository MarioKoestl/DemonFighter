#nullable enable
using System;
using System.Collections.Generic;

namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// Queue and subscriber list for one event type. Double-buffered, so a handler that publishes during a flush
    /// queues for the next one, and tolerant of handlers that unsubscribe while being invoked.
    /// </summary>
    internal sealed class EventChannel<TEvent> : IEventChannel
        where TEvent : struct, ISimulationEvent
    {
        private readonly List<Action<TEvent>> _handlers = new List<Action<TEvent>>();
        private readonly List<Action<TEvent>> _removedWhileDelivering = new List<Action<TEvent>>();
        private List<TEvent> _queued = new List<TEvent>();
        private List<TEvent> _delivering = new List<TEvent>();
        private bool _isDelivering;

        /// <summary>Queues an event and returns its index within the next flush.</summary>
        public int Enqueue(in TEvent evt)
        {
            _queued.Add(evt);
            return _queued.Count - 1;
        }

        /// <summary>Adds a handler; disposing the result removes it again.</summary>
        public IDisposable Subscribe(Action<TEvent> handler)
        {
            _handlers.Add(handler);
            return new EventSubscription<TEvent>(this, handler);
        }

        /// <summary>Removes a handler now, or after the running flush if it is being delivered to.</summary>
        public void Unsubscribe(Action<TEvent> handler)
        {
            if (_isDelivering)
            {
                _removedWhileDelivering.Add(handler);
                return;
            }

            _handlers.Remove(handler);
        }

        /// <inheritdoc />
        public void BeginFlush()
        {
            List<TEvent> swap = _delivering;
            _delivering = _queued;
            _queued = swap;
            _isDelivering = true;
        }

        /// <inheritdoc />
        public void Deliver(int index)
        {
            TEvent evt = _delivering[index];
            int count = _handlers.Count;
            for (int i = 0; i < count; i++)
            {
                Action<TEvent> handler = _handlers[i];
                if (_removedWhileDelivering.Contains(handler))
                {
                    continue;
                }

                handler(evt);
            }
        }

        /// <inheritdoc />
        public void EndFlush()
        {
            _delivering.Clear();
            _isDelivering = false;
            for (int i = 0; i < _removedWhileDelivering.Count; i++)
            {
                _handlers.Remove(_removedWhileDelivering[i]);
            }

            _removedWhileDelivering.Clear();
        }
    }
}
