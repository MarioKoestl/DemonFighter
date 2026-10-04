#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Mutation
{
    /// <summary>
    /// Option A from O-001, the v1 default: every part kind of the catalog as an attach offer, an upgrade offer for
    /// every attached part that can still grow, a regrow offer for every lost part. Unavailable offers stay listed
    /// with their reason, so the player can plan.
    /// </summary>
    public sealed class ShopOfferPolicy : IMutationOfferPolicy
    {
        /// <inheritdoc />
        public IReadOnlyList<MutationOffer> Offers(RunState state, Demon demon)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (demon == null)
            {
                throw new ArgumentNullException(nameof(demon));
            }

            var offers = new List<MutationOffer>();
            foreach (BodyPartSpec spec in state.Catalog.BodyParts)
            {
                if (spec.IsCore)
                {
                    continue;
                }

                bool available = MutationRules.CanAttach(demon, spec, state, out string reason, out float cost);
                offers.Add(new MutationOffer(MutationKind.Attach, spec, -1, cost, available, reason));
            }

            IReadOnlyList<BodyPart> parts = demon.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                if (part.Spec.IsCore)
                {
                    continue;
                }

                if (part.IsLost)
                {
                    bool canRegrow = MutationRules.CanRegrow(demon, part, state, out string regrowReason, out float regrowCost);
                    offers.Add(new MutationOffer(MutationKind.Regrow, part.Spec, part.Index, regrowCost, canRegrow, regrowReason));
                }
                else if (part.UpgradeLevel < part.Spec.MaxUpgrade)
                {
                    bool canUpgrade = MutationRules.CanUpgrade(demon, part, state, out string upgradeReason, out float upgradeCost);
                    offers.Add(new MutationOffer(MutationKind.Upgrade, part.Spec, part.Index, upgradeCost, canUpgrade, upgradeReason));
                }
            }

            return offers;
        }
    }
}
