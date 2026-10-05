#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.Demons;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class DamageStagesTests
    {
        private const float WoundedBelow = 0.5f;
        private const float MangledBelow = 0.2f;

        [TestCase(1f, DamageStage.Intact)]
        [TestCase(0.5f, DamageStage.Intact)]
        [TestCase(0.49f, DamageStage.Wounded)]
        [TestCase(0.2f, DamageStage.Wounded)]
        [TestCase(0.19f, DamageStage.Mangled)]
        [TestCase(0.01f, DamageStage.Mangled)]
        public void For_HealthFraction_PicksTheStageBelowTheThresholds(float hpFraction, DamageStage expected)
        {
            DamageStage stage = DamageStages.For(hpFraction, false, WoundedBelow, MangledBelow);

            stage.Should().Be(expected);
        }

        [Test]
        public void For_LostPart_IsLostWhateverTheHealth()
        {
            DamageStage stage = DamageStages.For(1f, true, WoundedBelow, MangledBelow);

            stage.Should().Be(DamageStage.Lost);
        }

        [Test]
        public void For_MangledThresholdAboveWounded_NeverManglesAHealthyPart()
        {
            DamageStage stage = DamageStages.For(0.6f, false, 0.5f, 0.9f);

            stage.Should().Be(DamageStage.Intact);
        }
    }
}
