#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Hazards;

namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// What an AI demon notices within its perception radius (GAME_DESIGN, "AI demons"): prey worth the reward and at
    /// most one tier above, food, and fights to run from. Pure distance checks over the run state, so every demon
    /// perceives the same world and the run stays deterministic.
    /// </summary>
    internal static class Perception
    {
        /// <summary>Score bonus for prey that is already wounded.</summary>
        public const float WoundedBonus = 0.5f;

        /// <summary>Score bonus for prey that is busy eating.</summary>
        public const float EatingBonus = 0.5f;

        /// <summary>How far beyond the perception radius a chase or a walk to food continues before it is dropped.</summary>
        public const float LeashFactor = 1.5f;

        /// <summary>
        /// The best prey in range with its score, or null. Prey more than one tier above is avoided; prey two or more
        /// tiers below is ignored unless it attacked us recently, which is how elders ignore blobs until bitten.
        /// </summary>
        /// <summary>The living player demon, or null; the route pull of patrolling archetypes aims at it (D-070).</summary>
        public static Demon? FindPlayer(RunState state)
        {
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                if (demons[i].Controller == ControllerKind.Player && demons[i].IsAlive)
                {
                    return demons[i];
                }
            }

            return null;
        }

        public static Demon? FindPrey(RunState state, Demon self, ArchetypeSpec archetype, out float score)
        {
            CombatTuning tuning = state.Catalog.Tuning;
            int combatWindow = state.Config.TicksFor(tuning.InCombatSeconds);
            Demon? best = null;
            score = 0f;
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon other = demons[i];
                if (other == self || !other.IsAlive)
                {
                    continue;
                }

                int gap = other.Tier - self.Tier;
                if (gap > 1)
                {
                    continue;
                }

                bool provoked = self.LastAttackedBy == other.Id && self.WasAttackedWithin(state.Tick, combatWindow);
                if (gap < -1 && !provoked)
                {
                    continue;
                }

                float distance = PlanarDistance(self.Position, other.Position);
                if (distance > Radius(self, archetype))
                {
                    continue;
                }

                float preference = 1f + (IsWounded(other) ? WoundedBonus : 0f) + (other.IsEating ? EatingBonus : 0f);
                // Being attacked makes prey worth hunting whatever the reward says: elders fight back when bitten.
                float reward = tuning.RewardFactor(self.Tier, other.Tier);
                if (provoked)
                {
                    reward = MathF.Max(1f, reward);
                }

                float candidate = reward * preference * Proximity(distance, Radius(self, archetype));
                if (candidate > score)
                {
                    score = candidate;
                    best = other;
                }
            }

            return best;
        }

        /// <summary>The most attractive food in range with its score, or null.</summary>
        public static FoodItem? FindFood(RunState state, Demon self, ArchetypeSpec archetype, out float score)
        {
            CombatTuning tuning = state.Catalog.Tuning;
            FoodItem? best = null;
            score = 0f;
            IReadOnlyList<FoodItem> food = state.Food;
            for (int i = 0; i < food.Count; i++)
            {
                FoodItem item = food[i];

                // Food lying in lava or a fissure is out of reach for a demon that will not walk into fire (D-086).
                if (item.IsDepleted || state.Hazards.KindAt(item.Position) != HazardKind.None)
                {
                    continue;
                }

                float distance = PlanarDistance(self.Position, item.Position);
                if (distance > Radius(self, archetype))
                {
                    continue;
                }

                float candidate = tuning.RewardFactor(self.Tier, item.SourceTier) * Proximity(distance, Radius(self, archetype));
                if (candidate > score)
                {
                    score = candidate;
                    best = item;
                }
            }

            return best;
        }

        /// <summary>The nearest demon in range that is in a fight and not far below us, or null.</summary>
        public static Demon? FindThreat(RunState state, Demon self, ArchetypeSpec archetype)
        {
            int combatWindow = state.Config.TicksFor(state.Catalog.Tuning.InCombatSeconds);
            Demon? nearest = null;
            float nearestDistance = float.MaxValue;
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon other = demons[i];
                if (other == self || !other.IsAlive || other.Tier < self.Tier - 1 || !other.IsInCombat(state.Tick, combatWindow))
                {
                    continue;
                }

                float distance = PlanarDistance(self.Position, other.Position);
                if (distance <= Radius(self, archetype) && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = other;
                }
            }

            return nearest;
        }

        /// <summary>The demon that hit us within the combat window and is still alive and near enough to chase, or null.</summary>
        public static Demon? FindAttacker(RunState state, Demon self, ArchetypeSpec archetype)
        {
            if (!self.LastAttackedBy.IsValid)
            {
                return null;
            }

            int combatWindow = state.Config.TicksFor(state.Catalog.Tuning.InCombatSeconds);
            if (!self.WasAttackedWithin(state.Tick, combatWindow) || !state.TryGetDemon(self.LastAttackedBy, out Demon? attacker) || !attacker.IsAlive)
            {
                return null;
            }

            return PlanarDistance(self.Position, attacker.Position) <= Radius(self, archetype) * LeashFactor ? attacker : null;
        }

        /// <summary>True when any part is wounded or lost.</summary>
        public static bool IsWounded(Demon demon)
        {
            IReadOnlyList<BodyPart> parts = demon.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Condition != PartCondition.Healthy)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Ground-plane distance between two positions.</summary>
        public static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        // Sensory parts widen what the archetype allows (Eyes).
        private static float Radius(Demon self, ArchetypeSpec archetype)
        {
            return archetype.PerceptionRadius * (1f + self.Body.PerceptionBonus);
        }

        // Nearer things score higher: 1 at zero distance, 0.5 at the edge of perception.
        private static float Proximity(float distance, float radius)
        {
            return 1f - 0.5f * (distance / radius);
        }
    }
}
