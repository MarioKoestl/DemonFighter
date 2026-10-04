#nullable enable
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Tests.Content
{
    /// <summary>Hand-written specs that stand in for the content assets, so simulation tests need no Unity.</summary>
    internal static class TestContent
    {
        public const string CoreId = "part.core";
        public const string ArmId = "part.arm.test";
        public const string LegsId = "part.legs.test";
        public const string JawsId = "part.jaws.test";
        public const string EyesId = "part.eyes.test";
        public const string TailId = "part.tail.test";
        public const string HideId = "part.hide.thick.test";
        public const string SpinesId = "part.spines.test";
        public const string BiteId = "skill.bite";
        public const string ClawId = "skill.claw";
        public const string GrabId = "skill.grab";
        public const string LungeId = "skill.lunge";
        public const string SprintId = "skill.sprint";
        public const string TailSwingId = "skill.tailswing";

        public static readonly SkillSpec Bite = new SkillSpec
        {
            Id = BiteId,
            Name = "Bite",
            Perk = new SkillPerkSpec { Name = "Deep Bite", Description = "Bite wounds bleed half again as long.", BleedDurationMultiplier = 1.5f },
        };

        public static readonly SkillSpec Claw = new SkillSpec
        {
            Id = ClawId,
            Name = "Claw",
            DamageType = DamageType.Cut,
            BaseDamage = 8f,
            StaminaCost = 10f,
            WindupSeconds = 0.1f,
            ActiveSeconds = 0.15f,
            RecoverySeconds = 0.25f,
            CooldownSeconds = 0.4f,
            BleedSeconds = 4f,
            BleedDamagePerSecond = 3f,
            SlotPriority = 1,
            Perk = new SkillPerkSpec { Name = "Quick Claw", Description = "Claw recovers a quarter faster.", RecoveryMultiplier = 0.75f },
        };

        public static readonly SkillSpec Grab = new SkillSpec
        {
            Id = GrabId,
            Name = "Grab",
            BehaviourId = "grab",
            InputSlot = SkillSlot.Secondary,
            DamageType = DamageType.Blunt,
            BaseDamage = 0f,
            BleedSeconds = 0f,
            StaminaCost = 20f,
            CooldownSeconds = 4f,
            HoldSeconds = 1.5f,
            Perk = new SkillPerkSpec { Name = "Iron Grip", Description = "Grab holds for two seconds.", EffectMultiplier = 4f / 3f },
        };

        public static readonly SkillSpec Lunge = new SkillSpec
        {
            Id = LungeId,
            Name = "Lunge",
            BehaviourId = "lunge",
            InputSlot = SkillSlot.Lunge,
            DamageType = DamageType.Blunt,
            BaseDamage = 10f,
            BleedSeconds = 0f,
            StaggerSeconds = 0.5f,
            StaminaCost = 25f,
            WindupSeconds = 0.1f,
            ActiveSeconds = 0.3f,
            RecoverySeconds = 0.3f,
            CooldownSeconds = 3f,
            DashMeters = 4f,
            KnockbackMeters = 2f,
            Perk = new SkillPerkSpec { Name = "Long Leap", Description = "Lunge leaps a quarter farther.", EffectMultiplier = 1.25f },
        };

        public static readonly SkillSpec Sprint = new SkillSpec
        {
            Id = SprintId,
            Name = "Sprint",
            BehaviourId = string.Empty,
            InputSlot = SkillSlot.Sprint,
            IsPassive = true,
            EnablesSprint = true,
            BaseDamage = 0f,
            BleedSeconds = 0f,
            StaminaCost = 0f,
            StaminaCostPerSecond = 15f,
            SkillXpPerSecond = 2f,
            SkillXpPerHit = 0f,
            Perk = new SkillPerkSpec { Name = "Tireless", Description = "Sprint costs a third less stamina.", StaminaMultiplier = 0.7f },
        };

        public static readonly SkillSpec TailSwing = new SkillSpec
        {
            Id = TailSwingId,
            Name = "Tail Swing",
            InputSlot = SkillSlot.TailSwing,
            DamageType = DamageType.Blunt,
            BaseDamage = 14f,
            BleedSeconds = 0f,
            StaggerSeconds = 0.6f,
            ArcDegrees = 150f,
            StaminaCost = 20f,
            CooldownSeconds = 2f,
            Perk = new SkillPerkSpec { Name = "Heavy Tail", Description = "Tail Swing staggers half again as long.", EffectMultiplier = 1.5f },
        };

        public const string RoarId = "skill.roar";

        public static readonly SkillSpec Roar = new SkillSpec
        {
            Id = RoarId,
            Name = "Roar",
            InputSlot = SkillSlot.None,
            DamageType = DamageType.Blunt,
            BaseDamage = 5f,
            BleedSeconds = 0f,
            StaggerSeconds = 1f,
            ArcDegrees = 360f,
            StaminaCost = 30f,
            CooldownSeconds = 10f,
        };

        public static readonly EvolutionSpec Brute1 = new EvolutionSpec
        {
            Id = "evolution.brute.1",
            Name = "Brute",
            Stage = 1,
            StatPoints = 6,
            StatCapBonuses = new[] { new StatValue(StatIds.Strength, 5) },
            StatBonuses = new[] { new StatValue(StatIds.Strength, 10) },
            UnlockedPartIds = new[] { TailId },
            FreeMutationPartIds = new[] { HideId },
            FitStat = StatIds.Strength,
        };

        public static readonly EvolutionSpec Stalker1 = new EvolutionSpec
        {
            Id = "evolution.stalker.1",
            Name = "Stalker",
            Stage = 1,
            StatPoints = 4,
            StatCapBonuses = new[] { new StatValue(StatIds.Agility, 5) },
            StatBonuses = new[] { new StatValue(StatIds.Agility, 10) },
            UnlockedPartIds = new[] { EyesId },
            FreeMutationPartIds = new[] { LegsId },
            FitStat = StatIds.Agility,
        };

        public static readonly EvolutionSpec Bulwark1 = new EvolutionSpec
        {
            Id = "evolution.bulwark.1",
            Name = "Bulwark",
            Stage = 1,
            StatPoints = 5,
            StatCapBonuses = new[] { new StatValue(StatIds.Constitution, 5) },
            StatBonuses = new[] { new StatValue(StatIds.Constitution, 10) },
            FreeMutationPartIds = new[] { HideId },
            FitStat = StatIds.Constitution,
        };

        public static readonly EvolutionSpec Brute2 = Brute1 with { Id = "evolution.brute.2", Stage = 2, StatPoints = 8, UnlockedPartIds = System.Array.Empty<string>(), FreeMutationPartIds = System.Array.Empty<string>(), ExtraSkillIds = new[] { RoarId } };

        public static readonly EvolutionSpec Stalker2 = Stalker1 with { Id = "evolution.stalker.2", Stage = 2, StatPoints = 6, UnlockedPartIds = System.Array.Empty<string>(), FreeMutationPartIds = System.Array.Empty<string>() };

        public static readonly EvolutionSpec Bulwark2 = Bulwark1 with { Id = "evolution.bulwark.2", Stage = 2, StatPoints = 7, FreeMutationPartIds = System.Array.Empty<string>() };

        public static readonly BodyPartSpec Core = new BodyPartSpec
        {
            Id = CoreId,
            Name = "Core",
            Socket = SocketKind.Core,
            MaxHp = 60f,
            Fate = PartFate.Destroyed,
            GrantedSkillIds = new[] { BiteId },
            BiomassValue = 20f,
            MaxUpgrade = 0,
            Sockets = new[]
            {
                new SocketSlot(SocketKind.Head, 2),
                new SocketSlot(SocketKind.Limb, 2),
                new SocketSlot(SocketKind.Locomotion, 1),
                new SocketSlot(SocketKind.Hide, 1),
                new SocketSlot(SocketKind.Tail, 1),
            },
        };

        public static readonly BodyPartSpec Arm = new BodyPartSpec
        {
            Id = ArmId,
            Name = "Test Arm",
            Socket = SocketKind.Limb,
            MaxHp = 20f,
            Fate = PartFate.Severed,
            BiomassValue = 5f,
            GrantedSkillIds = new[] { ClawId, GrabId },
            StatBonusesPerLevel = new[] { new StatValue(StatIds.Strength, 1) },
            BiomassCost = 30f,
            RepeatMinLevel = 3,
        };

        public static readonly BodyPartSpec Legs = new BodyPartSpec
        {
            Id = LegsId,
            Name = "Test Legs",
            Socket = SocketKind.Locomotion,
            MaxHp = 25f,
            Fate = PartFate.Severed,
            BiomassValue = 6f,
            MoveSpeedBonus = 0.5f,
            GrantedSkillIds = new[] { LungeId, SprintId },
            BiomassCost = 40f,
        };

        public static readonly BodyPartSpec Jaws = new BodyPartSpec
        {
            Id = JawsId,
            Name = "Test Jaws",
            Socket = SocketKind.Head,
            MaxHp = 15f,
            Fate = PartFate.Destroyed,
            BiomassValue = 3f,
            SkillDamageBonusesPerLevel = new[] { new SkillBonus(BiteId, 0.25f) },
        };

        public static readonly BodyPartSpec Eyes = new BodyPartSpec
        {
            Id = EyesId,
            Name = "Test Eyes",
            Socket = SocketKind.Head,
            MaxHp = 10f,
            Fate = PartFate.Destroyed,
            BiomassValue = 2f,
            PerceptionBonus = 0.3f,
        };

        public static readonly BodyPartSpec Tail = new BodyPartSpec
        {
            Id = TailId,
            Name = "Test Tail",
            Socket = SocketKind.Tail,
            MaxHp = 18f,
            Fate = PartFate.Severed,
            BiomassValue = 5f,
            GrantedSkillIds = new[] { TailSwingId },
            BiomassCost = 35f,
            MinLevel = 4,
            RequiresUnlock = true,
            RequiredPartIds = new[] { LegsId },
        };

        public static readonly BodyPartSpec Spines = new BodyPartSpec
        {
            Id = SpinesId,
            Name = "Test Spines",
            Socket = SocketKind.Hide,
            MaxHp = 30f,
            Fate = PartFate.Destroyed,
            BiomassValue = 3f,
            BiomassCost = 35f,
            RequiresUnlock = true,
            ReturnDamageFraction = 0.3f,
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
            BiomassCost = 35f,
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
            return new ContentCatalog(
                Tuning,
                new[] { Core, Arm, Legs, Jaws, Eyes, Tail, ThickHide, Spines },
                new[] { Bite, Claw, Grab, Lunge, Sprint, TailSwing, Roar },
                new[] { Blob, Elder },
                new[] { Brute1, Stalker1, Bulwark1, Brute2, Stalker2, Bulwark2 });
        }
    }
}
