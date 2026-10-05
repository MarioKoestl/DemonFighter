#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.UI;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.UI
{
    /// <summary>Damage numbers over the targets of the player's hits (D-092).</summary>
    public sealed class FloatingDamageTests
    {
        private const float Tolerance = 0.0001f;

        private static readonly DemonId Player = new DemonId(1);
        private static readonly DemonId Enemy = new DemonId(2);
        private static readonly DemonId Other = new DemonId(3);

        [Test]
        public void Add_HitThePlayerLanded_IsShown()
        {
            var numbers = new FloatingDamage();

            bool shown = numbers.Add(new DamageApplied(Enemy, 0, 12f, DamageType.Pierce, Player), Player, Vector3.zero, 1.2f);

            shown.Should().BeTrue();
            numbers.Numbers.Should().ContainSingle().Which.Amount.Should().Be(12f);
        }

        [Test]
        public void Add_HitsThatAreNotThePlayers_AreIgnored()
        {
            var numbers = new FloatingDamage();

            numbers.Add(new DamageApplied(Enemy, 0, 12f, DamageType.Pierce, Other), Player, Vector3.zero, 1f).Should().BeFalse("another demon hit it");
            numbers.Add(new DamageApplied(Enemy, 0, 2f, DamageType.Pierce, DemonId.None), Player, Vector3.zero, 1f).Should().BeFalse("bleeding and fire on others have no attacker");
            numbers.Numbers.Should().BeEmpty();
        }

        [Test]
        public void Add_HitOnThePlayer_IsShownAsTaken()
        {
            var numbers = new FloatingDamage();

            numbers.Add(new DamageApplied(Player, 0, 9f, DamageType.Cut, Enemy), Player, Vector3.zero, 1f);

            numbers.Numbers.Should().ContainSingle().Which.Taken.Should().BeTrue();
        }

        // Lava burns every tick; one number every half second instead of twenty a second.
        [Test]
        public void Add_PlayerBurning_AddsUpIntoOneNumberEveryHalfSecond()
        {
            var numbers = new FloatingDamage();
            for (int i = 0; i < 10; i++)
            {
                numbers.Add(new DamageApplied(Player, 0, 0.6f, DamageType.Fire, DemonId.None), Player, Vector3.zero, 1f);
            }

            numbers.Advance(0.3f);
            int early = numbers.Numbers.Count;
            numbers.Advance(0.3f);

            early.Should().Be(0);
            numbers.Numbers.Should().ContainSingle();
            numbers.Numbers[0].Amount.Should().BeApproximately(6f, Tolerance);
            numbers.Numbers[0].Taken.Should().BeTrue();
        }

        // In test mode the player takes nothing, but sees what it would have taken, in brackets (D-092).
        [Test]
        public void Add_BlockedHitOnThePlayer_IsShownInBrackets()
        {
            var numbers = new FloatingDamage();

            numbers.Add(new DamageBlocked(Player, 0, 12f, DamageType.Pierce, Enemy), Player, Vector3.zero, 1f).Should().BeTrue();
            numbers.Add(new DamageBlocked(Enemy, 0, 12f, DamageType.Pierce, Player), Player, Vector3.zero, 1f).Should().BeFalse("only the player's own blocked damage shows");

            FloatingDamage.Number number = numbers.Numbers.Should().ContainSingle().Which;
            number.Taken.Should().BeTrue();
            number.Blocked.Should().BeTrue();
            FloatingDamage.Text(number.Amount, number.Blocked).Should().Be("(12)");
        }

        [Test]
        public void Advance_PastItsLife_RemovesTheNumber()
        {
            var numbers = new FloatingDamage();
            numbers.Add(new DamageApplied(Enemy, 0, 12f, DamageType.Cut, Player), Player, Vector3.zero, 1f);

            numbers.Advance(FloatingDamage.LifeSeconds * 0.5f);
            int halfway = numbers.Numbers.Count;
            numbers.Advance(FloatingDamage.LifeSeconds * 0.6f);

            halfway.Should().Be(1);
            numbers.Numbers.Should().BeEmpty();
        }

        [Test]
        public void RiseAndAlpha_StartAtTheHeadAndFadeOut()
        {
            FloatingDamage.Rise(0f).Should().Be(0f);
            FloatingDamage.Rise(FloatingDamage.LifeSeconds).Should().BeApproximately(1f, Tolerance);
            FloatingDamage.Rise(0.25f).Should().BeGreaterThan(0.25f, "a number leaves quickly, then slows");
            FloatingDamage.Alpha(0.2f).Should().Be(1f);
            FloatingDamage.Alpha(FloatingDamage.LifeSeconds).Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void WorldPosition_HangsOverTheHeadAndRises()
        {
            var numbers = new FloatingDamage();
            numbers.Add(new DamageApplied(Enemy, 0, 12f, DamageType.Blunt, Player), Player, Vector3.zero, 2f);
            Vector3 atHit = numbers.Numbers[0].WorldPosition(Vector3.zero);

            numbers.Advance(0.5f);
            Vector3 later = numbers.Numbers[0].WorldPosition(Vector3.zero);

            atHit.y.Should().BeGreaterThan(2f, "above the head of a body two meters tall");
            later.y.Should().BeGreaterThan(atHit.y);
        }

        [Test]
        public void Add_QuickHits_FanOutSideways()
        {
            var numbers = new FloatingDamage();
            numbers.Add(new DamageApplied(Enemy, 0, 5f, DamageType.Pierce, Player), Player, Vector3.zero, 1f);
            numbers.Add(new DamageApplied(Enemy, 0, 5f, DamageType.Pierce, Player), Player, Vector3.zero, 1f);

            numbers.Numbers[0].Side.Should().NotBe(numbers.Numbers[1].Side);
        }

        [TestCase(0.3f, "1")]
        [TestCase(12.4f, "12")]
        [TestCase(12.6f, "13")]
        public void Text_ShowsWholePointsAtLeastOne(float amount, string expected)
        {
            FloatingDamage.Text(amount).Should().Be(expected);
        }

        [Test]
        public void ColorFor_EachDamageTypeReadsApart()
        {
            FloatingDamage.ColorFor(DamageType.Pierce).Should().NotBe(FloatingDamage.ColorFor(DamageType.Cut));
            FloatingDamage.ColorFor(DamageType.Cut).Should().NotBe(FloatingDamage.ColorFor(DamageType.Blunt));
            FloatingDamage.ColorFor(DamageType.Blunt).Should().NotBe(FloatingDamage.ColorFor(DamageType.Fire));
        }
    }
}
