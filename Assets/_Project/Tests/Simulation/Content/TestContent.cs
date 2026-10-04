#nullable enable
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Tests.Content
{
    /// <summary>Hand-written specs that stand in for the content assets, so simulation tests need no Unity.</summary>
    internal static class TestContent
    {
        public const string CoreId = "part.core";
        public const string ArmId = "part.arm.test";
        public const string HideId = "part.hide.thick.test";
        public const string BiteId = "skill.bite";

        public static readonly SkillSpec Bite = new SkillSpec { Id = BiteId, Name = "Bite" };

        public static readonly BodyPartSpec Core = new BodyPartSpec
        {
            Id = CoreId,
            Name = "Core",
            Socket = SocketKind.Core,
            MaxHp = 60f,
            Fate = PartFate.Destroyed,
            GrantedSkillIds = new[] { BiteId },
            BiomassValue = 20f,
        };

        public static readonly BodyPartSpec Arm = new BodyPartSpec
        {
            Id = ArmId,
            Name = "Test Arm",
            Socket = SocketKind.Limb,
            MaxHp = 20f,
            Fate = PartFate.Severed,
            BiomassValue = 5f,
        };

        public static readonly BodyPartSpec ThickHide = new BodyPartSpec
        {
            Id = HideId,
            Name = "Test Thick Hide",
            Socket = SocketKind.Hide,
            MaxHp = 30f,
            Defense = DefenseType.ThickHide,
            Fate = PartFate.Destroyed,
            BiomassValue = 3f,
        };

        public static readonly DemonSpec Blob = new DemonSpec { Id = "demon.blob", Name = "Blob" };

        public static readonly DemonSpec Elder = new DemonSpec
        {
            Id = "demon.elder",
            Name = "Elder",
            Tier = 6,
            SizeMeters = 15f,
            MoveSpeed = 3f,
            SprintMultiplier = 1f,
            StartingStats = new[] { new StatValue(StatIds.Strength, 10), new StatValue(StatIds.Constitution, 20) },
        };

        public static readonly CombatTuning Tuning = new CombatTuning();

        public static ContentCatalog Catalog()
        {
            return new ContentCatalog(Tuning, new[] { Core, Arm, ThickHide }, new[] { Bite }, new[] { Blob, Elder });
        }
    }
}
