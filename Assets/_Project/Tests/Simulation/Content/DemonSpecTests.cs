#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Content
{
    public sealed class DemonSpecTests
    {
        [Test]
        public void Validate_Defaults_Pass()
        {
            Action act = () => TestContent.Blob.Validate();

            act.Should().NotThrow();
        }

        [Test]
        public void Validate_ZeroSize_Throws()
        {
            DemonSpec broken = TestContent.Blob with { SizeMeters = 0f };

            Action act = () => broken.Validate();

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Validate_SprintSlowerThanWalking_Throws()
        {
            DemonSpec broken = TestContent.Blob with { SprintMultiplier = 0.5f };

            Action act = () => broken.Validate();

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Validate_BlankName_Throws()
        {
            DemonSpec broken = TestContent.Blob with { Name = " " };

            Action act = () => broken.Validate();

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void StatValue_NegativePoints_Throws()
        {
            Action act = () => _ = new StatValue(StatIds.Strength, -1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
