#nullable enable
using System;
using System.Collections.Generic;

namespace DemonFighter.Simulation.Playtest
{
    /// <summary>
    /// Test mode (D-089), a playtest setting: a demon in it takes no damage (the damage system skips it) and never
    /// runs short of Biomass or stat points, both kept at 1000 or more. Runs right after commands are applied, in a
    /// tick and in a paused menu, so whatever a mutation or a stat point spent is back before anyone looks.
    /// </summary>
    internal static class TestModeSystem
    {
        /// <summary>Biomass a demon in test mode never falls below.</summary>
        internal const float Biomass = 1000f;

        /// <summary>Unspent stat points a demon in test mode never falls below.</summary>
        internal const int StatPoints = 1000;

        public static void Refill(RunState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (demon.InTestMode && demon.IsAlive)
                {
                    demon.TopUp(Biomass, StatPoints);
                }
            }
        }
    }
}
