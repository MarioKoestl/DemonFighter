#nullable enable
using System;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Tests.Builders;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class DemonTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Velocity_StandingStill_IsZero()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());

            Vector3 velocity = demon.Velocity;

            velocity.Length().Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void Velocity_WalkingNorth_IsWalkSpeedAlongZ()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());

            demon.SetIntent(new MovementIntent(new Vector2(0f, 1f), sprint: false));

            demon.Velocity.Z.Should().BeApproximately(DemonBuilder.Blob.MoveSpeed, Tolerance);
            demon.Velocity.X.Should().BeApproximately(0f, Tolerance);
            demon.Velocity.Y.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void MaxSpeed_Sprinting_AppliesTheSprintMultiplier()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());

            demon.SetIntent(new MovementIntent(new Vector2(1f, 0f), sprint: true));

            float expected = DemonBuilder.Blob.MoveSpeed * DemonBuilder.Blob.SprintMultiplier;
            demon.MaxSpeed.Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void SetPose_FromTheView_OverwritesPositionAndYaw()
        {
            Demon demon = new DemonBuilder().At(1f, 2f).SpawnInto(new RunStateBuilder().Build());

            demon.SetPose(new Vector3(5f, 0.5f, 7f), 1.25f);

            demon.Position.Should().Be(new Vector3(5f, 0.5f, 7f));
            demon.Yaw.Should().BeApproximately(1.25f, Tolerance);
        }

        [Test]
        public void YawToward_East_IsQuarterTurnClockwise()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());

            float yaw = demon.YawToward(new Vector2(1f, 0f));

            yaw.Should().BeApproximately(MathF.PI / 2f, Tolerance);
        }

        [Test]
        public void YawToward_ZeroDirection_KeepsTheCurrentYaw()
        {
            Demon demon = new DemonBuilder().FacingYaw(0.7f).SpawnInto(new RunStateBuilder().Build());

            float yaw = demon.YawToward(Vector2.Zero);

            yaw.Should().BeApproximately(0.7f, Tolerance);
        }

        [Test]
        public void FacingDirection_YawZero_PointsNorth()
        {
            Demon demon = new DemonBuilder().FacingYaw(0f).SpawnInto(new RunStateBuilder().Build());

            Vector2 facing = demon.FacingDirection;

            facing.X.Should().BeApproximately(0f, Tolerance);
            facing.Y.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void SizeMeters_AtSpawn_ComesFromTheTemplate()
        {
            Demon demon = new DemonBuilder().WithTemplate(DemonBuilder.Elder).SpawnInto(new RunStateBuilder().Build());

            float size = demon.SizeMeters;

            size.Should().BeApproximately(15f, Tolerance);
            demon.Tier.Should().Be(6);
        }
    }
}
