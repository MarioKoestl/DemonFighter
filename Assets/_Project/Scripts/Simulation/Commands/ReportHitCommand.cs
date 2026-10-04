#nullable enable
namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Unity detected that the actor's active skill touched a part of another demon (ARCHITECTURE, "Movement,
    /// collision and hits"). The simulation decides whether the hit counts and what it does.
    /// </summary>
    public readonly struct ReportHitCommand : ICommand
    {
        public ReportHitCommand(DemonId actor, DemonId target, int partIndex)
        {
            Actor = actor;
            Target = target;
            PartIndex = partIndex;
        }

        /// <inheritdoc />
        public DemonId Actor { get; }

        /// <summary>The demon that was touched.</summary>
        public DemonId Target { get; }

        /// <summary>Index of the touched part in the target's body.</summary>
        public int PartIndex { get; }
    }
}
