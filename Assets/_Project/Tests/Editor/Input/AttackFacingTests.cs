#nullable enable
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Input;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.Input
{
    /// <summary>Which way the body turns while the player attacks (D-093).</summary>
    public sealed class AttackFacingTests
    {
        private static readonly Vector2 North = new Vector2(0f, 1f);
        private static readonly Vector2 South = new Vector2(0f, -1f);
        private static readonly Vector2 East = new Vector2(1f, 0f);

        [Test]
        public void Choose_CameraBehind_TurnsTheBodyWhereTheCameraLooks()
        {
            var heading = Vector2.Normalize(new Vector2(0.3f, 1f));

            AttackFacing.Choose(heading, North, firstPerson: false).Should().Be(heading);
        }

        [Test]
        public void Choose_CameraBeside_StillTurnsTheBody()
        {
            AttackFacing.Choose(East, North, firstPerson: false).Should().Be(East);
        }

        // Turned around to look at the mouth, the camera must not swing the body away again.
        [Test]
        public void Choose_CameraLookingAtTheFace_KeepsTheFacing()
        {
            AttackFacing.Choose(South, North, firstPerson: false).Should().Be(North);
        }

        [Test]
        public void Choose_FirstPerson_AlwaysLooksWhereTheCameraLooks()
        {
            AttackFacing.Choose(South, North, firstPerson: true).Should().Be(South);
        }
    }
}
