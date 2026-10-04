#nullable enable
using System;
using AwesomeAssertions;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class DemonIdTests
    {
        [Test]
        public void IsValid_None_IsFalse()
        {
            DemonId id = DemonId.None;

            bool isValid = id.IsValid;

            isValid.Should().BeFalse();
        }

        [Test]
        public void IsValid_PositiveValue_IsTrue()
        {
            var id = new DemonId(1);

            bool isValid = id.IsValid;

            isValid.Should().BeTrue();
        }

        [Test]
        public void Constructor_NegativeValue_Throws()
        {
            Action act = () => _ = new DemonId(-1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Equals_SameValue_IsEqualWithSameHash()
        {
            var first = new DemonId(7);
            var second = new DemonId(7);

            bool equal = first == second;

            equal.Should().BeTrue();
            first.GetHashCode().Should().Be(second.GetHashCode());
        }

        [Test]
        public void Equals_DifferentValue_IsNotEqual()
        {
            var first = new DemonId(7);
            var second = new DemonId(8);

            bool different = first != second;

            different.Should().BeTrue();
        }
    }
}
