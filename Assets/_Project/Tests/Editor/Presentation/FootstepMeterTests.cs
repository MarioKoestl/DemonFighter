#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.Audio;
using DemonFighter.Simulation;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class FootstepMeterTests
    {
        private static readonly DemonId Walker = new DemonId(7);
        private static readonly DemonId Other = new DemonId(8);

        [Test]
        public void Advance_StepsOnceEveryStride()
        {
            var meter = new FootstepMeter();

            bool first = meter.Advance(Walker, 0.3f, 0.8f);
            bool second = meter.Advance(Walker, 0.3f, 0.8f);
            bool third = meter.Advance(Walker, 0.3f, 0.8f);
            bool fourth = meter.Advance(Walker, 0.3f, 0.8f);

            first.Should().BeFalse();
            second.Should().BeFalse();
            third.Should().BeTrue();
            fourth.Should().BeFalse();
        }

        [Test]
        public void Advance_KeepsBodiesApart()
        {
            var meter = new FootstepMeter();
            meter.Advance(Walker, 0.7f, 0.8f);

            bool other = meter.Advance(Other, 0.2f, 0.8f);
            bool walker = meter.Advance(Walker, 0.2f, 0.8f);

            other.Should().BeFalse();
            walker.Should().BeTrue();
        }

        [Test]
        public void Advance_StandingStill_NeverSteps()
        {
            var meter = new FootstepMeter();

            for (int i = 0; i < 100; i++)
            {
                meter.Advance(Walker, 0f, 0.8f).Should().BeFalse();
            }
        }

        [Test]
        public void Advance_WithoutAStride_NeverSteps()
        {
            var meter = new FootstepMeter();

            meter.Advance(Walker, 5f, 0f).Should().BeFalse();
        }

        [Test]
        public void Forget_ResetsTheCount()
        {
            var meter = new FootstepMeter();
            meter.Advance(Walker, 0.7f, 0.8f);

            meter.Forget(Walker);
            bool step = meter.Advance(Walker, 0.2f, 0.8f);

            step.Should().BeFalse();
        }
    }
}
