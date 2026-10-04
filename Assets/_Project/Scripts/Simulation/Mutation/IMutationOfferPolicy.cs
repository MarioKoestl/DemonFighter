#nullable enable
using System.Collections.Generic;

namespace DemonFighter.Simulation.Mutation
{
    /// <summary>
    /// Which mutations the menu lists (O-001): the shop lists everything, the random policy offers a few. Both read
    /// the same rules, so a listed offer is never one the simulation would refuse for a reason the list did not show.
    /// </summary>
    public interface IMutationOfferPolicy
    {
        /// <summary>The offers for this demon right now, in display order.</summary>
        IReadOnlyList<MutationOffer> Offers(RunState state, Demon demon);
    }
}
