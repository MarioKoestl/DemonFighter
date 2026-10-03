#nullable enable
using System;
using AwesomeAssertions;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class IdSequenceTests
    {
        [Test]
        public void Next_FreshSequence_IssuesOneFirst()
        {
            var sequence = new IdSequence();

            int first = sequence.Next();

            first.Should().Be(1);
        }

        [Test]
        public void Next_CalledRepeatedly_IssuesIncreasingIds()
        {
            var sequence = new IdSequence();

            int first = sequence.Next();
            int second = sequence.Next();
            int third = sequence.Next();

            new[] { first, second, third }.Should().Equal(1, 2, 3);
            sequence.LastIssued.Should().Be(3);
        }

        [Test]
        public void Constructor_ResumedAfterLastIssued_ContinuesAfterIt()
        {
            var sequence = new IdSequence(41);

            int next = sequence.Next();

            next.Should().Be(42);
        }

        [Test]
        public void Constructor_NegativeLastIssued_Throws()
        {
            Action act = () => _ = new IdSequence(-1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Next_RangeExhausted_Throws()
        {
            var sequence = new IdSequence(int.MaxValue);

            Action act = () => sequence.Next();

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
