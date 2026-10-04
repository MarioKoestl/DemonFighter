#nullable enable
using System;

namespace DemonFighter.Simulation.Content
{
    /// <summary>One stat with a point value, used for the starting stats of a demon kind.</summary>
    public readonly struct StatValue
    {
        public StatValue(StatId stat, int value)
        {
            if (!stat.IsValid)
            {
                throw new ArgumentException("A stat value needs a stat id.", nameof(stat));
            }

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Stat points are never negative.");
            }

            Stat = stat;
            Value = value;
        }

        public StatId Stat { get; }

        public int Value { get; }
    }
}
