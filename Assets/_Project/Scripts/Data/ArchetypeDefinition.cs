#nullable enable
using System;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// Inspector-editable AI personality, converted to an <see cref="ArchetypeSpec"/> at load. Nested inside the
    /// biome asset for the elder and wrapped by <see cref="ArchetypeAsset"/> for the blob personalities (D-071).
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
        [SerializeField] private float _routePullPerThreat;
        [SerializeField] private float _routePullMax;
        [SerializeField] private BodyPartDefinition[] _preferredParts = Array.Empty<BodyPartDefinition>();
        [SerializeField] private string _preferredEvolutionStatId = "stat.strength";
        [SerializeField] private string _preferredStatId = "stat.strength";

        /// <summary>Content name.</summary>
        public string Name => _name;

        /// <summary>Builds the immutable spec; throws for invalid numbers, which OnValidate reports.</summary>
        public ArchetypeSpec ToSpec()
        {
            var preferred = new string[_preferredParts.Length];
            for (int i = 0; i < preferred.Length; i++)
            {
                if (_preferredParts[i] == null)
                {
                    throw new ContentException("Archetype " + _name + " has an empty preferred part slot.");
                }

                preferred[i] = _preferredParts[i].Id;
            }

            return new ArchetypeSpec(
                _name, _wanderWeight, _restWeight, _patrolWeight, _wanderRadius,
                _restSecondsMin, _restSecondsMax, _decisionIntervalTicks, _arriveDistance,
                _huntWeight, _eatWeight, _fleeHealthFraction, _perceptionRadius, _routePullPerThreat, _routePullMax)
            {
                PreferredPartIds = preferred,
                PreferredEvolutionStat = new StatId(_preferredEvolutionStatId),
                PreferredStat = new StatId(_preferredStatId),
            };
        }

        /// <summary>Points the preferences at part assets; the generator resolves them from the spec ids.</summary>
        internal void SetPreferredParts(BodyPartDefinition[] parts)
        {
            _preferredParts = parts;
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
            _routePullPerThreat = spec.RoutePullPerThreat;
            _routePullMax = spec.RoutePullMax;
            _preferredEvolutionStatId = spec.PreferredEvolutionStat.Value;
            _preferredStatId = spec.PreferredStat.Value;
        }
    }
}
