#nullable enable
using System;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>Inspector-editable starting points of one stat, nested in a demon asset.</summary>
    [Serializable]
    public sealed class StatValueDefinition
    {
        [SerializeField] private string _statId = "stat.strength";
        [SerializeField] private int _value;

        public StatValue ToSpec()
        {
            return new StatValue(new StatId(_statId), _value);
        }

        internal void Configure(StatValue spec)
        {
            _statId = spec.Stat.Value;
            _value = spec.Value;
        }
    }
}
