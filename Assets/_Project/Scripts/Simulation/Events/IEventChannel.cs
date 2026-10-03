#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// Type-erased view of one <see cref="EventChannel{TEvent}"/>, so the bus can flush channels of every event type
    /// in one global publication order.
    /// </summary>
    internal interface IEventChannel
    {
        /// <summary>Makes the queued events the ones being delivered; later publishes queue for the next flush.</summary>
        void BeginFlush();

        /// <summary>Delivers the event at the given index of the current flush to every subscriber.</summary>
        void Deliver(int index);

        /// <summary>Forgets the delivered events and applies subscriptions removed during delivery.</summary>
        void EndFlush();
    }
}
