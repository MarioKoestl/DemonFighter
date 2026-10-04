#nullable enable
using System;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Progression
{
    /// <summary>
    /// The one place character XP is handed out: it goes onto the demon, which turns it into levels and stat points
    /// (D-037), and both the gain and any new level leave as events for the HUD.
    /// </summary>
    internal static class XpSystem
    {
        /// <summary>Gives XP to a living demon; nothing happens for a corpse or a zero amount.</summary>
        public static void Grant(Demon demon, float amount, XpSource source, CombatTuning tuning, SimulationEvents events)
        {
            if (demon == null)
            {
                throw new ArgumentNullException(nameof(demon));
            }

            if (amount <= 0f || !demon.IsAlive)
            {
                return;
            }

            int levels = demon.GainXp(amount, tuning);
            events.Publish(new XpGained(demon.Id, amount, source));
            if (levels > 0)
            {
                events.Publish(new LevelUp(demon.Id, demon.Level, demon.Stats.UnspentPoints));
            }
        }
    }
}
