#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.UI;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.UI
{
    public sealed class ResolutionOptionsTests
    {
        [Test]
        public void Build_DeduplicatesSortsLargestFirstAndFindsTheCurrent()
        {
            var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(2560, 1440) };

            List<string> labels = ResolutionOptions.Build(sizes, new Vector2Int(1920, 1080), out int current);

            labels.Should().Equal("2560 x 1440", "1920 x 1080", "1280 x 720");
            current.Should().Be(1);
        }

        [Test]
        public void Build_AddsAnUnlistedCurrentSize()
        {
            var sizes = new[] { new Vector2Int(1920, 1080) };

            List<string> labels = ResolutionOptions.Build(sizes, new Vector2Int(1600, 900), out int current);

            labels.Should().Equal("1920 x 1080", "1600 x 900");
            current.Should().Be(1);
        }

        [Test]
        public void Build_WithoutSizes_StillListsTheCurrent()
        {
            List<string> labels = ResolutionOptions.Build(new Vector2Int[0], new Vector2Int(1280, 800), out int current);

            labels.Should().Equal("1280 x 800");
            current.Should().Be(0);
        }

        [Test]
        public void TryParse_ReadsALabelBack()
        {
            ResolutionOptions.TryParse("1920 x 1080", out Vector2Int size).Should().BeTrue();
            size.Should().Be(new Vector2Int(1920, 1080));
            ResolutionOptions.TryParse(ResolutionOptions.Label(new Vector2Int(3440, 1440)), out size).Should().BeTrue();
            size.Should().Be(new Vector2Int(3440, 1440));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("wide")]
        [TestCase("0 x 1080")]
        [TestCase("1920 x 1080 x 60")]
        public void TryParse_RejectsAnythingElse(string? label)
        {
            ResolutionOptions.TryParse(label, out _).Should().BeFalse();
        }
    }
}
