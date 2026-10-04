#nullable enable
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Mutation
{
    public sealed class OfferPolicyTests
    {
        [Test]
        public void Shop_FreshBlobWithBiomass_ListsEveryPartKindWithItsVerdict()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().AsPlayer().SpawnInto(state);
            demon.GainBiomass(100f);

            IReadOnlyList<MutationOffer> offers = new ShopOfferPolicy().Offers(state, demon);

            offers.Should().HaveCount(7);
            offers.Should().OnlyContain(o => o.Kind == MutationKind.Attach);
            offers.Single(o => o.Part.Id == TestContent.ArmId).Available.Should().BeTrue();
            offers.Single(o => o.Part.Id == TestContent.TailId).Available.Should().BeFalse();
            offers.Single(o => o.Part.Id == TestContent.TailId).Reason.Should().Be(MutationRules.Locked);
            offers.Single(o => o.Part.Id == TestContent.LegsId).Cost.Should().Be(40f);
        }

        [Test]
        public void Shop_WithAnArmAndALostLegs_AddsUpgradeAndRegrowOffers()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().AsPlayer().SpawnInto(state);
            demon.GainBiomass(100f);
            BodyPart arm = demon.AttachPart(TestContent.Arm);
            BodyPart legs = demon.AttachPart(TestContent.Legs);
            legs.ApplyDamage(1000f);

            IReadOnlyList<MutationOffer> offers = new ShopOfferPolicy().Offers(state, demon);

            MutationOffer upgrade = offers.Single(o => o.Kind == MutationKind.Upgrade);
            upgrade.PartIndex.Should().Be(arm.Index);
            upgrade.Cost.Should().Be(15f);
            upgrade.Available.Should().BeFalse();
            upgrade.Reason.Should().Be(MutationRules.LevelTooLow);
            MutationOffer regrow = offers.Single(o => o.Kind == MutationKind.Regrow);
            regrow.PartIndex.Should().Be(legs.Index);
            regrow.Cost.Should().Be(20f);
            regrow.Available.Should().BeTrue();
        }

        [Test]
        public void Shop_InCombat_OffersAsUsual()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon demon = new DemonBuilder().AsPlayer().SpawnInto(state);
            Demon other = new DemonBuilder().At(0f, 3f).SpawnInto(state);
            demon.GainBiomass(100f);
            ticker.Damage.ApplyDamage(demon, demon.Body.Core, 1f, DamageType.Pierce, other.Id);

            IReadOnlyList<MutationOffer> offers = new ShopOfferPolicy().Offers(state, demon);

            offers.Should().Contain(o => o.Available);
        }

        [Test]
        public void Random_Hand_ShowsThreeAvailableAttachOffersAndStaysTheSameUntilALevelUp()
        {
            RunState state = new RunStateBuilder().Build();
            Demon demon = new DemonBuilder().AsPlayer().SpawnInto(state);
            demon.GainBiomass(1000f);
            var policy = new RandomOfferPolicy();

            IReadOnlyList<MutationOffer> first = policy.Offers(state, demon);
            IReadOnlyList<MutationOffer> second = policy.Offers(state, demon);
            demon.GainXp(100f, TestContent.Tuning);
            IReadOnlyList<MutationOffer> afterLevel = policy.Offers(state, demon);

            first.Should().HaveCount(3);
            first.Should().OnlyContain(o => o.Kind == MutationKind.Attach && o.Available);
            second.Select(o => o.Part.Id).Should().Equal(first.Select(o => o.Part.Id));
            afterLevel.Should().HaveCount(3);
        }
    }
}
