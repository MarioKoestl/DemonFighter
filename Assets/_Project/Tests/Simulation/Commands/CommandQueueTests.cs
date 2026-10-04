#nullable enable
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Commands
{
    public sealed class CommandQueueTests
    {
        private static readonly DemonId Actor = new DemonId(1);

        [Test]
        public void Submit_BeforeApply_DoesNotReachTheHandler()
        {
            var queue = new CommandQueue();
            var handler = new RecordingHandler();
            queue.RegisterHandler(handler);

            queue.Submit(new TestCommand(Actor, 1));

            handler.Seen.Should().BeEmpty();
            queue.PendingCount.Should().Be(1);
        }

        [Test]
        public void ApplyAll_ThreeCommands_ReachTheHandlerInSubmissionOrder()
        {
            var queue = new CommandQueue();
            var handler = new RecordingHandler();
            queue.RegisterHandler(handler);
            queue.Submit(new TestCommand(Actor, 1));
            queue.Submit(new TestCommand(Actor, 2));
            queue.Submit(new TestCommand(Actor, 3));

            queue.ApplyAll(new RunStateBuilder().Build(), new SimulationEvents());

            handler.Seen.Should().Equal(1, 2, 3);
            queue.PendingCount.Should().Be(0);
        }

        [Test]
        public void ApplyAll_Twice_AppliesEachCommandOnce()
        {
            var queue = new CommandQueue();
            var handler = new RecordingHandler();
            queue.RegisterHandler(handler);
            queue.Submit(new TestCommand(Actor, 1));
            RunState state = new RunStateBuilder().Build();
            var events = new SimulationEvents();

            queue.ApplyAll(state, events);
            queue.ApplyAll(state, events);

            handler.Seen.Should().Equal(1);
        }

        [Test]
        public void ApplyAll_HandlerRejects_PublishesCommandRejected()
        {
            var queue = new CommandQueue();
            queue.RegisterHandler(new RecordingHandler());
            var events = new SimulationEvents();
            var rejections = new List<CommandRejected>();
            events.Subscribe<CommandRejected>(rejections.Add);
            queue.Submit(new TestCommand(Actor, -1));

            queue.ApplyAll(new RunStateBuilder().Build(), events);
            events.Flush();

            rejections.Should().ContainSingle();
            rejections[0].Actor.Should().Be(Actor);
            rejections[0].CommandName.Should().Be(nameof(TestCommand));
            rejections[0].Reason.Should().Be(RecordingHandler.NegativeReason);
        }

        [Test]
        public void ApplyAll_WithoutHandler_Throws()
        {
            var queue = new CommandQueue();
            queue.Submit(new TestCommand(Actor, 1));

            Action act = () => queue.ApplyAll(new RunStateBuilder().Build(), new SimulationEvents());

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void RegisterHandler_Twice_Throws()
        {
            var queue = new CommandQueue();
            queue.RegisterHandler(new RecordingHandler());

            Action act = () => queue.RegisterHandler(new RecordingHandler());

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
