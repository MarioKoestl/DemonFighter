#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Food
{
    /// <summary>
    /// Moves Biomass from food into the demons holding Eat on it, at the tuning rate scaled by the tier gap
    /// (GAME_DESIGN, "Reward scaling"). A tick without an accepted eat command, a hit from an attacker, a started
    /// skill or food that is gone ends the meal; food eaten to the last bit leaves the world.
    /// </summary>
    internal static class EatingSystem
    {
        /// <summary>Applies one tick of eating for every demon that asked for it this tick.</summary>
        public static void Advance(RunState state, SimulationEvents events, float seconds)
        {
            CombatTuning tuning = state.Catalog.Tuning;
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (!demon.IsAlive)
                {
                    continue;
                }

                if (demon.EatRequestTick != state.Tick)
                {
                    demon.StopEating();
                    continue;
                }

                if (!demon.IsEating)
                {
                    // Asked this tick but interrupted since by a hit or a skill; the next held tick resumes.
                    continue;
                }

                if (!state.TryGetFood(demon.EatingFoodId, out FoodItem? food))
                {
                    demon.StopEating();
                    continue;
                }

                float taken = food.Take(tuning.EatBiomassPerSecond * seconds);
                float gained = taken * tuning.RewardFactor(demon.Tier, food.SourceTier);
                demon.GainBiomass(gained);
                events.Publish(new FoodConsumed(demon.Id, food.Id, gained));
                if (food.IsDepleted)
                {
                    state.RemoveFood(food.Id);
                    events.Publish(new FoodRemoved(food.Id, FoodRemovalReason.Eaten));
                    demon.StopEating();
                }
            }
        }
    }
}
