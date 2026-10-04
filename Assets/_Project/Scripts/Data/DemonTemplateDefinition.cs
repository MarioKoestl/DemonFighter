#nullable enable
using System;
using DemonFighter.Simulation;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// Inspector-editable body of a demon, converted to a <see cref="DemonTemplate"/> at load. Nested inside the
    /// biome asset for M1; becomes part of the archetype assets in M3.
    /// </summary>
    [Serializable]
    public sealed class DemonTemplateDefinition
    {
        [SerializeField] private string _name = "Blob";
        [SerializeField] private int _tier;
        [SerializeField] private float _sizeMeters = 1.2f;
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _sprintMultiplier = 1.6f;

        /// <summary>Builds the immutable spec; throws for invalid numbers, which OnValidate reports.</summary>
        public DemonTemplate ToSpec()
        {
            return new DemonTemplate(_name, _tier, _sizeMeters, _moveSpeed, _sprintMultiplier);
        }

        internal void ApplyDefaults(DemonTemplate spec)
        {
            _name = spec.Name;
            _tier = spec.Tier;
            _sizeMeters = spec.SizeMeters;
            _moveSpeed = spec.MoveSpeed;
            _sprintMultiplier = spec.SprintMultiplier;
        }
    }
}
