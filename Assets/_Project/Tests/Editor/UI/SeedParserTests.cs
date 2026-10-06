#nullable enable
using AwesomeAssertions;
using DemonFighter.UI;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.UI
{
    public sealed class SeedParserTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Parse_BlankText_MeansARandomRun(string? text)
        {
            SeedParser.Parse(text).Should().BeNull();
        }

        [Test]
        public void Parse_PositiveNumber_IsTheSeed()
        {
            SeedParser.Parse(" 42 ").Should().Be(42);
            SeedParser.Parse("2147483647").Should().Be(int.MaxValue);
        }

        [Test]
        public void Parse_Words_GiveTheSameSeedEveryTime()
        {
            int? first = SeedParser.Parse("mario");
            int? second = SeedParser.Parse("mario");
            int? other = SeedParser.Parse("Mario");

            first.Should().NotBeNull();
            first.Should().Be(second);
            first.Should().NotBe(other);
            first!.Value.Should().BePositive();
        }

        [TestCase("0")]
        [TestCase("-7")]
        public void Parse_ZeroOrNegativeNumbers_AreHashedLikeWords(string text)
        {
            int? seed = SeedParser.Parse(text);

            seed.Should().NotBeNull();
            seed!.Value.Should().BePositive();
        }

        [Test]
        public void Hash_IsNeverZeroAndIndependentOfTheProcess()
        {
            SeedParser.Hash("a").Should().Be(SeedParser.Hash("a"));
            SeedParser.Hash("demon").Should().NotBe(0);
            SeedParser.Hash("demon").Should().Be(SeedParser.Parse("demon"));
        }
    }
}
