#nullable enable
using System;
using AwesomeAssertions;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class DemonTemplateTests
    {
        [Test]
        public void Constructor_ValidNumbers_KeepsThem()
        {
            var template = new DemonTemplate("Blob", 0, 1.2f, 4f, 1.6f);

            float size = template.SizeMeters;

            size.Should().BeApproximately(1.2f, 0.0001f);
            template.Name.Should().Be("Blob");
        }

        [Test]
        public void Constructor_ZeroSize_Throws()
        {
            Action act = () => _ = new DemonTemplate("Blob", 0, 0f, 4f, 1.6f);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Constructor_SprintSlowerThanWalking_Throws()
        {
            Action act = () => _ = new DemonTemplate("Blob", 0, 1.2f, 4f, 0.5f);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Constructor_BlankName_Throws()
        {
            Action act = () => _ = new DemonTemplate(" ", 0, 1.2f, 4f, 1.6f);

            act.Should().Throw<ArgumentException>();
        }
    }
}
