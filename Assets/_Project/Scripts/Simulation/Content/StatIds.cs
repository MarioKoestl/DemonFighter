#nullable enable
using System.Collections.Generic;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// The three v1 base stats (D-020) as well-known ids. The formulas that give each stat its effect live in the
    /// derived stats; content refers to stats by these ids.
    /// </summary>
    public static class StatIds
    {
        public static readonly StatId Strength = new StatId("stat.strength");
        public static readonly StatId Constitution = new StatId("stat.constitution");
        public static readonly StatId Agility = new StatId("stat.agility");

        /// <summary>Every v1 stat, in display order.</summary>
        public static readonly IReadOnlyList<StatId> All = new[] { Strength, Constitution, Agility };
    }
}
