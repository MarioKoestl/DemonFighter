#nullable enable
using System;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>Inspector-editable display data of one base stat, nested in the combat tuning asset.</summary>
    [Serializable]
    public sealed class StatDefinition
    {
        [SerializeField] private string _id = "stat.new";
        [SerializeField] private string _name = "New Stat";
        [SerializeField] private string _description = string.Empty;

        public StatSpec ToSpec()
        {
            return new StatSpec(new StatId(_id), _name, _description);
        }

        internal void Configure(StatSpec spec)
        {
            _id = spec.Id.Value;
            _name = spec.Name;
            _description = spec.Description;
        }
    }
}
