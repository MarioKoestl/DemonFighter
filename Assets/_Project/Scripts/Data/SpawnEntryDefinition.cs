#nullable enable
using System;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>One line of the spawn table of a biome asset (D-070): a demon asset, the threat level it appears from and its weight.</summary>
    [Serializable]
    public sealed class SpawnEntryDefinition
    {
        [SerializeField] private DemonDefinition _demon = null!;
        [SerializeField] private int _minThreat;
        [SerializeField] private float _weight = 1f;

        /// <summary>The demon asset this line spawns; null in an unfinished asset.</summary>
        public DemonDefinition Demon => _demon;

        /// <summary>Builds the immutable entry; throws for a missing demon or a bad number.</summary>
        public SpawnEntry ToSpec(string biomeId)
        {
            if (_demon == null)
            {
                throw new ContentException("Biome " + biomeId + " has a spawn entry without a demon.");
            }

            return new SpawnEntry(_demon.ToSpec(), _minThreat, _weight);
        }

        internal void Configure(DemonDefinition demon, SpawnEntry entry)
        {
            _demon = demon;
            _minThreat = entry.MinThreat;
            _weight = entry.Weight;
        }
    }
}
