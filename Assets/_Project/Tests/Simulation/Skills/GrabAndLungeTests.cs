#nullable enable
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Movement;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Skills
{
    public sealed class GrabAndLungeTests
    {
        private const float Tolerance = 0.3f;

        [Test]
        public void Grab_OnASmallerDemon_HoldsItUntilTheHoldTimeEnds()
        {
            var scenario = new CombatScenario(TestContent.Blob with { SizeMeters = 2f });
            scenario.Attacker.AttachPart(TestContent.Arm);
            scenario.StartSkillAndReachActiveWindow(TestContent.GrabId);

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);
            long heldUntil = scenario.State.Tick + scenario.State.Config.TicksFor(TestContent.Grab.HoldSeconds) - 1;
            scenario.Submit(new MoveCommand(scenario.Target.Id, new Vector2(0f, 1f), sprint: false));
            scenario.Tick(1);

            scenario.Held.Should().ContainSingle();
            scenario.Held[0].Target.Should().Be(scenario.Target.Id);
            scenario.Held[0].UntilTick.Should().Be(heldUntil);
            scenario.Target.IsHeld(scenario.State.Tick).Should().BeTrue();
            scenario.Target.Intent.IsMoving.Should().BeFalse();
            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MoveCommandHandler.Held);
            scenario.DamageFrom(scenario.Attacker.Id).Should().BeEmpty();

            scenario.Tick(scenario.State.Config.TicksFor(TestContent.Grab.HoldSeconds));
            scenario.Submit(new MoveCommand(scenario.Target.Id, new Vector2(0f, 1f), sprint: false));
            scenario.Tick(1);

            scenario.Target.IsHeld(scenario.State.Tick).Should().BeFalse();
            scenario.Target.Intent.IsMoving.Should().BeTrue();
        }

        [Test]
        public void Grab_HeldDemon_FollowsTheHolderAndIsFreedWhenTheHoldEnds()
        {
            var scenario = new CombatScenario(TestContent.Blob with { SizeMeters = 2f });
            scenario.Attacker.AttachPart(TestContent.Arm);
            scenario.StartSkillAndReachActiveWindow(TestContent.GrabId);
            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(12);

            scenario.Target.HeldBy.Should().Be(scenario.Attacker.Id);
            for (int i = 0; i < 6; i++)
            {
                scenario.Submit(new MoveCommand(scenario.Attacker.Id, new Vector2(1f, 0f), sprint: false));
                scenario.Tick(1);
            }

            Vector2 anchor = HoldSystem.Anchor(scenario.Attacker, scenario.Target.HeldOffset);
            var holderAt = new Vector2(scenario.Attacker.Position.X, scenario.Attacker.Position.Z);
            var heldAt = new Vector2(scenario.Target.Position.X, scenario.Target.Position.Z);
            scenario.Attacker.Position.X.Should().BeGreaterThan(0.5f);
            heldAt.X.Should().BeApproximately(anchor.X, Tolerance);
            heldAt.Y.Should().BeApproximately(anchor.Y, Tolerance);
            Vector2.Distance(heldAt, holderAt).Should().BeApproximately(1.6f, Tolerance);

            scenario.Tick(scenario.State.Config.TicksFor(TestContent.Grab.HoldSeconds));

            scenario.Target.HeldBy.Should().Be(DemonId.None);
            scenario.Target.IsHeld(scenario.State.Tick).Should().BeFalse();
            scenario.Target.ExternalVelocity.Should().Be(Vector2.Zero);
        }

        [Test]
        public void Grab_OnAnEqualDemon_HoldsIt()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Arm);
            scenario.StartSkillAndReachActiveWindow(TestContent.GrabId);

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);

            scenario.Held.Should().ContainSingle();
            scenario.Target.IsHeld(scenario.State.Tick).Should().BeTrue();
            scenario.Target.HeldBy.Should().Be(scenario.Attacker.Id);
        }

        [Test]
        public void Grab_OnABiggerDemon_HoldsNothing()
        {
            var scenario = new CombatScenario(TestContent.Blob with { SizeMeters = 0.8f });
            scenario.Attacker.AttachPart(TestContent.Arm);
            scenario.Target.SetPose(new Vector3(0f, 0f, 1.5f), 0f);
            scenario.StartSkillAndReachActiveWindow(TestContent.GrabId);

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);

            scenario.Rejections.Should().BeEmpty();
            scenario.Held.Should().BeEmpty();
            scenario.Target.IsHeld(scenario.State.Tick).Should().BeFalse();
            scenario.SkillXp.Should().ContainSingle();
        }

        [Test]
        public void Lunge_WhenUsed_LeapsForwardUntilTheActiveWindowCloses()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Legs);
            scenario.Target.SetPose(new Vector3(0f, 0f, 12f), 0f);

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.LungeId));
            scenario.Tick(1);
            long activeUntil = scenario.Activations[0].ActiveUntilTick;
            while (scenario.State.Tick <= activeUntil + 1)
            {
                scenario.Tick(1);
            }

            scenario.Attacker.Position.Z.Should().BeApproximately(TestContent.Lunge.DashMeters, Tolerance);
            scenario.Attacker.ExternalVelocity.Should().Be(Vector2.Zero);
            scenario.Tick(5);
            scenario.Attacker.Position.Z.Should().BeApproximately(TestContent.Lunge.DashMeters, Tolerance);
        }

        [Test]
        public void Lunge_Hit_KnocksTheTargetBackAndStaggersIt()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Legs);
            scenario.StartSkillAndReachActiveWindow(TestContent.LungeId);
            float targetBefore = scenario.Target.Position.Z;

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);
            bool staggered = scenario.Target.IsStaggered(scenario.State.Tick);
            scenario.Tick(scenario.State.Config.TicksFor(TestContent.Tuning.KnockbackSeconds) + 1);

            staggered.Should().BeTrue();
            scenario.DamageFrom(scenario.Attacker.Id).Should().ContainSingle().Which.Amount.Should().BeApproximately(10f, 0.01f);
            (scenario.Target.Position.Z - targetBefore).Should().BeApproximately(TestContent.Lunge.KnockbackMeters, Tolerance);
            scenario.Target.ExternalVelocity.Should().Be(Vector2.Zero);
        }
    }
}
