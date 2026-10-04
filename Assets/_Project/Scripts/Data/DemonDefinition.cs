#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// A demon kind as an asset: the body it is born as, its size, movement and starting stats, converted to an
    /// immutable <see cref="DemonSpec"/> at load, plus the starting package of a stronger spawn: parts, Biomass,
    /// level and an evolution (D-070). The AI personality stays with the biome until archetype assets arrive.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Content/Demon", fileName = "DM_NewDemon")]
    public sealed class DemonDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = "demon.new";
        [SerializeField] private string _displayName = "New Demon";
        [SerializeField] private int _tier;

        [Header("Body")]
        [SerializeField] private BodyPartDefinition _core = null!;
        [SerializeField] private float _sizeMeters = 1.2f;
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _sprintMultiplier = 1.6f;

        [Header("Starting stats")]
        [SerializeField] private StatValueDefinition[] _startingStats = Array.Empty<StatValueDefinition>();

        [Header("Starting package")]
        [SerializeField] private BodyPartDefinition[] _startingParts = Array.Empty<BodyPartDefinition>();
        [SerializeField] private float _startingBiomass;
        [SerializeField] private int _startingLevel = 1;
        [SerializeField] private EvolutionDefinition? _startingEvolution;

        /// <summary>Stable content id.</summary>
        public string Id => _id;

        /// <summary>Builds the immutable spec; throws for invalid content or a missing core reference.</summary>
        public DemonSpec ToSpec()
        {
            if (_core == null)
            {
                throw new ContentException("Demon " + _id + " has no core part.");
            }

            var stats = new StatValue[_startingStats.Length];
            for (int i = 0; i < stats.Length; i++)
            {
                stats[i] = _startingStats[i].ToSpec();
            }

            var startingParts = new string[_startingParts.Length];
            for (int i = 0; i < startingParts.Length; i++)
            {
                if (_startingParts[i] == null)
                {
                    throw new ContentException("Demon " + _id + " has an empty starting part slot.");
                }

                startingParts[i] = _startingParts[i].Id;
            }

            var spec = new DemonSpec
            {
                Id = _id,
                Name = _displayName,
                Tier = _tier,
                SizeMeters = _sizeMeters,
                MoveSpeed = _moveSpeed,
                SprintMultiplier = _sprintMultiplier,
                CoreId = _core.Id,
                StartingStats = stats,
                StartingPartIds = startingParts,
                StartingBiomass = _startingBiomass,
                StartingLevel = _startingLevel,
                StartingEvolutionId = _startingEvolution != null ? _startingEvolution.Id : string.Empty,
            };
            spec.Validate();
            return spec;
        }

        internal void Configure(DemonSpec spec, BodyPartDefinition core, BodyPartDefinition[] startingParts, EvolutionDefinition? startingEvolution)
        {
            _startingParts = startingParts;
            _startingBiomass = spec.StartingBiomass;
            _startingLevel = spec.StartingLevel;
            _startingEvolution = startingEvolution;
            _id = spec.Id;
            _displayName = spec.Name;
            _tier = spec.Tier;
            _core = core;
            _sizeMeters = spec.SizeMeters;
            _moveSpeed = spec.MoveSpeed;
            _sprintMultiplier = spec.SprintMultiplier;
            _startingStats = new StatValueDefinition[spec.StartingStats.Count];
            for (int i = 0; i < _startingStats.Length; i++)
            {
                _startingStats[i] = new StatValueDefinition();
                _startingStats[i].Configure(spec.StartingStats[i]);
            }
        }

        private void OnValidate()
        {
            try
            {
                ToSpec();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.Content, "Demon asset " + name + " is invalid: " + exception.Message, this);
            }
        }
    }
}
