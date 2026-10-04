#nullable enable
using DemonFighter.Simulation.Commands;

namespace DemonFighter.Simulation.Mutation
{
    /// <summary>A confirmed line of the Mutate tab: attach a part kind, upgrade or regrow a part of the body.</summary>
    public readonly struct MutateCommand : ICommand
    {
        public MutateCommand(DemonId actor, MutationKind kind, string partId, int partIndex)
        {
            Actor = actor;
            Kind = kind;
            PartId = partId;
            PartIndex = partIndex;
        }

        /// <inheritdoc />
        public DemonId Actor { get; }

        public MutationKind Kind { get; }

        /// <summary>Content id of the part kind to attach; ignored for upgrades and regrows.</summary>
        public string PartId { get; }

        /// <summary>Index of the body part to upgrade or regrow; ignored for attaching.</summary>
        public int PartIndex { get; }

        public static MutateCommand Attach(DemonId actor, string partId)
        {
            return new MutateCommand(actor, MutationKind.Attach, partId, -1);
        }

        public static MutateCommand Upgrade(DemonId actor, int partIndex)
        {
            return new MutateCommand(actor, MutationKind.Upgrade, string.Empty, partIndex);
        }

        public static MutateCommand Regrow(DemonId actor, int partIndex)
        {
            return new MutateCommand(actor, MutationKind.Regrow, string.Empty, partIndex);
        }
    }
}
