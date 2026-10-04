#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Worldgen
{
    public sealed class HeightfieldTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void SampleHeight_AtAVertex_ReturnsThatVertex()
        {
            Heightfield field = TwoByTwo();

            float height = field.SampleHeight(2f, 0f);

            height.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void SampleHeight_BetweenVertices_InterpolatesBilinearly()
        {
            Heightfield field = TwoByTwo();

            float height = field.SampleHeight(1f, 1f);

            height.Should().BeApproximately(1.5f, Tolerance);
        }

        [Test]
        public void SampleHeight_OutsideTheField_ClampsToTheEdge()
        {
            Heightfield field = TwoByTwo();

            float height = field.SampleHeight(50f, -50f);

            height.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void MaxSlopeTangent_RiseOfTwoOverTwo_IsOne()
        {
            Heightfield field = TwoByTwo();

            float slope = field.MaxSlopeTangent();

            slope.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void Constructor_WrongArrayLength_Throws()
        {
            Action act = () => _ = new Heightfield(2, 2, 1f, 0f, 0f, new float[3]);

            act.Should().Throw<ArgumentException>();
        }

        // Heights: (0,0)=0 (2,0)=1 (0,2)=2 (2,2)=3 with a 2 m cell.
        private static Heightfield TwoByTwo()
        {
            return new Heightfield(2, 2, 2f, 0f, 0f, new[] { 0f, 1f, 2f, 3f });
        }
    }
}
