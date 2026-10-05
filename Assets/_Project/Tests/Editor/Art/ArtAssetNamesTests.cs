#nullable enable
using AwesomeAssertions;
using DemonFighter.Editor.Art;
using DemonFighter.Simulation.Content;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.Art
{
    public sealed class ArtAssetNamesTests
    {
        [Test]
        public void TryParsePart_NameWithVariantAndState_GivesIdAndState()
        {
            bool parsed = ArtAssetNames.TryParsePart("BP_Hide_Thick_Wounded", out string partId, out MeshState state);

            parsed.Should().BeTrue();
            partId.Should().Be("part.hide.thick");
            state.Should().Be(MeshState.Wounded);
        }

        [Test]
        public void TryParsePart_NameWithoutState_GivesNoneState()
        {
            bool parsed = ArtAssetNames.TryParsePart("BP_Jaws", out string partId, out MeshState state);

            parsed.Should().BeTrue();
            partId.Should().Be("part.jaws");
            state.Should().Be(MeshState.None);
        }

        [Test]
        public void TryParsePart_IgnoresCasing()
        {
            bool parsed = ArtAssetNames.TryParsePart("bp_ARM_stump", out string partId, out MeshState state);

            parsed.Should().BeTrue();
            partId.Should().Be("part.arm");
            state.Should().Be(MeshState.Stump);
        }

        [TestCase("M_Rock")]
        [TestCase("")]
        [TestCase("BP_")]
        [TestCase("BP_Intact")]
        public void TryParsePart_OutsideTheConvention_IsFalse(string name)
        {
            bool parsed = ArtAssetNames.TryParsePart(name, out string partId, out MeshState state);

            parsed.Should().BeFalse();
            partId.Should().BeEmpty();
            state.Should().Be(MeshState.None);
        }

        [Test]
        public void TryParsePart_NumericLastToken_BelongsToTheId()
        {
            bool parsed = ArtAssetNames.TryParsePart("BP_Jaws_2", out string partId, out MeshState state);

            parsed.Should().BeTrue();
            partId.Should().Be("part.jaws.2");
            state.Should().Be(MeshState.None);
        }

        [Test]
        public void ModelNameFor_RoundTripsThroughTryParsePart()
        {
            string modelName = ArtAssetNames.ModelNameFor("part.hide.thick");
            ArtAssetNames.TryParsePart(modelName, out string partId, out _);

            modelName.Should().Be("BP_Hide_Thick");
            partId.Should().Be("part.hide.thick");
        }

        [TestCase("Socket_Head", SocketKind.Head)]
        [TestCase("Socket_LimbL", SocketKind.Limb)]
        [TestCase("Socket_limbr", SocketKind.Limb)]
        [TestCase("Socket_Locomotion", SocketKind.Locomotion)]
        [TestCase("Socket_Tail", SocketKind.Tail)]
        public void TryParseSocket_KnownSocket_GivesTheKind(string name, SocketKind expected)
        {
            bool parsed = ArtAssetNames.TryParseSocket(name, out SocketKind kind);

            parsed.Should().BeTrue();
            kind.Should().Be(expected);
        }

        [TestCase("Socket_Core")]
        [TestCase("Socket_Hand")]
        [TestCase("Head")]
        [TestCase("")]
        public void TryParseSocket_OtherNames_IsFalse(string name)
        {
            bool parsed = ArtAssetNames.TryParseSocket(name, out _);

            parsed.Should().BeFalse();
        }
    }
}
