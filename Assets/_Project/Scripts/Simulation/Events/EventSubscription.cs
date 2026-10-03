#nullable enable
using System;

namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// Handle returned by <see cref="SimulationEvents.Subscribe{TEvent}"/>; disposing it removes the handler once,
    /// disposing again does nothing.
    /// </summary>
    internal sealed class EventSubscription<TEvent> : IDisposable
        where TEvent : struct, ISimulationEvent
    {
        private EventChannel<TEvent>? _channel;
        private Action<TEvent>? _handler;

        public EventSubscription(EventChannel<TEvent> channel, Action<TEvent> handler)
        {
            _channel = channel;
            _handler = handler;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_channel == null || _handler == null)
            {
                return;
            }

            _channel.Unsubscribe(_handler);
            _channel = null;
            _handler = null;
        }
    }
}
