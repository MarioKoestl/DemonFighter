#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Movement
{
    public sealed class MovementTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Tick_WithMoveCommand_MovesTheDemonBySpeedTimesTickSeconds()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            ticker.Commands.Submit(new MoveCommand(demon.Id, new Vector2(0f, 1f), sprint: false));

            ticker.Tick();

            float expected = DemonBuilder.Blob.MoveSpeed * state.Config.TickSeconds;
            demon.Position.Z.Should().BeApproximately(expected, Tolerance);
            demon.Position.X.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void Tick_TwentyTicksWalkingEast_CoversTheWalkSpeedInMeters()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());

            for (int i = 0; i < 20; i++)
            {
                ticker.Commands.Submit(new MoveCommand(demon.Id, new Vector2(1f, 0f), sprint: false));
                ticker.Tick();
            }

            demon.Position.X.Should().BeApproximately(DemonBuilder.Blob.MoveSpeed, 0.001f);
        }

        [Test]
        public void Tick_Sprinting_MovesFasterByTheMultiplier()
        {
            RunState state = new RunStateBuilder().Build();
            Demon walker = new DemonBuilder().SpawnInto(state);
            Demon sprinter = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            ticker.Commands.Submit(new MoveCommand(walker.Id, new Vector2(0f, 1f), sprint: false));
            ticker.Commands.Submit(new MoveCommand(sprinter.Id, new Vector2(0f, 1f), sprint: true));

            ticker.Tick();

            float ratio = sprinter.Position.Z / walker.Position.Z;
            ratio.Should().BeApproximately(DemonBuilder.Blob.SprintMultiplier, Tolerance);
        }

        [Test]
        public void Tick_Moving_FacesTheMovementDirection()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().FacingYaw(0f).SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            ticker.Commands.Submit(new MoveCommand(demon.Id, new Vector2(1f, 0f), sprint: false));

            ticker.Tick();

            demon.Yaw.Should().BeApproximately(MathF.PI / 2f, Tolerance);
        }

        [Test]
        public void Tick_IntentPersists_UntilAZeroMoveCommandStopsTheDemon()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, new SimulationEvents());
            ticker.Commands.Submit(new MoveCommand(demon.Id, new Vector2(0f, 1f), sprint: false));
            ticker.Tick();
            ticker.Tick();
            float afterTwoTicks = demon.Position.Z;

            ticker.Commands.Submit(new MoveCommand(demon.Id, Vector2.Zero, sprint: false));
            ticker.Tick();

            afterTwoTicks.Should().BeApproximately(2f * DemonBuilder.Blob.MoveSpeed * state.Config.TickSeconds, Tolerance);
            demon.Position.Z.Should().BeApproximately(afterTwoTicks, Tolerance);
            demon.Intent.IsMoving.Should().BeFalse();
        }

        [Test]
        public void Tick_DemonWithABody_KeepsTheIntentButIsNotMovedByTheSimulation()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().SpawnInto(state);
            demon.AttachBody();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            ticker.Commands.Submit(new MoveCommand(demon.Id, new Vector2(0f, 1f), sprint: false));

            ticker.Tick();

            demon.Intent.IsMoving.Should().BeTrue();
            demon.Position.Z.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void AttachBody_Twice_Throws()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());
            demon.AttachBody();

            Action act = () => demon.AttachBody();

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Tick_MoveCommandForADeadDemon_IsRejected()
        {
            RunState state = new RunStateBuilder().Build();
            var events = new SimulationEvents();
            var rejections = new List<CommandRejected>();
            events.Subscribe<CommandRejected>(rejections.Add);
            Demon demon = new DemonBuilder().SpawnInto(state);
            var ticker = new SimulationTicker(state, events);
            demon.Body.Core.ApplyDamage(1000f);
            ticker.Commands.Submit(new MoveCommand(demon.Id, new Vector2(0f, 1f), sprint: false));

            ticker.Tick();

            rejections.Should().ContainSingle();
            rejections[0].Reason.Should().Be(MoveCommandHandler.ActorDead);
            demon.Position.Z.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void Tick_MoveCommandForUnknownActor_IsRejectedWithAnEvent()
        {
            RunState state = new RunStateBuilder().Build();
            var events = new SimulationEvents();
            var rejections = new List<CommandRejected>();
            events.Subscribe<CommandRejected>(rejections.Add);
            var ticker = new SimulationTicker(state, events);
            ticker.Commands.Submit(new MoveCommand(new DemonId(99), new Vector2(0f, 1f), sprint: false));

            ticker.Tick();

            rejections.Should().ContainSingle();
            rejections[0].Reason.Should().Be(MoveCommandHandler.UnknownActor);
            rejections[0].CommandName.Should().Be(nameof(MoveCommand));
        }
    }
}
