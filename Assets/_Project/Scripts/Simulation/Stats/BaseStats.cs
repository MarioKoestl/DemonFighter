#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Stats
{
    /// <summary>
    /// A demon's allocated base stat points and the points still unspent (D-020, D-037). Stats are addressed by id,
    /// stored in two small arrays, so reads allocate nothing. Changed only by the simulation on level-up and spending.
    /// </summary>
    public sealed class BaseStats
    {
        private readonly StatId[] _ids;
        private readonly int[] _values;

        /// <summary>Starts every stat of the given list at zero.</summary>
        public BaseStats(IReadOnlyList<StatSpec> stats)
        {
            if (stats == null || stats.Count == 0)
            {
                throw new ArgumentException("At least one stat is required.", nameof(stats));
            }

            _ids = new StatId[stats.Count];
            _values = new int[stats.Count];
            for (int i = 0; i < stats.Count; i++)
            {
                _ids[i] = stats[i].Id;
            }
        }

        // Copy for previews; shares nothing with the source.
        private BaseStats(BaseStats source)
        {
            _ids = (StatId[])source._ids.Clone();
            _values = (int[])source._values.Clone();
            UnspentPoints = source.UnspentPoints;
        }

        /// <summary>The stats this block knows, in display order.</summary>
        public IReadOnlyList<StatId> Ids => _ids;

        /// <summary>Points granted by levels and not yet allocated.</summary>
        public int UnspentPoints { get; private set; }

        /// <summary>True when the stat exists in this block.</summary>
        public bool Has(StatId id)
        {
            return IndexOf(id) >= 0;
        }

        /// <summary>Allocated points of a stat; an unknown stat is a programming error.</summary>
        public int Get(StatId id)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                throw new ArgumentException("Unknown stat " + id + ".", nameof(id));
            }

            return _values[index];
        }

        /// <summary>
        /// A copy with extra points on one stat, for the planning preview of the Stats tab; this block stays as it is
        /// and nothing is spent. Unknown stats and negative points are programming errors.
        /// </summary>
        public BaseStats WithAdded(StatId id, int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), points, "Planned points are never negative.");
            }

            var copy = new BaseStats(this);
            copy._values[copy.RequireIndex(id)] += points;
            return copy;
        }

        /// <summary>Sets the starting points of a stat at spawn.</summary>
        internal void Set(StatId id, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Stat points are never negative.");
            }

            _values[RequireIndex(id)] = value;
        }

        /// <summary>Adds unspent points, as a level-up does.</summary>
        internal void GrantPoints(int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), points, "Granted points are never negative.");
            }

            UnspentPoints += points;
        }

        /// <summary>Moves one unspent point into a stat; false when there is none or the stat is unknown.</summary>
        internal bool TrySpendPoint(StatId id)
        {
            int index = IndexOf(id);
            if (index < 0 || UnspentPoints <= 0)
            {
                return false;
            }

            _values[index]++;
            UnspentPoints--;
            return true;
        }

        private int RequireIndex(StatId id)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                throw new ArgumentException("Unknown stat " + id + ".", nameof(id));
            }

            return index;
        }

        private int IndexOf(StatId id)
        {
            for (int i = 0; i < _ids.Length; i++)
            {
                if (_ids[i] == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
