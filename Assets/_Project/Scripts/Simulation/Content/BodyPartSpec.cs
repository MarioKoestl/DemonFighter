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

        /// <summary>True for the root part whose loss kills the demon.</summary>
        public bool IsCore => Socket == SocketKind.Core;

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            Require(!string.IsNullOrWhiteSpace(Id), "Id is required.");
            Require(!string.IsNullOrWhiteSpace(Name), "Name is required.");
            Require(MaxHp > 0f, "MaxHp must be positive.");
            Require(BiomassValue >= 0f, "BiomassValue is never negative.");
            Require(GrantedSkillIds != null, "GrantedSkillIds must not be null.");
            Require(Socket == SocketKind.Hide || Defense == DefenseType.None, "Only hide parts carry a defense type.");
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
