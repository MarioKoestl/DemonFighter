#nullable enable
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Simulation.Events;
using NSubstitute;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Events
{
    public sealed class SimulationEventsTests
    {
        [Test]
        public void Publish_BeforeFlush_DoesNotInvokeSubscriber()
        {
            var events = new SimulationEvents();
            int received = 0;
            events.Subscribe<TestEvent>(evt => received++);

            events.Publish(new TestEvent(1));

            received.Should().Be(0);
            events.PendingCount.Should().Be(1);
        }

        [Test]
        public void Flush_WithPublishedEvent_InvokesSubscriberWithPayload()
        {
            var events = new SimulationEvents();
            int received = 0;
            events.Subscribe<TestEvent>(evt => received = evt.Value);
            events.Publish(new TestEvent(7));

            events.Flush();

            received.Should().Be(7);
            events.PendingCount.Should().Be(0);
        }

        [Test]
        public void Flush_WithSubstituteHandler_InvokesItExactlyOnce()
        {
            var events = new SimulationEvents();
            Action<TestEvent> handler = Substitute.For<Action<TestEvent>>();
            events.Subscribe(handler);
            events.Publish(new TestEvent(7));

            events.Flush();

            handler.Received(1).Invoke(Arg.Is<TestEvent>(evt => evt.Value == 7));
        }

        [Test]
        public void Flush_WithEventsOfTwoTypes_PreservesPublicationOrder()
        {
            var events = new SimulationEvents();
            var order = new List<string>();
            events.Subscribe<TestEvent>(evt => order.Add("test " + evt.Value));
            events.Subscribe<OtherTestEvent>(evt => order.Add("other " + evt.Value));
            events.Publish(new TestEvent(1));
            events.Publish(new OtherTestEvent(2));
            events.Publish(new TestEvent(3));

            events.Flush();

            order.Should().Equal("test 1", "other 2", "test 3");
        }

        [Test]
        public void Flush_Twice_DeliversEachEventOnce()
        {
            var events = new SimulationEvents();
            int received = 0;
            events.Subscribe<TestEvent>(evt => received++);
            events.Publish(new TestEvent(1));

            events.Flush();
            events.Flush();

            received.Should().Be(1);
        }

        [Test]
        public void Dispose_Subscription_StopsDelivery()
        {
            var events = new SimulationEvents();
            int received = 0;
            IDisposable subscription = events.Subscribe<TestEvent>(evt => received++);
            events.Publish(new TestEvent(1));

            subscription.Dispose();
            events.Flush();

            received.Should().Be(0);
        }

        [Test]
        public void Flush_HandlerUnsubscribesItself_StopsAfterTheCurrentEvent()
        {
            var events = new SimulationEvents();
            int received = 0;
            IDisposable? subscription = null;
            subscription = events.Subscribe<TestEvent>(evt =>
            {
                received++;
                subscription!.Dispose();
            });
            events.Publish(new TestEvent(1));
            events.Publish(new TestEvent(2));

            Action act = () => events.Flush();

            act.Should().NotThrow();
            received.Should().Be(1);
        }

        [Test]
        public void Flush_HandlerPublishesEvent_QueuesItForTheNextFlush()
        {
            var events = new SimulationEvents();
            var received = new List<int>();
            events.Subscribe<TestEvent>(evt =>
            {
                received.Add(evt.Value);
                if (evt.Value == 1)
                {
                    events.Publish(new TestEvent(2));
                }
            });
            events.Publish(new TestEvent(1));

            events.Flush();
            List<int> afterFirstFlush = new List<int>(received);
            events.Flush();

            afterFirstFlush.Should().Equal(1);
            received.Should().Equal(1, 2);
        }

        [Test]
        public void Subscribe_NullHandler_Throws()
        {
            var events = new SimulationEvents();

            Action act = () => events.Subscribe<TestEvent>(null!);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
