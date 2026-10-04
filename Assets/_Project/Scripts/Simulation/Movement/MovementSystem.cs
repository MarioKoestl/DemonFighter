#nullable enable
using System.Collections.Generic;

namespace DemonFighter.Simulation.Movement
{
    /// <summary>
    /// Moves every demon by its intent for one step. This is the simulation's own prediction, so tests and a headless
    /// server move entities without Unity; in the game the view moves the body with collisions and writes the real
    /// pose back (ARCHITECTURE, "Movement, collision and hits"). Pushes from dashes and knockbacks expire here for
    /// every demon, body or not, so the view never keeps pushing after the simulation stopped.
    /// </summary>
    internal static class MovementSystem
    {
        /// <summary>Expires finished pushes, then integrates position and facing of every demon that no Unity body moves.</summary>
        public static void Advance(RunState state, float tickSeconds)
        {
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (demon.ExternalVelocityUntilTick >= 0 && state.Tick >= demon.ExternalVelocityUntilTick)
                {
                    demon.ClearExternalVelocity();
                }

                if (demon.IsAlive && !demon.HasBody)
                {
                    demon.Integrate(tickSeconds);
                }
            }
        }
    }
}
