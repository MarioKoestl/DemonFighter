#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Content
{
    public sealed class SpecValidationTests
    {
        [Test]
        public void BodyPart_Validate_WithoutId_Throws()
        {
            BodyPartSpec spec = TestContent.Arm with { Id = string.Empty };

            Action act = () => spec.Validate();

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void BodyPart_Validate_DefenseOnALimb_Throws()
        {
            BodyPartSpec spec = TestContent.Arm with { Defense = DefenseType.Plates };

            Action act = () => spec.Validate();

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void BodyPart_IsCore_CoreSocket_IsTrue()
        {
            bool isCore = TestContent.Core.IsCore;

            isCore.Should().BeTrue();
            TestContent.Arm.IsCore.Should().BeFalse();
        }

        [Test]
        public void Skill_Validate_ZeroActiveWindow_Throws()
        {
            SkillSpec spec = TestContent.Bite with { ActiveSeconds = 0f };

            Action act = () => spec.Validate();

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Skill_Validate_Defaults_Pass()
        {
            Action act = () => TestContent.Bite.Validate();

            act.Should().NotThrow();
        }

        [Test]
        public void SkillLevelCurve_XpForNextLevel_Level1_IsBaseXp()
        {
            var curve = new SkillLevelCurve();

            float xp = curve.XpForNextLevel(1);

            xp.Should().BeApproximately(100f, 0.0001f);
        }

        [Test]
        public void SkillLevelCurve_XpForNextLevel_Level0_Throws()
        {
            var curve = new SkillLevelCurve();

            Action act = () => curve.XpForNextLevel(0);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void StatId_SameValue_IsEqual()
        {
            var first = new StatId("stat.strength");

            bool equal = first == StatIds.Strength;

            equal.Should().BeTrue();
            first.GetHashCode().Should().Be(StatIds.Strength.GetHashCode());
        }

        [Test]
        public void StatId_Default_IsInvalid()
        {
            StatId id = default;

            bool valid = id.IsValid;

            valid.Should().BeFalse();
        }
    }
}
