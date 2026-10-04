#nullable enable
using System.Numerics;
using AwesomeAssertions;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class MovementIntentTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Constructor_DiagonalKeyboardInput_IsClampedToUnitLength()
        {
            var intent = new MovementIntent(new Vector2(1f, 1f), sprint: false);

            float length = intent.Direction.Length();

            length.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void Constructor_HalfStick_KeepsItsLength()
        {
            var intent = new MovementIntent(new Vector2(0.5f, 0f), sprint: false);

            float length = intent.Direction.Length();

            length.Should().BeApproximately(0.5f, Tolerance);
        }

        [Test]
        public void Constructor_InputInsideTheDeadZone_IsStandingStill()
        {
            var intent = new MovementIntent(new Vector2(0.00001f, 0f), sprint: true);

            bool moving = intent.IsMoving;

            moving.Should().BeFalse();
            intent.Direction.Should().Be(Vector2.Zero);
        }

        [Test]
        public void None_Always_IsNotMoving()
        {
            MovementIntent intent = MovementIntent.None;

            bool moving = intent.IsMoving;

            moving.Should().BeFalse();
            intent.Sprint.Should().BeFalse();
        }
    }
}
