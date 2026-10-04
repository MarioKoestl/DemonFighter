#nullable enable
using System;
using System.Collections.Generic;

namespace DemonFighter.Simulation.Mutation
{
    /// <summary>
    /// Option B from O-001, stubbed for the M4 playtest: a handful of the available attach offers, picked by the run
    /// seed and the character level so the same demon sees the same hand until it levels, without consuming the run
    /// Rng. Upgrades and regrows are always listed; a roguelike hand should never hide repairing what you own.
    /// </summary>
    public sealed class RandomOfferPolicy : IMutationOfferPolicy
    {
        private readonly ShopOfferPolicy _shop = new ShopOfferPolicy();

        public RandomOfferPolicy(int handSize = 3)
        {
            if (handSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(handSize), handSize, "A hand holds at least one offer.");
            }

            HandSize = handSize;
        }

        /// <summary>How many attach offers one hand shows.</summary>
        public int HandSize { get; }

        /// <inheritdoc />
        public IReadOnlyList<MutationOffer> Offers(RunState state, Demon demon)
        {
            IReadOnlyList<MutationOffer> all = _shop.Offers(state, demon);
            var attachable = new List<MutationOffer>();
            var result = new List<MutationOffer>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Kind == MutationKind.Attach)
                {
                    if (all[i].Available)
                    {
                        attachable.Add(all[i]);
                    }
                }
                else
                {
                    result.Add(all[i]);
                }
            }

            int start = attachable.Count == 0 ? 0 : Math.Abs(HashCode.Combine(state.Seed, demon.Level, demon.Id.Value)) % attachable.Count;
            for (int i = 0; i < Math.Min(HandSize, attachable.Count); i++)
            {
                result.Insert(i, attachable[(start + i) % attachable.Count]);
            }

            return result;
        }
    }
}
