#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.Demons;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    /// <summary>The core stands on its legs: lifted as far as they reach below it, down again when they go (D-094).</summary>
    public sealed class DemonFigureStanceTests
    {
        private const float Tolerance = 0.001f;
        private const float Size = 2f;
        private const float RadiusPerMeter = 0.3f;

        private GameObject _demon = null!;
        private DemonFigure _figure = null!;

        [SetUp]
        public void BuildAFigure()
        {
            _demon = new GameObject("Demon");
            var body = new GameObject("Body");
            body.transform.SetParent(_demon.transform, false);
            _figure = new DemonFigure(_demon.transform, null, body.transform, null, null);
            _figure.ApplySize(Size, RadiusPerMeter);
        }

        [TearDown]
        public void RemoveTheFigure()
        {
            Object.DestroyImmediate(_demon);
        }

        // A cube half a body unit wide, 0.1 up on the rig: its bottom is 0.15 body units, so 0.3 m, below the core.
        [Test]
        public void SnapStance_LegsReachingBelowTheCore_LiftTheRootUntilTheirLowestPointIsOnTheGround()
        {
            _figure.AddStandingPart(Leg(0.1f));

            _figure.SnapStance();

            _figure.Stance.Should().BeApproximately(0.3f, Tolerance);
            _figure.Root.localPosition.y.Should().BeApproximately(0.3f, Tolerance);
        }

        [Test]
        public void SnapStance_PartWithinTheCore_LeavesItOnTheGround()
        {
            _figure.AddStandingPart(Leg(0.5f));

            _figure.SnapStance();

            _figure.Stance.Should().Be(0f, "a part that does not reach below the core carries nothing");
        }

        [Test]
        public void SnapStance_WithoutStandingParts_IsZero()
        {
            _figure.SnapStance();

            _figure.Stance.Should().Be(0f);
        }

        // New legs raise the body over the settle time instead of popping it up; legs that go let it down the same way.
        [Test]
        public void UpdateStance_RisesOnNewLegsAndDropsWhenTheyGo()
        {
            BodyPartView leg = Leg(0.1f);
            _figure.AddStandingPart(leg);

            _figure.UpdateStance(0.15f);
            float halfway = _figure.Stance;
            _figure.UpdateStance(1f);
            float standing = _figure.Stance;
            Object.DestroyImmediate(leg.gameObject);
            _figure.UpdateStance(0.15f);
            float dropping = _figure.Stance;
            _figure.UpdateStance(1f);

            halfway.Should().BeApproximately(0.15f, Tolerance);
            standing.Should().BeApproximately(0.3f, Tolerance);
            dropping.Should().BeApproximately(0.15f, Tolerance);
            _figure.Stance.Should().Be(0f);
        }

        [Test]
        public void Animate_AddsTheStanceToTheBob()
        {
            _figure.AddStandingPart(Leg(0.1f));
            _figure.SnapStance();

            _figure.Animate(Vector3.up * 0.05f, Quaternion.identity, Vector3.one);

            _figure.Root.localPosition.y.Should().BeApproximately(0.35f, Tolerance);
        }

        // Upgrades show as growth around the pivot (D-096): +5 at 0.04 per level is a fifth bigger.
        [Test]
        public void ShowUpgrade_GrowsThePartAroundItsPivot()
        {
            BodyPartView leg = Leg(0.1f);

            leg.ShowUpgrade(5, 0.04f);

            leg.transform.localScale.x.Should().BeApproximately(0.6f, Tolerance);
            leg.transform.localPosition.y.Should().BeApproximately(0.1f, Tolerance, "the part grows where it meets the body");
        }

        [Test]
        public void SnapStance_UpgradedLegs_StandHigher()
        {
            BodyPartView leg = Leg(0.1f);
            _figure.AddStandingPart(leg);
            leg.ShowUpgrade(5, 0.04f);

            _figure.SnapStance();

            _figure.Stance.Should().BeApproximately(0.4f, Tolerance, "a cube 0.6 wide at 0.1 reaches 0.2 body units, 0.4 m, below the core");
        }

        // A swing turns the part about the axes of the body, whichever way the model was turned to fit.
        [Test]
        public void SetAnimation_TurnsAboutTheAxesOfTheBody()
        {
            BodyPartView leg = Leg(0.1f);
            leg.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            leg.RememberBasePose();

            leg.SetAnimation(Quaternion.Euler(30f, 0f, 0f));

            Quaternion expected = Quaternion.Euler(30f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f);
            Quaternion.Angle(leg.transform.localRotation, expected).Should().BeLessThan(0.01f);
        }

        // Rest pose taken after placing, as the figure does for a part it creates.
        private BodyPartView Leg(float height)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.transform.SetParent(_figure.Rig, false);
            leg.transform.localPosition = new Vector3(0f, height, 0f);
            leg.transform.localScale = Vector3.one * 0.5f;
            BodyPartView view = leg.AddComponent<BodyPartView>();
            view.RememberBasePose();
            return view;
        }
    }
}
