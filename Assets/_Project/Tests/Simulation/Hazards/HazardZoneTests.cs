#nullable enable
using System;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Hazards;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Hazards
{
    public sealed class HazardZoneTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void Disc_SignedDistance_IsNegativeInsideZeroOnTheEdgePositiveOutside()
        {
            HazardZone disc = HazardZone.Disc(HazardKind.Lava, new Vector2(10f, 0f), 5f);

            disc.SignedDistance(new Vector2(10f, 0f)).Should().BeApproximately(-5f, Tolerance);
            disc.SignedDistance(new Vector2(15f, 0f)).Should().BeApproximately(0f, Tolerance);
            disc.SignedDistance(new Vector2(10f, 8f)).Should().BeApproximately(3f, Tolerance);
        }

        [Test]
        public void Rectangle_Unturned_IsLongAlongZAndNarrowAlongX()
        {
            HazardZone fissure = HazardZone.Rectangle(HazardKind.Fissure, Vector2.Zero, 0f, 2f, 16f);

            fissure.SignedDistance(new Vector2(0f, 7f)).Should().BeLessThan(0f);
            fissure.SignedDistance(new Vector2(0f, 10f)).Should().BeApproximately(2f, Tolerance);
            fissure.SignedDistance(new Vector2(3f, 0f)).Should().BeApproximately(2f, Tolerance);
            fissure.SignedDistance(Vector2.Zero).Should().BeApproximately(-1f, Tolerance);
        }

        [Test]
        public void Rectangle_TurnedAQuarter_RunsAlongX()
        {
            HazardZone fissure = HazardZone.Rectangle(HazardKind.Fissure, Vector2.Zero, MathF.PI * 0.5f, 2f, 16f);

            fissure.SignedDistance(new Vector2(7f, 0f)).Should().BeLessThan(0f);
            fissure.SignedDistance(new Vector2(0f, 3f)).Should().BeApproximately(2f, Tolerance);
        }

        [Test]
        public void Rectangle_Corner_MeasuresTheDiagonal()
        {
            HazardZone box = HazardZone.Rectangle(HazardKind.Fissure, Vector2.Zero, 0f, 2f, 2f);

            box.SignedDistance(new Vector2(4f, 5f)).Should().BeApproximately(5f, Tolerance);
        }

        [Test]
        public void Outward_PointsAwayFromTheZone()
        {
            HazardZone disc = HazardZone.Disc(HazardKind.Lava, Vector2.Zero, 5f);
            HazardZone fissure = HazardZone.Rectangle(HazardKind.Fissure, Vector2.Zero, 0f, 2f, 16f);

            Vector2 fromDisc = disc.Outward(new Vector2(0f, 3f));
            Vector2 fromFissure = fissure.Outward(new Vector2(0.5f, 0f));

            fromDisc.Y.Should().BeApproximately(1f, Tolerance);
            fromFissure.X.Should().BeApproximately(1f, Tolerance);
        }

        [Test]
        public void Reach_CoversTheWholeZone()
        {
            HazardZone.Disc(HazardKind.Lava, Vector2.Zero, 5f).Reach.Should().Be(5f);
            HazardZone.Rectangle(HazardKind.Fissure, Vector2.Zero, 0f, 6f, 8f).Reach.Should().BeApproximately(5f, Tolerance);
        }

        [Test]
        public void Factories_RejectEmptyZones()
        {
            Action disc = () => HazardZone.Disc(HazardKind.Lava, Vector2.Zero, 0f);
            Action rectangle = () => HazardZone.Rectangle(HazardKind.Fissure, Vector2.Zero, 0f, 0f, 3f);

            disc.Should().Throw<ArgumentOutOfRangeException>();
            rectangle.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
