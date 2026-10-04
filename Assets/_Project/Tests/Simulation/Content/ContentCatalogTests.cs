#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Content
{
    public sealed class ContentCatalogTests
    {
        private static readonly DemonSpec[] NoDemons = Array.Empty<DemonSpec>();

        [Test]
        public void GetBodyPart_KnownId_ReturnsTheSpec()
        {
            ContentCatalog catalog = TestContent.Catalog();

            BodyPartSpec core = catalog.GetBodyPart(TestContent.CoreId);

            core.Should().BeSameAs(TestContent.Core);
        }

        [Test]
        public void GetBodyPart_UnknownId_ThrowsContentException()
        {
            ContentCatalog catalog = TestContent.Catalog();

            Action act = () => catalog.GetBodyPart("part.nope");

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void GetDemon_KnownId_ReturnsTheSpec()
        {
            ContentCatalog catalog = TestContent.Catalog();

            DemonSpec elder = catalog.GetDemon(TestContent.Elder.Id);

            elder.Should().BeSameAs(TestContent.Elder);
        }

        [Test]
        public void TryGetSkill_UnknownId_IsFalse()
        {
            ContentCatalog catalog = TestContent.Catalog();

            bool found = catalog.TryGetSkill("skill.nope", out SkillSpec? spec);

            found.Should().BeFalse();
            spec.Should().BeNull();
        }

        [Test]
        public void Constructor_DuplicateSkillId_Throws()
        {
            Action act = () => _ = new ContentCatalog(TestContent.Tuning, new[] { TestContent.Core }, new[] { TestContent.Bite, TestContent.Bite }, NoDemons);

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Constructor_PartGrantingUnknownSkill_Throws()
        {
            BodyPartSpec broken = TestContent.Core with { GrantedSkillIds = new[] { "skill.nope" } };

            Action act = () => _ = new ContentCatalog(TestContent.Tuning, new[] { broken }, new[] { TestContent.Bite }, NoDemons);

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Constructor_InvalidSpec_Throws()
        {
            BodyPartSpec broken = TestContent.Arm with { MaxHp = 0f };

            Action act = () => _ = new ContentCatalog(TestContent.Tuning, new[] { broken }, new[] { TestContent.Bite }, NoDemons);

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Constructor_DemonBornAsALimb_Throws()
        {
            DemonSpec broken = TestContent.Blob with { CoreId = TestContent.ArmId };

            Action act = () => _ = new ContentCatalog(TestContent.Tuning, new[] { TestContent.Core, TestContent.Arm }, new[] { TestContent.Bite }, new[] { broken });

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Constructor_DemonWithUnknownStartingStat_Throws()
        {
            DemonSpec broken = TestContent.Blob with { StartingStats = new[] { new StatValue(new StatId("stat.will"), 3) } };

            Action act = () => _ = new ContentCatalog(TestContent.Tuning, new[] { TestContent.Core }, new[] { TestContent.Bite }, new[] { broken });

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Collections_Default_ListEveryRegisteredSpec()
        {
            ContentCatalog catalog = TestContent.Catalog();

            int skills = catalog.Skills.Count;

            skills.Should().Be(7);
            catalog.Evolutions.Count.Should().Be(6);
            catalog.BodyParts.Count.Should().Be(8);
            catalog.Demons.Count.Should().Be(2);
        }
    }
}
