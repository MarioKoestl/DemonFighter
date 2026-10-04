#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Anatomy
{
    public sealed class BodySocketTests
    {
        private const float Tolerance = 0.0001f;
        private static readonly BodyRules Rules = new BodyRules(0.5f, 0.15f);

        [Test]
        public void Attach_Arm_TakesOneOfTwoLimbSlots()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            int before = body.FreeSlots(SocketKind.Limb);

            body.Attach(TestContent.Arm);

            before.Should().Be(2);
            body.FreeSlots(SocketKind.Limb).Should().Be(1);
            body.CanAttach(TestContent.Arm, out _).Should().BeTrue();
        }

        [Test]
        public void Attach_ThirdArm_IsRefusedAndNamesTheSocket()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            body.Attach(TestContent.Arm);
            body.Attach(TestContent.Arm);

            bool canAttach = body.CanAttach(TestContent.Arm, out string reason);
            Action act = () => body.Attach(TestContent.Arm);

            canAttach.Should().BeFalse();
            reason.Should().Contain("Limb");
            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Attach_SecondHide_IsRefused()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            body.Attach(TestContent.ThickHide);

            body.CanAttach(TestContent.ThickHide, out _).Should().BeFalse();
        }

        [Test]
        public void Attach_AfterLosingAnArm_TheSlotStaysTaken()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart first = body.Attach(TestContent.Arm);
            body.Attach(TestContent.Arm);

            first.ApplyDamage(1000f);

            body.FreeSlots(SocketKind.Limb).Should().Be(0);
            body.CanAttach(TestContent.Arm, out _).Should().BeFalse();
        }

        [Test]
        public void Upgrade_Arm_RaisesHpStatBonusAndInvestment()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart arm = body.Attach(TestContent.Arm);

            body.Upgrade(arm);

            arm.UpgradeLevel.Should().Be(1);
            arm.MaxHp.Should().BeApproximately(23f, Tolerance);
            arm.Hp.Should().BeApproximately(23f, Tolerance);
            body.StatBonus(StatIds.Strength).Should().Be(2);
            body.InvestmentPoints.Should().Be(2);
        }

        [Test]
        public void Upgrade_BeyondTheMaximum_Throws()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart arm = body.Attach(TestContent.Arm);
            for (int i = 0; i < TestContent.Arm.MaxUpgrade; i++)
            {
                body.Upgrade(arm);
            }

            Action upgradeArm = () => body.Upgrade(arm);
            Action upgradeCore = () => body.Upgrade(body.Core);

            upgradeArm.Should().Throw<InvalidOperationException>();
            upgradeCore.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Upgrade_LostPart_Throws()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart arm = body.Attach(TestContent.Arm);
            arm.ApplyDamage(1000f);

            Action act = () => body.Upgrade(arm);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Regrow_LostArm_RestoresFullHealthAndItsSkill()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart arm = body.Attach(TestContent.Arm);
            arm.StartBleeding(3f, 2f, DamageType.Cut);
            arm.ApplyDamage(1000f);
            bool grantedWhileLost = body.Grants(TestContent.ClawId);

            body.Regrow(arm);

            grantedWhileLost.Should().BeFalse();
            arm.IsLost.Should().BeFalse();
            arm.Hp.Should().BeApproximately(20f, Tolerance);
            arm.Condition.Should().Be(PartCondition.Healthy);
            arm.IsBleeding.Should().BeFalse();
            body.Grants(TestContent.ClawId).Should().BeTrue();
        }

        [Test]
        public void Regrow_IntactPart_Throws()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart arm = body.Attach(TestContent.Arm);

            Action act = () => body.Regrow(arm);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Grants_WithTwoArms_LosingOneKeepsClaw()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart first = body.Attach(TestContent.Arm);
            BodyPart second = body.Attach(TestContent.Arm);

            first.ApplyDamage(1000f);
            bool afterOne = body.Grants(TestContent.ClawId);
            second.ApplyDamage(1000f);

            afterOne.Should().BeTrue();
            body.Grants(TestContent.ClawId).Should().BeFalse();
        }

        [Test]
        public void StatBonus_FromALostPart_StopsCounting()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart arm = body.Attach(TestContent.Arm);

            arm.ApplyDamage(1000f);

            body.StatBonus(StatIds.Strength).Should().Be(0);
            body.InvestmentPoints.Should().Be(1);
        }

        [Test]
        public void MoveSpeedBonus_WithLegs_IsTheLegsValueUntilTheyAreLost()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart legs = body.Attach(TestContent.Legs);
            float withLegs = body.MoveSpeedBonus;

            legs.ApplyDamage(1000f);

            withLegs.Should().BeApproximately(0.5f, Tolerance);
            body.MoveSpeedBonus.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void SkillDamageBonus_FromUpgradedJaws_GrowsPerLevel()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart jaws = body.Attach(TestContent.Jaws);
            body.Upgrade(jaws);

            float bonus = body.SkillDamageBonus(TestContent.BiteId);

            bonus.Should().BeApproximately(0.5f, Tolerance);
            body.PerceptionBonus.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void RescaleHp_KeepsTheUpgradeBonus()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart arm = body.Attach(TestContent.Arm);
            body.Upgrade(arm);

            body.RescaleHp(2f);

            arm.MaxHp.Should().BeApproximately(46f, Tolerance);
        }

        [Test]
        public void InvestmentPoints_CountsPartsUpgradesAndLostParts()
        {
            var body = new Body(TestContent.Core, 1f, Rules);
            BodyPart arm = body.Attach(TestContent.Arm);
            BodyPart legs = body.Attach(TestContent.Legs);
            body.Upgrade(arm);
            body.Upgrade(arm);
            legs.ApplyDamage(1000f);

            int points = body.InvestmentPoints;

            points.Should().Be(4);
        }
    }
}
