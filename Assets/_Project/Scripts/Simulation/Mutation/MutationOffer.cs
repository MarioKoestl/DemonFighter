#nullable enable
using System;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Mutation
{
    /// <summary>
    /// One line of the Mutate tab: what it is, what it costs and whether the demon may take it right now, with the
    /// reason when it may not. Offers are built fresh from the rules whenever the menu asks.
    /// </summary>
    public sealed record MutationOffer
    {
        public MutationOffer(MutationKind kind, BodyPartSpec part, int partIndex, float cost, bool available, string reason)
        {
            Kind = kind;
            Part = part ?? throw new ArgumentNullException(nameof(part));
            PartIndex = partIndex;
            Cost = cost;
            Available = available;
            Reason = reason ?? string.Empty;
        }

        public MutationKind Kind { get; }

        /// <summary>The part kind attached, upgraded or regrown.</summary>
        public BodyPartSpec Part { get; }

        /// <summary>Index of the body part for upgrades and regrows; -1 for a new part.</summary>
        public int PartIndex { get; }

        /// <summary>Biomass the offer costs.</summary>
        public float Cost { get; }

        /// <summary>True when the demon may confirm it right now.</summary>
        public bool Available { get; }

        /// <summary>Why it is not available; empty when it is.</summary>
        public string Reason { get; }

        /// <summary>The command that takes this offer.</summary>
        public MutateCommand ToCommand(DemonId actor)
        {
            return new MutateCommand(actor, Kind, Part.Id, PartIndex);
        }
    }
}
