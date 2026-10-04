#nullable enable
using System;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
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
            demon.AttachPart(TestContent.Legs);

            demon.SetIntent(new MovementIntent(new Vector2(1f, 0f), sprint: true));

            float expected = DemonBuilder.Blob.MoveSpeed * DemonBuilder.Blob.SprintMultiplier * (1f + TestContent.Legs.MoveSpeedBonus);
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
        public void SizeMeters_AtSpawn_ComesFromTheSpec()
        {
            Demon demon = new DemonBuilder().WithSpec(DemonBuilder.Elder).SpawnInto(new RunStateBuilder().Build());

            float size = demon.SizeMeters;

            size.Should().BeApproximately(15f, Tolerance);
            demon.Tier.Should().Be(6);
        }

        [Test]
        public void Constructor_Blob_StartsAliveWithFullCoreAndStamina()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());

            bool alive = demon.IsAlive;

            alive.Should().BeTrue();
            demon.Body.Core.Hp.Should().BeApproximately(60f, Tolerance);
            demon.Stamina.Should().BeApproximately(100f, Tolerance);
            demon.Level.Should().Be(1);
            demon.Biomass.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void Constructor_Elder_AppliesStartingStatsToBodyAndDamage()
        {
            Demon elder = new DemonBuilder().WithSpec(DemonBuilder.Elder).SpawnInto(new RunStateBuilder().Build());

            float coreHp = elder.Body.Core.MaxHp;

            coreHp.Should().BeApproximately(60f * (1f + 20f * 10f / 60f), 0.01f);
            elder.Derived.DamageMultiplier.Should().BeApproximately(1.5f, Tolerance);
            elder.Stats.Get(StatIds.Constitution).Should().Be(20);
        }

        [Test]
        public void MaxSpeed_WithAgility_IsScaledByTheDerivedMultiplier()
        {
            DemonSpec quick = DemonBuilder.Blob with { StartingStats = new[] { new StatValue(StatIds.Agility, 10) } };
            Demon demon = new DemonBuilder().WithSpec(quick).SpawnInto(new RunStateBuilder().Build());

            float speed = demon.MaxSpeed;

            speed.Should().BeApproximately(4f * 1.3f, Tolerance);
        }

        [Test]
        public void GainXp_EnoughForOneLevel_GrantsStatPoints()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());

            int levels = demon.GainXp(120f, TestContent.Tuning);

            levels.Should().Be(1);
            demon.Level.Should().Be(2);
            demon.Xp.Should().BeApproximately(20f, Tolerance);
            demon.Stats.UnspentPoints.Should().Be(3);
        }

        [Test]
        public void TrySpendStamina_MoreThanAvailable_IsFalseAndKeepsStamina()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());

            bool spent = demon.TrySpendStamina(150f);

            spent.Should().BeFalse();
            demon.Stamina.Should().BeApproximately(100f, Tolerance);
        }

        [Test]
        public void RecomputeDerived_AfterConstitutionPoint_RescalesTheCore()
        {
            Demon demon = new DemonBuilder().SpawnInto(new RunStateBuilder().Build());
            demon.Stats.GrantPoints(1);
            demon.Stats.TrySpendPoint(StatIds.Constitution);

            demon.RecomputeDerived(TestContent.Tuning);

            demon.Body.Core.MaxHp.Should().BeApproximately(70f, 0.01f);
            demon.Body.Core.Hp.Should().BeApproximately(70f, 0.01f);
        }
    }
}
