#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Skills
{
    public sealed class SkillBehaviourRegistryTests
    {
        [Test]
        public void Get_MeleeStrike_IsFoundByAttribute()
        {
            var registry = new SkillBehaviourRegistry();

            ISkillBehaviour behaviour = registry.Get(MeleeStrikeBehaviour.Id);

            behaviour.Should().BeOfType<MeleeStrikeBehaviour>();
        }

        [Test]
        public void Get_UnknownId_ThrowsContentException()
        {
            var registry = new SkillBehaviourRegistry();

            Action act = () => registry.Get("nope");

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Validate_CatalogWithKnownBehaviours_Passes()
        {
            var registry = new SkillBehaviourRegistry();

            Action act = () => registry.Validate(TestContent.Catalog());

            act.Should().NotThrow();
        }

        [Test]
        public void Validate_SkillNamingUnknownBehaviour_Throws()
        {
            var registry = new SkillBehaviourRegistry();
            SkillSpec odd = TestContent.Bite with { Id = "skill.odd", BehaviourId = "nope" };
            var catalog = new ContentCatalog(TestContent.Tuning, new[] { TestContent.Core }, new[] { TestContent.Bite, odd }, Array.Empty<DemonSpec>());

            Action act = () => registry.Validate(catalog);

            act.Should().Throw<ContentException>();
        }
    }
}
