#nullable enable
using System.Collections.Generic;

namespace DemonFighter.Simulation.Movement
{
    /// <summary>
    /// Moves every demon by its intent for one step. This is the simulation's own prediction, so tests and a headless
    /// server move entities without Unity; in the game the view moves the body with collisions and writes the real
    /// pose back (ARCHITECTURE, "Movement, collision and hits").
    /// </summary>
    internal static class MovementSystem
    {
        /// <summary>Integrates position and facing of every demon that no Unity body moves.</summary>
        public static void Advance(RunState state, float tickSeconds)
        {
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (!demon.HasBody)
                {
                    demon.Integrate(tickSeconds);
                }
            }
        }
    }
}
