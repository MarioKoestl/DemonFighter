#nullable enable
using AwesomeAssertions;
using DemonFighter.Data;
using DemonFighter.Editor.Generate;
using NUnit.Framework;
using UnityEditor;

namespace DemonFighter.Editor.Tests.Setup
{
    public sealed class AudioCatalogTests
    {
        private const string BiomePath = "Assets/_Project/Content/Biomes/BI_AshCavern.asset";
        private const string BitePath = "Assets/_Project/Content/Skills/SK_Bite.asset";
        private const string SprintPath = "Assets/_Project/Content/Skills/SK_Sprint.asset";
        private const string ArmPath = "Assets/_Project/Content/BodyParts/BP_Arm.asset";

        // The generator creates these assets once; the project ships with them (D-084).
        [Test]
        public void Catalog_HasEverySoundWithClips()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalogDefinition>(AudioGenerator.CatalogPath);

            (catalog != null).Should().BeTrue("run Demon Fighter > Generate > Placeholder Assets");
            HasClips(catalog!.Footstep);
            HasClips(catalog.Eat);
            HasClips(catalog.WetImpact);
            HasClips(catalog.PartSevered);
            HasClips(catalog.PartDestroyed);
            HasClips(catalog.DemonDeath);
            HasClips(catalog.PlayerDeath);
            HasClips(catalog.MutationStart);
            HasClips(catalog.MutationComplete);
            HasClips(catalog.Evolved);
            HasClips(catalog.LevelUp);
            HasClips(catalog.ThreatRise);
            HasClips(catalog.Burn);
            (catalog.MenuTrack != null).Should().BeTrue();
            catalog.FootstepStridePerMeter.Should().BeGreaterThan(0f);
        }

        [Test]
        public void Biome_BringsItsLoopsAndTracks()
        {
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomePath);

            (biome != null).Should().BeTrue();
            (biome!.AmbientLoop != null).Should().BeTrue();
            (biome.LavaLoop != null).Should().BeTrue();
            (biome.CalmTrack != null).Should().BeTrue();
            (biome.CombatTrack != null).Should().BeTrue();
        }

        [Test]
        public void Skills_CarryTheirUseSoundOrNone()
        {
            var bite = AssetDatabase.LoadAssetAtPath<SkillDefinition>(BitePath);
            var sprint = AssetDatabase.LoadAssetAtPath<SkillDefinition>(SprintPath);

            (bite!.UseSound != null).Should().BeTrue("a bite snaps");
            (sprint!.UseSound == null).Should().BeTrue("sprinting is quiet by itself");
        }

        [Test]
        public void Parts_CarryTheirSeverSound()
        {
            var arm = AssetDatabase.LoadAssetAtPath<BodyPartDefinition>(ArmPath);

            (arm!.SeverSound != null).Should().BeTrue();
            arm.SeverSound!.Id.Should().Be("sound.sever");
        }

        [Test]
        public void Events_PickAClipAndRespectTheirRanges()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalogDefinition>(AudioGenerator.CatalogPath);
            AudioEventDefinition footstep = catalog!.Footstep!;

            (footstep.PickClip(0f) != null).Should().BeTrue();
            (footstep.PickClip(1f) != null).Should().BeTrue();
            footstep.MaxDistance.Should().BeGreaterThan(footstep.MinDistance);
            footstep.Spatial.Should().BeTrue();
            catalog.LevelUp!.Spatial.Should().BeFalse("the player's own cues play in the head");
        }

        private static void HasClips(AudioEventDefinition? sound)
        {
            (sound != null).Should().BeTrue();
            sound!.HasClips.Should().BeTrue(sound.name + " needs a clip");
        }
    }
}
