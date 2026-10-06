#nullable enable
using AwesomeAssertions;
using DemonFighter.UI;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.UI
{
    public sealed class SettingsValuesTests
    {
        [Test]
        public void Clamped_PullsVolumesAndSensitivityIntoRange()
        {
            var values = new SettingsValues { Master = 1.5f, Effects = -0.2f, Ambient = 0.5f, Music = 2f, MouseSensitivity = 9f };

            SettingsValues clamped = values.Clamped();

            clamped.Master.Should().Be(1f);
            clamped.Effects.Should().Be(0f);
            clamped.Ambient.Should().Be(0.5f);
            clamped.Music.Should().Be(1f);
            clamped.MouseSensitivity.Should().Be(SettingsValues.MaxSensitivity);
        }

        [Test]
        public void Clamped_KeepsIndicesInsideTheirLists()
        {
            var values = new SettingsValues { PresetNames = new[] { "Low", "High" }, PresetIndex = 5, Resolutions = new string[0], ResolutionIndex = 3 };

            SettingsValues clamped = values.Clamped();

            clamped.PresetIndex.Should().Be(1);
            clamped.ResolutionIndex.Should().Be(0);
        }

        [Test]
        public void Copy_IsIndependentOfTheOriginal()
        {
            var values = new SettingsValues { Master = 0.3f, InvertY = true, RandomOffers = true, TestMode = true };

            SettingsValues copy = values.Copy();
            copy.Master = 0.9f;
            copy.InvertY = false;

            values.Master.Should().Be(0.3f);
            values.InvertY.Should().BeTrue();
            copy.RandomOffers.Should().BeTrue();
            copy.TestMode.Should().BeTrue();
            values.Clamped().TestMode.Should().BeTrue();
        }

        [Test]
        public void Defaults_MatchTheSettingsFileDefaults()
        {
            var values = new SettingsValues();

            values.Master.Should().Be(0.8f);
            values.Effects.Should().Be(1f);
            values.Ambient.Should().Be(1f);
            values.Music.Should().Be(0.7f);
            values.MouseSensitivity.Should().Be(1f);
            values.Fullscreen.Should().BeTrue();
            values.VSync.Should().BeTrue();
        }
    }
}
