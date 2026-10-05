#nullable enable
using System.Collections.Generic;
using DemonFighter.Data;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// The v1 content the generator creates once as assets (GAME_DESIGN, "Skills" and "The body"; DECISIONS D-054 to
    /// D-059): ten body parts, six skills, six evolution options and how the placeholder stage draws each part.
    /// After generation the assets are the truth; this is only their starting point.
    /// </summary>
    internal static class PlaceholderContent
    {
        public const string CoreId = "part.core";
        public const string JawsId = "part.jaws";
        public const string ArmId = "part.arm";
        public const string LegsId = "part.legs";
        public const string ThickHideId = "part.hide.thick";
        public const string PlatesId = "part.hide.plates";
        public const string ElasticTissueId = "part.hide.elastic";
        public const string EyesId = "part.eyes";
        public const string SpinesId = "part.spines";
        public const string TailId = "part.tail";

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
            SlotPriority = 1,
            DamageType = DamageType.Cut,
            BaseDamage = 8f,
            StaminaCost = 10f,
            WindupSeconds = 0.1f,
            ActiveSeconds = 0.15f,
            RecoverySeconds = 0.25f,
            CooldownSeconds = 0.4f,
            BleedSeconds = 4f,
            BleedDamagePerSecond = 3f,
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

        public static readonly IReadOnlyList<SkillSpec> Skills = new[] { Bite, Claw, Grab, Lunge, Sprint, TailSwing };

        public static readonly BodyPartSpec Core = new BodyPartSpec
        {
            Id = CoreId,
            Name = "Core",
            Socket = SocketKind.Core,
            MaxHp = 100f,
            Fate = PartFate.Destroyed,
            GrantedSkillIds = new[] { BiteId },
            BiomassValue = 20f,
            MaxUpgrade = 0,
            BiomassCost = 0f,
            Sockets = new[]
            {
                new SocketSlot(SocketKind.Head, 2),
                new SocketSlot(SocketKind.Limb, 2),
                new SocketSlot(SocketKind.Locomotion, 1),
                new SocketSlot(SocketKind.Hide, 1),
                new SocketSlot(SocketKind.Tail, 1),
            },
        };

        public static readonly BodyPartSpec Jaws = new BodyPartSpec
        {
            Id = JawsId,
            Name = "Jaws",
            Socket = SocketKind.Head,
            MaxHp = 20f,
            Fate = PartFate.Destroyed,
            BiomassValue = 4f,
            BiomassCost = 25f,
            SkillDamageBonusesPerLevel = new[] { new SkillBonus(BiteId, 0.25f) },
        };

        public static readonly BodyPartSpec Arm = new BodyPartSpec
        {
            Id = ArmId,
            Name = "Arm",
            Socket = SocketKind.Limb,
            MaxHp = 25f,
            Fate = PartFate.Severed,
            BiomassValue = 6f,
            BiomassCost = 30f,
            RepeatMinLevel = 3,
            GrantedSkillIds = new[] { ClawId, GrabId },
            StatBonusesPerLevel = new[] { new StatValue(StatIds.Strength, 1) },
        };

        public static readonly BodyPartSpec Legs = new BodyPartSpec
        {
            Id = LegsId,
            Name = "Legs",
            Socket = SocketKind.Locomotion,
            MaxHp = 30f,
            Fate = PartFate.Severed,
            BiomassValue = 8f,
            BiomassCost = 40f,
            MinLevel = 2,
            GrantedSkillIds = new[] { LungeId, SprintId },
            MoveSpeedBonus = 0.5f,
        };

        public static readonly BodyPartSpec ThickHide = new BodyPartSpec
        {
            Id = ThickHideId,
            Name = "Thick Hide",
            Socket = SocketKind.Hide,
            MaxHp = 40f,
            Defense = DefenseType.ThickHide,
            Fate = PartFate.Destroyed,
            BiomassValue = 4f,
            BiomassCost = 35f,
            StatBonusesPerLevel = new[] { new StatValue(StatIds.Constitution, 1) },
        };

        public static readonly BodyPartSpec Plates = new BodyPartSpec
        {
            Id = PlatesId,
            Name = "Plates",
            Socket = SocketKind.Hide,
            MaxHp = 50f,
            Defense = DefenseType.Plates,
            Fate = PartFate.Destroyed,
            BiomassValue = 4f,
            BiomassCost = 35f,
            RequiresUnlock = true,
        };

        public static readonly BodyPartSpec ElasticTissue = new BodyPartSpec
        {
            Id = ElasticTissueId,
            Name = "Elastic Tissue",
            Socket = SocketKind.Hide,
            MaxHp = 35f,
            Defense = DefenseType.ElasticTissue,
            Fate = PartFate.Destroyed,
            BiomassValue = 4f,
            BiomassCost = 35f,
            RequiresUnlock = true,
            StatBonusesPerLevel = new[] { new StatValue(StatIds.Agility, 1) },
        };

        public static readonly BodyPartSpec Eyes = new BodyPartSpec
        {
            Id = EyesId,
            Name = "Eyes",
            Socket = SocketKind.Head,
            MaxHp = 10f,
            Fate = PartFate.Destroyed,
            BiomassValue = 2f,
            BiomassCost = 20f,
            PerceptionBonus = 0.3f,
        };

        public static readonly BodyPartSpec Spines = new BodyPartSpec
        {
            Id = SpinesId,
            Name = "Spines",
            Socket = SocketKind.Hide,
            MaxHp = 30f,
            Fate = PartFate.Destroyed,
            BiomassValue = 3f,
            BiomassCost = 35f,
            RequiresUnlock = true,
            ReturnDamageFraction = 0.3f,
        };

        public static readonly BodyPartSpec Tail = new BodyPartSpec
        {
            Id = TailId,
            Name = "Tail",
            Socket = SocketKind.Tail,
            MaxHp = 25f,
            Fate = PartFate.Severed,
            BiomassValue = 6f,
            BiomassCost = 35f,
            MinLevel = 4,
            RequiresUnlock = true,
            GrantedSkillIds = new[] { TailSwingId },
        };

        /// <summary>Parts in creation order: a part that requires another comes after it.</summary>
        public static readonly IReadOnlyList<BodyPartSpec> Parts = new[] { Core, Jaws, Arm, Legs, ThickHide, Plates, ElasticTissue, Eyes, Spines, Tail };

        public static readonly EvolutionSpec Brute1 = new EvolutionSpec
        {
            Id = "evolution.brute.1",
            Name = "Brute",
            Description = "Mass and muscle. +10 Strength, Plates and Tail unlocked, a Thick Hide for free.",
            Stage = 1,
            StatPoints = 0,
            StatBonuses = new[] { new StatValue(StatIds.Strength, 10) },
            StatCapBonuses = new[] { new StatValue(StatIds.Strength, 5) },
            UnlockedPartIds = new[] { PlatesId, TailId },
            FreeMutationPartIds = new[] { ThickHideId },
            FitStat = StatIds.Strength,
        };

        public static readonly EvolutionSpec Stalker1 = new EvolutionSpec
        {
            Id = "evolution.stalker.1",
            Name = "Stalker",
            Description = "Speed and senses. +10 Agility, Spines unlocked, Legs for free.",
            Stage = 1,
            StatPoints = 0,
            StatBonuses = new[] { new StatValue(StatIds.Agility, 10) },
            StatCapBonuses = new[] { new StatValue(StatIds.Agility, 5) },
            UnlockedPartIds = new[] { SpinesId },
            FreeMutationPartIds = new[] { LegsId },
            FitStat = StatIds.Agility,
        };

        public static readonly EvolutionSpec Bulwark1 = new EvolutionSpec
        {
            Id = "evolution.bulwark.1",
            Name = "Bulwark",
            Description = "Endurance. +10 Constitution, Plates and Elastic Tissue unlocked, Elastic Tissue for free.",
            Stage = 1,
            StatPoints = 0,
            StatBonuses = new[] { new StatValue(StatIds.Constitution, 10) },
            StatCapBonuses = new[] { new StatValue(StatIds.Constitution, 5) },
            UnlockedPartIds = new[] { PlatesId, ElasticTissueId },
            FreeMutationPartIds = new[] { ElasticTissueId },
            FitStat = StatIds.Constitution,
        };

        public static readonly EvolutionSpec Brute2 = new EvolutionSpec
        {
            Id = "evolution.brute.2",
            Name = "Brute",
            Description = "The Brute grown. +10 Strength and +5 Constitution, Jaws for free.",
            Stage = 2,
            StatPoints = 0,
            StatBonuses = new[] { new StatValue(StatIds.Strength, 10), new StatValue(StatIds.Constitution, 5) },
            StatCapBonuses = new[] { new StatValue(StatIds.Strength, 5) },
            FreeMutationPartIds = new[] { JawsId },
            FitStat = StatIds.Strength,
        };

        public static readonly EvolutionSpec Stalker2 = new EvolutionSpec
        {
            Id = "evolution.stalker.2",
            Name = "Stalker",
            Description = "The Stalker grown. +10 Agility and +5 Strength, Eyes for free.",
            Stage = 2,
            StatPoints = 0,
            StatBonuses = new[] { new StatValue(StatIds.Agility, 10), new StatValue(StatIds.Strength, 5) },
            StatCapBonuses = new[] { new StatValue(StatIds.Agility, 5) },
            FreeMutationPartIds = new[] { EyesId },
            FitStat = StatIds.Agility,
        };

        public static readonly EvolutionSpec Bulwark2 = new EvolutionSpec
        {
            Id = "evolution.bulwark.2",
            Name = "Bulwark",
            Description = "The Bulwark grown. +10 Constitution and +5 Agility, Spines unlocked, Legs for free.",
            Stage = 2,
            StatPoints = 0,
            StatBonuses = new[] { new StatValue(StatIds.Constitution, 10), new StatValue(StatIds.Agility, 5) },
            StatCapBonuses = new[] { new StatValue(StatIds.Constitution, 5) },
            UnlockedPartIds = new[] { SpinesId },
            FreeMutationPartIds = new[] { LegsId },
            FitStat = StatIds.Constitution,
        };

        public static readonly IReadOnlyList<EvolutionSpec> Evolutions = new[] { Brute1, Stalker1, Bulwark1, Brute2, Stalker2, Bulwark2 };

        /// <summary>Where and how each part is drawn on the body capsule (capsule mesh units: two tall, radius a half).</summary>
        public static readonly IReadOnlyDictionary<string, PartVisual> Visuals = new Dictionary<string, PartVisual>
        {
            [CoreId] = new PartVisual(PartVisualKind.None, PartMaterialRole.Owner, Vector3.zero, Vector3.one, Vector3.zero, false),
            [JawsId] = new PartVisual(PartVisualKind.Cube, PartMaterialRole.Maw, new Vector3(0f, 0.2f, 0.5f), new Vector3(0.65f, 0.28f, 0.45f), Vector3.zero, false),
            [ArmId] = new PartVisual(PartVisualKind.Capsule, PartMaterialRole.Owner, new Vector3(0.62f, 0.1f, 0.15f), new Vector3(0.28f, 0.55f, 0.28f), new Vector3(0f, 0f, -30f), true),
            [LegsId] = new PartVisual(PartVisualKind.Cube, PartMaterialRole.Dark, new Vector3(0f, -0.95f, 0f), new Vector3(0.9f, 0.3f, 0.5f), Vector3.zero, false),
            [ThickHideId] = new PartVisual(PartVisualKind.Cube, PartMaterialRole.Dark, new Vector3(0f, 0.1f, -0.52f), new Vector3(0.8f, 1.2f, 0.22f), Vector3.zero, false),
            [PlatesId] = new PartVisual(PartVisualKind.Cube, PartMaterialRole.Plate, new Vector3(0f, 0.2f, -0.55f), new Vector3(0.9f, 1.0f, 0.25f), Vector3.zero, false),
            [ElasticTissueId] = new PartVisual(PartVisualKind.Capsule, PartMaterialRole.Owner, new Vector3(0f, 0f, -0.5f), new Vector3(0.7f, 0.9f, 0.35f), Vector3.zero, false),
            [EyesId] = new PartVisual(PartVisualKind.Sphere, PartMaterialRole.Eye, new Vector3(0f, 0.55f, 0.42f), new Vector3(0.5f, 0.18f, 0.18f), Vector3.zero, false),
            [SpinesId] = new PartVisual(PartVisualKind.Cube, PartMaterialRole.Plate, new Vector3(0f, 0.45f, -0.5f), new Vector3(0.6f, 0.7f, 0.3f), new Vector3(30f, 0f, 0f), false),
            [TailId] = new PartVisual(PartVisualKind.Capsule, PartMaterialRole.Owner, new Vector3(0f, -0.3f, -0.9f), new Vector3(0.2f, 0.7f, 0.2f), new Vector3(-60f, 0f, 0f), false),
        };

        /// <summary>One placeholder visual, ready to write into a part definition.</summary>
        /// <summary>
        /// Where parts plug into the placeholder capsule, in body units (1 is the body height, origin at the base), the
        /// first limb anchor on the right like the primitives. A core model with Socket_ transforms replaces them.
        /// </summary>
        public static readonly IReadOnlyList<SocketAnchor> DefaultAnchors = new[]
        {
            new SocketAnchor(SocketKind.Head, new Vector3(0f, 0.62f, 0.28f), Vector3.zero),
            new SocketAnchor(SocketKind.Limb, new Vector3(0.29f, 0.52f, 0.06f), new Vector3(0f, 90f, 0f)),
            new SocketAnchor(SocketKind.Limb, new Vector3(-0.29f, 0.52f, 0.06f), new Vector3(0f, -90f, 0f)),
            new SocketAnchor(SocketKind.Locomotion, new Vector3(0f, 0.04f, 0f), new Vector3(90f, 0f, 0f)),
            new SocketAnchor(SocketKind.Hide, new Vector3(0f, 0.55f, -0.28f), new Vector3(0f, 180f, 0f)),
            new SocketAnchor(SocketKind.Tail, new Vector3(0f, 0.3f, -0.28f), new Vector3(25f, 180f, 0f)),
        };

        /// <summary>How the view moves each placeholder part (D-082); hides, eyes, plates and spines sit still.</summary>
        public static PartMotion MotionFor(string partId)
        {
            switch (partId)
            {
                case JawsId:
                    return PartMotion.Jaws;
                case ArmId:
                    return PartMotion.Limb;
                case LegsId:
                    return PartMotion.Legs;
                case TailId:
                    return PartMotion.Tail;
                default:
                    return PartMotion.None;
            }
        }

        /// <summary>How the view animates each placeholder skill (D-082); sprint has no motion of its own.</summary>
        public static SkillMotion SkillMotionFor(string skillId)
        {
            switch (skillId)
            {
                case BiteId:
                    return SkillMotion.Bite;
                case ClawId:
                case GrabId:
                    return SkillMotion.Swipe;
                case LungeId:
                    return SkillMotion.Lunge;
                case TailSwingId:
                    return SkillMotion.TailSwing;
                default:
                    return SkillMotion.None;
            }
        }

        /// <summary>Default shift of a bound mesh from its anchor in body units; eyes sit above the jaws that share the head socket.</summary>
        public static Vector3 MeshOffsetFor(string partId)
        {
            return partId == EyesId ? new Vector3(0f, 0.12f, 0.02f) : Vector3.zero;
        }

        internal readonly struct SocketAnchor
        {
            public SocketAnchor(SocketKind kind, Vector3 position, Vector3 euler)
            {
                Kind = kind;
                Position = position;
                Euler = euler;
            }

            public SocketKind Kind { get; }

            public Vector3 Position { get; }

            public Vector3 Euler { get; }
        }

        internal readonly struct PartVisual
        {
            public PartVisual(PartVisualKind kind, PartMaterialRole material, Vector3 position, Vector3 scale, Vector3 euler, bool mirrorSecondCopy)
            {
                Kind = kind;
                Material = material;
                Position = position;
                Scale = scale;
                Euler = euler;
                MirrorSecondCopy = mirrorSecondCopy;
            }

            public PartVisualKind Kind { get; }

            public PartMaterialRole Material { get; }

            public Vector3 Position { get; }

            public Vector3 Scale { get; }

            public Vector3 Euler { get; }

            public bool MirrorSecondCopy { get; }
        }
    }
}
