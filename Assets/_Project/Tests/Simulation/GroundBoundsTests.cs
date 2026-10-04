#nullable enable
using System;
using System.Numerics;
using AwesomeAssertions;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class GroundBoundsTests
    {
        [Test]
        public void Clamp_PointOutside_MovesItOntoTheEdgeKeepingHeight()
        {
            GroundBounds bounds = GroundBounds.CenteredSquare(10f);

            Vector3 clamped = bounds.Clamp(new Vector3(8f, 3f, -9f));

            clamped.Should().Be(new Vector3(5f, 3f, -5f));
        }

        [Test]
        public void Contains_PointOnTheEdge_IsTrue()
        {
            GroundBounds bounds = GroundBounds.CenteredSquare(10f);

            bool inside = bounds.Contains(new Vector3(5f, 0f, 0f));

            inside.Should().BeTrue();
        }

        [Test]
        public void Shrink_ByMargin_PullsEverySideIn()
        {
            GroundBounds bounds = GroundBounds.CenteredSquare(10f);

            GroundBounds inner = bounds.Shrink(2f);

            inner.MinX.Should().BeApproximately(-3f, 0.0001f);
            inner.MaxZ.Should().BeApproximately(3f, 0.0001f);
        }

        [Test]
        public void Constructor_InvertedExtent_Throws()
        {
            Action act = () => _ = new GroundBounds(5f, 0f, -5f, 10f);

            act.Should().Throw<ArgumentException>();
        }
    }
}
