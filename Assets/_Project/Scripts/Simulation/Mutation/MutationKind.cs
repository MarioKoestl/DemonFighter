#nullable enable
namespace DemonFighter.Simulation.Mutation
{
    /// <summary>The three things Biomass buys in the Mutate tab (GAME_DESIGN, "Mutation").</summary>
    public enum MutationKind
    {
        /// <summary>A new part into a free socket.</summary>
        Attach,

        /// <summary>One upgrade level on an attached part.</summary>
        Upgrade,

        /// <summary>A lost part back in its socket.</summary>
        Regrow,
    }
}
