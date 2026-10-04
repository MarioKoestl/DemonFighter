#nullable enable
using System;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Movement
{
    public sealed class FacingTests
    {
        [Test]
        public void Tick_MoveCommandWithFacingAndNoDirection_TurnsTheStandingDemon()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon demon = new DemonBuilder().At(0f, 0f).FacingYaw(0f).SpawnInto(state);

            ticker.Commands.Submit(new MoveCommand(demon.Id, Vector2.Zero, sprint: false, facing: new Vector2(1f, 0f)));
            ticker.Tick();

            demon.Yaw.Should().BeApproximately(MathF.PI / 2f, 0.001f);
            demon.Position.Should().Be(Vector3.Zero);
            demon.Intent.HasFacing.Should().BeTrue();
        }

        [Test]
        public void Tick_MoveCommandWithFacingAndDirection_MovesOneWayWhileFacingAnother()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon demon = new DemonBuilder().At(0f, 0f).FacingYaw(0f).SpawnInto(state);

            ticker.Commands.Submit(new MoveCommand(demon.Id, new Vector2(0f, 1f), sprint: false, facing: new Vector2(0f, -1f)));
            ticker.Tick();

            demon.Position.Z.Should().BeGreaterThan(0f);
            MathF.Abs(demon.Yaw).Should().BeApproximately(MathF.PI, 0.001f);
        }

        [Test]
        public void Constructor_LongFacing_IsNormalized()
        {
            var intent = new MovementIntent(Vector2.Zero, sprint: false, facing: new Vector2(0f, 5f));

            intent.Facing.Should().Be(new Vector2(0f, 1f));
            intent.IsMoving.Should().BeFalse();
        }
    }
}
