#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Anatomy
{
    public sealed class BodyTests
    {
        private const float Tolerance = 0.0001f;
        private const float WoundedThreshold = 0.5f;

        [Test]
        public void Constructor_FromCore_HasOnePartWithScaledHp()
        {
            var body = new Body(TestContent.Core, 1.5f, WoundedThreshold);

            int parts = body.Parts.Count;

            parts.Should().Be(1);
            body.Core.MaxHp.Should().BeApproximately(90f, Tolerance);
            body.Core.Index.Should().Be(0);
            body.TotalMaxHp.Should().BeApproximately(90f, Tolerance);
        }

        [Test]
        public void Constructor_FromALimb_Throws()
        {
            Action act = () => _ = new Body(TestContent.Arm, 1f, WoundedThreshold);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void ApplyDamage_BelowTheThreshold_IsWounded()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);

            bool lost = body.Core.ApplyDamage(35f);

            lost.Should().BeFalse();
            body.Core.Condition.Should().Be(PartCondition.Wounded);
            body.Core.Hp.Should().BeApproximately(25f, Tolerance);
        }

        [Test]
        public void ApplyDamage_ToZero_LosesThePartAndDestroysTheCore()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);

            bool lost = body.Core.ApplyDamage(60f);

            lost.Should().BeTrue();
            body.Core.Condition.Should().Be(PartCondition.Lost);
            body.IsCoreDestroyed.Should().BeTrue();
            body.TotalMaxHp.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void ApplyDamage_AlreadyLost_ReportsNothingNew()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);
            body.Core.ApplyDamage(100f);

            bool lostAgain = body.Core.ApplyDamage(10f);

            lostAgain.Should().BeFalse();
        }

        [Test]
        public void Heal_LostPart_StaysAtZero()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);
            body.Core.ApplyDamage(60f);

            body.Core.Heal(30f);

            body.Core.Hp.Should().BeApproximately(0f, Tolerance);
            body.Core.IsLost.Should().BeTrue();
        }

        [Test]
        public void Heal_WoundedPart_CapsAtMax()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);
            body.Core.ApplyDamage(10f);

            body.Core.Heal(50f);

            body.Core.Hp.Should().BeApproximately(60f, Tolerance);
            body.Core.Condition.Should().Be(PartCondition.Healthy);
        }

        [Test]
        public void AddPart_Arm_GetsTheNextIndex()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);

            BodyPart arm = body.AddPart(TestContent.Arm, 1f);

            arm.Index.Should().Be(1);
            body.GetPart(1).Should().BeSameAs(arm);
            body.HasPart(2).Should().BeFalse();
        }

        [Test]
        public void AddPart_SecondCore_Throws()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);

            Action act = () => body.AddPart(TestContent.Core, 1f);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void EffectiveDefense_WithIntactHide_ProtectsTheCore()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);
            body.AddPart(TestContent.ThickHide, 1f);

            DefenseType defense = body.EffectiveDefense(body.Core);

            defense.Should().Be(DefenseType.ThickHide);
        }

        [Test]
        public void EffectiveDefense_WithDestroyedHide_IsNone()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);
            BodyPart hide = body.AddPart(TestContent.ThickHide, 1f);
            hide.ApplyDamage(100f);

            DefenseType defense = body.EffectiveDefense(body.Core);

            defense.Should().Be(DefenseType.None);
        }

        [Test]
        public void RescaleHp_AfterConstitution_KeepsTheHealthFraction()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);
            body.Core.ApplyDamage(30f);

            body.RescaleHp(2f);

            body.Core.MaxHp.Should().BeApproximately(120f, Tolerance);
            body.Core.Hp.Should().BeApproximately(60f, Tolerance);
        }

        [Test]
        public void TotalHp_SumsEveryPart()
        {
            var body = new Body(TestContent.Core, 1f, WoundedThreshold);
            body.AddPart(TestContent.Arm, 1f);
            body.Core.ApplyDamage(10f);

            float total = body.TotalHp;

            total.Should().BeApproximately(70f, Tolerance);
            body.TotalMaxHp.Should().BeApproximately(80f, Tolerance);
        }
    }
}
