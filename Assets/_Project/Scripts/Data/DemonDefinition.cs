#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// A demon kind as an asset: the body it is born as, its size, movement and starting stats, converted to an
    /// immutable <see cref="DemonSpec"/> at load. The AI personality stays with the biome until archetype assets
    /// arrive in M4.
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
            };
            spec.Validate();
            return spec;
        }

        internal void Configure(DemonSpec spec, BodyPartDefinition core)
        {
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
