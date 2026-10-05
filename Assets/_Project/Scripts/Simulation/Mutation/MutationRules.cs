#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Mutation
{
    /// <summary>
    /// Who may buy what (GAME_DESIGN, "Mutation"; D-014): alive, not transforming, the part unlocked, the level
    /// reached, the required parts attached, a socket free and the Biomass there; the out-of-combat rule is suspended
    /// for the playtest (D-060). Every check names its reason, so the menu can say why a button is disabled. Costs
    /// come from the part and the tuning: repeats cost more, upgrades and regrows a fraction per level.
    /// </summary>
    public static class MutationRules
    {
        public const string Dead = "Dead";
        public const string Transforming = "Still transforming";
        public const string Locked = "Needs an evolution that unlocks it";
        public const string LevelTooLow = "Level too low";
        public const string MissingPart = "Needs another part first";
        public const string NoSocket = "No free socket";
        public const string NotEnoughBiomass = "Not enough Biomass";
        public const string AlreadyMax = "Already at the highest upgrade";
        public const string PartLost = "Part is lost; regrow it first";
        public const string NotLost = "Part is not lost";
        public const string IsCore = "The core cannot be bought, upgraded or regrown";

        /// <summary>True when the demon may confirm any mutation right now.</summary>
        public static bool CanMutateNow(Demon demon, RunState state, out string reason)
        {
            if (!demon.IsAlive)
            {
                reason = Dead;
                return false;
            }

            // Mutations applied together arrive in one tick and reshape the body once (D-064).
            if (demon.IsTransforming(state.Tick) && demon.TransformationStartedTick != state.Tick)
            {
                reason = Transforming;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>Biomass a new copy of the part costs; a second arm costs more than the first.</summary>
        public static float AttachCost(Demon demon, BodyPartSpec spec, CombatTuning tuning)
        {
            return spec.BiomassCost * (CountCopies(demon, spec) > 0 ? tuning.RepeatCostMultiplier : 1f);
        }

        /// <summary>Biomass the next upgrade level of the part costs.</summary>
        public static float UpgradeCost(BodyPart part, CombatTuning tuning)
        {
            return part.Spec.BiomassCost * tuning.UpgradeCostFraction * (part.UpgradeLevel + 1);
        }

        /// <summary>Biomass regrowing the part costs.</summary>
        public static float RegrowCost(BodyPart part, CombatTuning tuning)
        {
            return part.Spec.BiomassCost * tuning.RegrowCostFraction;
        }

        /// <summary>Progress level (over every tier, D-091) the next upgrade of the part needs.</summary>
        public static int UpgradeLevelRequirement(BodyPart part, CombatTuning tuning)
        {
            return (part.UpgradeLevel + 1) * tuning.CharacterLevelPerUpgradeLevel;
        }

        /// <summary>True when the demon may attach a new copy of the part now; the first failing rule is the reason.</summary>
        public static bool CanAttach(Demon demon, BodyPartSpec spec, RunState state, out string reason, out float cost)
        {
            CombatTuning tuning = state.Catalog.Tuning;
            cost = AttachCost(demon, spec, tuning);
            if (spec.IsCore)
            {
                reason = IsCore;
                return false;
            }

            if (!CanMutateNow(demon, state, out reason))
            {
                return false;
            }

            if (!demon.IsUnlocked(spec))
            {
                reason = Locked;
                return false;
            }

            int minLevel = CountCopies(demon, spec) > 0 ? Math.Max(spec.MinLevel, spec.RepeatMinLevel) : spec.MinLevel;
            if (demon.ProgressLevel < minLevel)
            {
                reason = LevelTooLow;
                return false;
            }

            if (!HasRequiredParts(demon, spec))
            {
                reason = MissingPart;
                return false;
            }

            if (!demon.Body.CanAttach(spec, out _))
            {
                reason = NoSocket;
                return false;
            }

            if (demon.Biomass < cost)
            {
                reason = NotEnoughBiomass;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>True when the demon may upgrade the part now.</summary>
        public static bool CanUpgrade(Demon demon, BodyPart part, RunState state, out string reason, out float cost)
        {
            CombatTuning tuning = state.Catalog.Tuning;
            cost = UpgradeCost(part, tuning);
            if (part.Spec.IsCore)
            {
                reason = IsCore;
                return false;
            }

            if (!CanMutateNow(demon, state, out reason))
            {
                return false;
            }

            if (part.IsLost)
            {
                reason = PartLost;
                return false;
            }

            if (part.UpgradeLevel >= part.Spec.MaxUpgrade)
            {
                reason = AlreadyMax;
                return false;
            }

            if (demon.ProgressLevel < UpgradeLevelRequirement(part, tuning))
            {
                reason = LevelTooLow;
                return false;
            }

            if (demon.Biomass < cost)
            {
                reason = NotEnoughBiomass;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>True when the demon may regrow the part now.</summary>
        public static bool CanRegrow(Demon demon, BodyPart part, RunState state, out string reason, out float cost)
        {
            CombatTuning tuning = state.Catalog.Tuning;
            cost = RegrowCost(part, tuning);
            if (part.Spec.IsCore)
            {
                reason = IsCore;
                return false;
            }

            if (!CanMutateNow(demon, state, out reason))
            {
                return false;
            }

            if (!part.IsLost)
            {
                reason = NotLost;
                return false;
            }

            if (demon.Biomass < cost)
            {
                reason = NotEnoughBiomass;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>How many copies of the part the body holds, lost or not.</summary>
        public static int CountCopies(Demon demon, BodyPartSpec spec)
        {
            int copies = 0;
            IReadOnlyList<BodyPart> parts = demon.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (string.Equals(parts[i].Spec.Id, spec.Id, StringComparison.Ordinal))
                {
                    copies++;
                }
            }

            return copies;
        }

        private static bool HasRequiredParts(Demon demon, BodyPartSpec spec)
        {
            IReadOnlyList<string> required = spec.RequiredPartIds;
            for (int r = 0; r < required.Count; r++)
            {
                if (!HasAttached(demon, required[r]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasAttached(Demon demon, string partId)
        {
            IReadOnlyList<BodyPart> parts = demon.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (!parts[i].IsLost && string.Equals(parts[i].Spec.Id, partId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
