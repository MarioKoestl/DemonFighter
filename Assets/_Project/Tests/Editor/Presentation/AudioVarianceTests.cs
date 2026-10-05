#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.Audio;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class AudioVarianceTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Apply_MiddleOfTheRange_LeavesTheValueAlone()
        {
            AudioVariance.Apply(1f, 0.1f, 0.5f).Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void Apply_EndsOfTheRange_MoveByTheVariance()
        {
            AudioVariance.Apply(1f, 0.1f, 1f).Should().BeApproximately(1.1f, Tolerance);
            AudioVariance.Apply(1f, 0.1f, 0f).Should().BeApproximately(0.9f, Tolerance);
            AudioVariance.Apply(0.5f, 0.2f, 1f).Should().BeApproximately(0.6f, Tolerance);
        }

        [Test]
        public void Apply_NegativeVariance_CountsAsNone()
        {
            AudioVariance.Apply(1f, -0.5f, 1f).Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void PitchForSize_BiggerBodiesSoundLower()
        {
            AudioVariance.PitchForSize(1f).Should().BeApproximately(1f, Tolerance);
            AudioVariance.PitchForSize(4f).Should().BeApproximately(0.5f, Tolerance);
            AudioVariance.PitchForSize(0.01f).Should().BeApproximately(2f, Tolerance);
        }
    }
}
