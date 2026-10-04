#nullable enable
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Ai
{
    public sealed class ArchetypeChoiceTests
    {
        [Test]
        public void PickBlobArchetype_WithoutChoices_IsThePlainBlobArchetype()
        {
            var rng = new Rng(1);

            ArchetypeSpec picked = BiomeSpec.AshCavern.PickBlobArchetype(rng);

            picked.Should().BeSameAs(BiomeSpec.AshCavern.BlobArchetype);
        }

        [Test]
        public void PickBlobArchetype_WithTheDefaultChoices_DrawsAllThreeByWeight()
        {
            BiomeSpec biome = BiomeSpec.AshCavern with { BlobArchetypes = BiomeSpec.DefaultBlobArchetypes(BiomeSpec.AshCavern) };
            var rng = new Rng(7);
            var counts = new Dictionary<string, int>();

            for (int i = 0; i < 300; i++)
            {
                string name = biome.PickBlobArchetype(rng).Name;
                counts[name] = counts.TryGetValue(name, out int count) ? count + 1 : 1;
            }

            counts.Should().HaveCount(3);
            counts["Aggressive"].Should().BeGreaterThan(counts["Cautious"]);
            counts["Cautious"].Should().BeGreaterThan(counts["Scavenger"]);
            counts["Scavenger"].Should().BeGreaterThan(0);
        }

        [Test]
        public void DefaultBlobArchetypes_AshCavern_AreAggressiveCautiousAndScavengerWithFleeingOn()
        {
            IReadOnlyList<ArchetypeChoice> choices = BiomeSpec.DefaultBlobArchetypes(BiomeSpec.AshCavern);

            choices.Should().HaveCount(3);
            choices[0].Archetype.Name.Should().Be("Aggressive");
            choices[0].Weight.Should().Be(50f);
            choices[0].Archetype.FleeHealthFraction.Should().Be(0.15f);
            choices[1].Archetype.Name.Should().Be("Cautious");
            choices[1].Weight.Should().Be(30f);
            choices[1].Archetype.FleeHealthFraction.Should().Be(0.4f);
            choices[2].Archetype.Name.Should().Be("Scavenger");
            choices[2].Weight.Should().Be(20f);
            choices[2].Archetype.FleeHealthFraction.Should().Be(0.5f);
            choices[0].Archetype.PreferredPartIds.Should().NotBeEmpty();
        }

        [Test]
        public void ArchetypeChoice_WithoutAPositiveWeight_Throws()
        {
            Action act = () => new ArchetypeChoice(BiomeSpec.AshCavern.BlobArchetype, 0f);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
