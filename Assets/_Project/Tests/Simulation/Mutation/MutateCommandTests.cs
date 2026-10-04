#nullable enable
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Mutation
{
    public sealed class MutateCommandTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Tick_AttachArmWithBiomass_SpendsPaysAndTransforms()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(100f);

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Tick(1);

            run.Rejections.Should().BeEmpty();
            run.Player.Body.Parts.Should().HaveCount(2);
            run.Player.Biomass.Should().BeApproximately(70f, Tolerance);
            run.Started.Should().ContainSingle();
            run.Started[0].Kind.Should().Be(MutationKind.Attach);
            run.Started[0].PartId.Should().Be(TestContent.ArmId);
            run.Started[0].Cost.Should().BeApproximately(30f, Tolerance);
            run.Started[0].UntilTick.Should().Be(run.State.Config.TicksFor(TestContent.Tuning.TransformationSeconds));
            run.Player.IsTransforming(run.State.Tick).Should().BeTrue();
        }

        [Test]
        public void Tick_WhileTransforming_MovesNotAndTakesNoDamageUntilItEnds()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(100f);
            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Tick(1);

            run.Submit(new MoveCommand(run.Player.Id, new Vector2(0f, 1f), sprint: false));
            run.Tick(1);
            run.Ticker.Damage.ApplyDamage(run.Player, run.Player.Body.Core, 20f, DamageType.Pierce, DemonId.None);
            float hpWhileTransforming = run.Player.Body.Core.Hp;
            run.Tick(run.State.Config.TicksFor(TestContent.Tuning.TransformationSeconds));

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MoveCommandHandler.Transforming);
            hpWhileTransforming.Should().BeApproximately(60f, Tolerance);
            run.Player.IsTransforming(run.State.Tick).Should().BeFalse();
            run.Completed.Should().ContainSingle().Which.Demon.Should().Be(run.Player.Id);
            run.Submit(new MoveCommand(run.Player.Id, new Vector2(0f, 1f), sprint: false));
            run.Tick(1);
            run.Player.Intent.IsMoving.Should().BeTrue();
        }

        [Test]
        public void Tick_AttachInCombat_IsAllowed()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(100f);
            run.Ticker.Damage.ApplyDamage(run.Player, run.Player.Body.Core, 5f, DamageType.Pierce, run.Other.Id);

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Tick(1);

            run.Rejections.Should().BeEmpty();
            run.Started.Should().ContainSingle();
        }

        [Test]
        public void Tick_AttachWithoutBiomass_IsRejected()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(10f);

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MutationRules.NotEnoughBiomass);
            run.Player.Body.Parts.Should().HaveCount(1);
            run.Player.Biomass.Should().BeApproximately(10f, Tolerance);
        }

        [Test]
        public void Tick_SecondArm_CostsMoreAndNeedsTheRepeatLevel()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(200f);
            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Tick(1);
            run.Tick(run.State.Config.TicksFor(TestContent.Tuning.TransformationSeconds));

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Tick(1);
            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MutationRules.LevelTooLow);

            run.Player.GainXp(400f, TestContent.Tuning);
            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Tick(1);

            run.Started.Should().HaveCount(2);
            run.Started[1].Cost.Should().BeApproximately(45f, Tolerance);
            run.Player.Biomass.Should().BeApproximately(125f, Tolerance);
        }

        [Test]
        public void Tick_LockedPart_IsRejectedUntilUnlocked()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(200f);
            run.Player.GainXp(2000f, TestContent.Tuning);
            run.Player.AttachPart(TestContent.Legs);

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.TailId));
            run.Tick(1);
            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MutationRules.Locked);

            run.Player.UnlockPart(TestContent.TailId);
            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.TailId));
            run.Tick(1);

            run.Started.Should().ContainSingle();
        }

        [Test]
        public void Tick_PartNeedingLegs_IsRejectedWithoutThem()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(200f);
            run.Player.GainXp(2000f, TestContent.Tuning);
            run.Player.UnlockPart(TestContent.TailId);

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.TailId));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MutationRules.MissingPart);
        }

        [Test]
        public void Tick_AttachIntoAFullSocket_IsRejected()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(500f);
            run.Player.AttachPart(TestContent.ThickHide);

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.HideId));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MutationRules.NoSocket);
        }

        [Test]
        public void Tick_UpgradeArm_CostsHalfTheBasePerLevelAndNeedsTheLevel()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(200f);
            BodyPart arm = run.Player.AttachPart(TestContent.Arm);

            run.Submit(MutateCommand.Upgrade(run.Player.Id, arm.Index));
            run.Tick(1);
            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MutationRules.LevelTooLow);

            run.Player.GainXp(100f, TestContent.Tuning);
            run.Submit(MutateCommand.Upgrade(run.Player.Id, arm.Index));
            run.Tick(1);

            run.Started.Should().ContainSingle().Which.Kind.Should().Be(MutationKind.Upgrade);
            arm.UpgradeLevel.Should().Be(1);
            run.Player.Biomass.Should().BeApproximately(185f, Tolerance);
        }

        [Test]
        public void Tick_RegrowLostArm_CostsHalfAndRestoresIt()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(100f);
            BodyPart arm = run.Player.AttachPart(TestContent.Arm);
            arm.ApplyDamage(1000f);

            run.Submit(MutateCommand.Regrow(run.Player.Id, arm.Index));
            run.Tick(1);

            run.Started.Should().ContainSingle().Which.Kind.Should().Be(MutationKind.Regrow);
            arm.IsLost.Should().BeFalse();
            run.Player.Biomass.Should().BeApproximately(85f, Tolerance);
        }

        [Test]
        public void Tick_RegrowIntactArm_IsRejected()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(100f);
            BodyPart arm = run.Player.AttachPart(TestContent.Arm);

            run.Submit(MutateCommand.Regrow(run.Player.Id, arm.Index));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MutationRules.NotLost);
        }

        [Test]
        public void Tick_UnknownPartOrIndex_IsRejected()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(100f);

            run.Submit(MutateCommand.Attach(run.Player.Id, "part.nope"));
            run.Submit(MutateCommand.Upgrade(run.Player.Id, 7));
            run.Tick(1);

            run.Rejections.Should().HaveCount(2);
            run.Rejections[0].Reason.Should().Be(MutateCommandHandler.UnknownPart);
            run.Rejections[1].Reason.Should().Be(MutateCommandHandler.UnknownPart);
        }

        [Test]
        public void Tick_SecondMutationWhileTransforming_IsRejected()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(200f);
            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Tick(1);

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.LegsId));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(MutationRules.Transforming);
        }

        [Test]
        public void Tick_TwoMutationsInOneTick_BothApplyAndShareOneTransformation()
        {
            var run = new MutationRun();
            run.Player.GainBiomass(100f);

            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.ArmId));
            run.Submit(MutateCommand.Attach(run.Player.Id, TestContent.EyesId));
            run.Tick(1);

            run.Rejections.Should().BeEmpty();
            run.Started.Should().HaveCount(2);
            run.Player.Body.Parts.Should().HaveCount(3);
            run.Player.Biomass.Should().BeApproximately(100f - 30f - TestContent.Eyes.BiomassCost, Tolerance);
            run.Started[1].UntilTick.Should().Be(run.Started[0].UntilTick);

            run.Tick(run.State.Config.TicksFor(TestContent.Tuning.TransformationSeconds));

            run.Completed.Should().ContainSingle();
            run.Player.IsTransforming(run.State.Tick).Should().BeFalse();
        }

        private sealed class MutationRun
        {
            public MutationRun()
            {
                State = new RunStateBuilder().Build();
                Events = new SimulationEvents();
                Ticker = new SimulationTicker(State, Events);
                Player = new DemonBuilder().AsPlayer().At(0f, 0f).SpawnInto(State);
                Other = new DemonBuilder().At(0f, 3f).SpawnInto(State);
                Events.Subscribe<CommandRejected>(Rejections.Add);
                Events.Subscribe<MutationStarted>(Started.Add);
                Events.Subscribe<MutationCompleted>(Completed.Add);
            }

            public RunState State { get; }

            public SimulationEvents Events { get; }

            public SimulationTicker Ticker { get; }

            public Demon Player { get; }

            public Demon Other { get; }

            public List<CommandRejected> Rejections { get; } = new List<CommandRejected>();

            public List<MutationStarted> Started { get; } = new List<MutationStarted>();

            public List<MutationCompleted> Completed { get; } = new List<MutationCompleted>();

            public void Submit<TCommand>(in TCommand command)
                where TCommand : struct, ICommand
            {
                Ticker.Commands.Submit(in command);
            }

            public void Tick(int times)
            {
                for (int i = 0; i < times; i++)
                {
                    Ticker.Tick();
                }
            }
        }
    }
}
