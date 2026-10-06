#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Immutable description of one body part kind (ARCHITECTURE, "Specs"): where it plugs in, how much it takes,
    /// what armor it is, what it grants and what it is worth as food. Created from a BodyPartDefinition asset.
    /// </summary>
    public sealed record BodyPartSpec
    {
        /// <summary>Stable content id, lowercase and dotted, for example part.core.</summary>
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public SocketKind Socket { get; init; } = SocketKind.Limb;

        /// <summary>Hit points before Constitution scaling.</summary>
        public float MaxHp { get; init; } = 20f;

        /// <summary>Armor material of hide parts; None for everything else.</summary>
        public DefenseType Defense { get; init; } = DefenseType.None;

        /// <summary>Severed or destroyed at zero HP; ignored for the core, whose zero is death.</summary>
        public PartFate Fate { get; init; } = PartFate.Severed;

        /// <summary>Skills this part gives its owner while attached.</summary>
        public IReadOnlyList<string> GrantedSkillIds { get; init; } = Array.Empty<string>();

        /// <summary>Biomass a severed instance of this part holds as food, before tier scaling.</summary>
        public float BiomassValue { get; init; } = 5f;

        /// <summary>Sockets this part exposes for other parts; the core has them, limbs have none in v1.</summary>
        public IReadOnlyList<SocketSlot> Sockets { get; init; } = Array.Empty<SocketSlot>();

        /// <summary>Highest upgrade level (+1 to +5 by design); zero for parts that cannot be upgraded, such as the core.</summary>
        public int MaxUpgrade { get; init; } = 5;

        /// <summary>Stat points the part contributes per level (upgrade level plus one) while attached.</summary>
        public IReadOnlyList<StatValue> StatBonusesPerLevel { get; init; } = Array.Empty<StatValue>();

        /// <summary>Damage bonuses the part gives skills per level while attached; Jaws for Bite.</summary>
        public IReadOnlyList<SkillBonus> SkillDamageBonusesPerLevel { get; init; } = Array.Empty<SkillBonus>();

        /// <summary>Fraction added to movement speed while attached; legs replace crawling with walking.</summary>
        public float MoveSpeedBonus { get; init; }

        /// <summary>Fraction added to perception radius and aim distance while attached; eyes.</summary>
        public float PerceptionBonus { get; init; }

        /// <summary>Share of every melee hit taken that is dealt back to the attacker as Pierce; Spines (D-058).</summary>
        public float ReturnDamageFraction { get; init; }

        /// <summary>Biomass the first copy costs; repeats, upgrades and regrows derive from it through the tuning.</summary>
        public float BiomassCost { get; init; } = 30f;

        /// <summary>Character level needed to attach the first copy.</summary>
        public int MinLevel { get; init; } = 1;

        /// <summary>Character level needed for every further copy, never below MinLevel; a second arm comes later than the first.</summary>
        public int RepeatMinLevel { get; init; } = 1;

        /// <summary>Fraction the part adds to the body's size while it is attached and not lost (D-091); 0.1 is a tenth bigger.</summary>
        public float SizeBonus { get; init; }

        /// <summary>Part kinds that must be attached before this one.</summary>
        public IReadOnlyList<string> RequiredPartIds { get; init; } = Array.Empty<string>();

        /// <summary>True when an evolution has to unlock the part before it appears as affordable.</summary>
        public bool RequiresUnlock { get; init; }

        /// <summary>True for the root part whose loss kills the demon.</summary>
        public bool IsCore => Socket == SocketKind.Core;

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            Require(!string.IsNullOrWhiteSpace(Id), "Id is required.");
            Require(!string.IsNullOrWhiteSpace(Name), "Name is required.");
            Require(MaxHp > 0f, "MaxHp must be positive.");
            Require(BiomassValue >= 0f, "BiomassValue is never negative.");
            Require(SizeBonus >= 0f, "SizeBonus is never negative.");
            Require(GrantedSkillIds != null, "GrantedSkillIds must not be null.");
            Require(Socket == SocketKind.Hide || Defense == DefenseType.None, "Only hide parts carry a defense type.");
            Require(Sockets != null && StatBonusesPerLevel != null && SkillDamageBonusesPerLevel != null, "Bonus lists must not be null.");
            Require(MaxUpgrade >= 0, "MaxUpgrade is never negative.");
            Require(MoveSpeedBonus > -1f, "MoveSpeedBonus must stay above -1.");
            Require(PerceptionBonus >= 0f, "PerceptionBonus is never negative.");
            Require(ReturnDamageFraction >= 0f && ReturnDamageFraction <= 1f, "ReturnDamageFraction must be in [0, 1].");
            Require(BiomassCost >= 0f, "BiomassCost is never negative.");
            Require(MinLevel >= 1 && RepeatMinLevel >= 1, "Level requirements start at 1.");
            Require(RequiredPartIds != null, "RequiredPartIds must not be null.");
        }

        private void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new ContentException("Body part " + Id + ": " + message);
            }
        }
    }
}
