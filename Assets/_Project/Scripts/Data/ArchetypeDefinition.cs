#nullable enable
using System;
using DemonFighter.Simulation.Ai;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// Inspector-editable AI personality, converted to an <see cref="ArchetypeSpec"/> at load. Nested inside the
    /// biome asset for M1; becomes its own asset type in M4.
    /// </summary>
    [Serializable]
    public sealed class ArchetypeDefinition
    {
        [SerializeField] private string _name = "Blob";
        [SerializeField] private float _wanderWeight = 1f;
        [SerializeField] private float _restWeight = 0.8f;
        [SerializeField] private float _patrolWeight;
        [SerializeField] private float _wanderRadius = 20f;
        [SerializeField] private float _restSecondsMin = 2f;
        [SerializeField] private float _restSecondsMax = 5f;
        [SerializeField] private int _decisionIntervalTicks = 10;
        [SerializeField] private float _arriveDistance = 1f;
        [SerializeField] private float _huntWeight = 0.8f;
        [SerializeField] private float _eatWeight = 2.5f;
        [SerializeField] private float _fleeHealthFraction;
        [SerializeField] private float _perceptionRadius = 18f;

        /// <summary>Builds the immutable spec; throws for invalid numbers, which OnValidate reports.</summary>
        public ArchetypeSpec ToSpec()
        {
            return new ArchetypeSpec(
                _name, _wanderWeight, _restWeight, _patrolWeight, _wanderRadius,
                _restSecondsMin, _restSecondsMax, _decisionIntervalTicks, _arriveDistance,
                _huntWeight, _eatWeight, _fleeHealthFraction, _perceptionRadius);
        }

        internal void ApplyDefaults(ArchetypeSpec spec)
        {
            _name = spec.Name;
            _wanderWeight = spec.WanderWeight;
            _restWeight = spec.RestWeight;
            _patrolWeight = spec.PatrolWeight;
            _wanderRadius = spec.WanderRadius;
            _restSecondsMin = spec.RestSecondsMin;
            _restSecondsMax = spec.RestSecondsMax;
            _decisionIntervalTicks = spec.DecisionIntervalTicks;
            _arriveDistance = spec.ArriveDistance;
            _huntWeight = spec.HuntWeight;
            _eatWeight = spec.EatWeight;
            _fleeHealthFraction = spec.FleeHealthFraction;
            _perceptionRadius = spec.PerceptionRadius;
        }
    }
}
