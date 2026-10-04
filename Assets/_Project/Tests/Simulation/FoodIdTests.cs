#nullable enable
using System;
using AwesomeAssertions;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class FoodIdTests
    {
        [Test]
        public void IsValid_None_IsFalse()
        {
            FoodId id = FoodId.None;

            bool isValid = id.IsValid;

            isValid.Should().BeFalse();
        }

        [Test]
        public void Constructor_NegativeValue_Throws()
        {
            Action act = () => _ = new FoodId(-1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Equals_SameValue_IsEqual()
        {
            var first = new FoodId(3);
            var second = new FoodId(3);

            bool equal = first.Equals(second);

            equal.Should().BeTrue();
        }
    }
}
