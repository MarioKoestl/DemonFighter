#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// An evolution option as an asset (GAME_DESIGN, "Evolution"), converted to an immutable <see cref="EvolutionSpec"/>
    /// at load. Parts and skills are asset references, so a broken link shows in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Content/Evolution", fileName = "EV_NewEvolution")]
    public sealed class EvolutionDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = "evolution.new.1";
        [SerializeField] private string _displayName = "New Line";
        [SerializeField, TextArea] private string _description = string.Empty;
        [SerializeField] private int _stage = 1;
        [SerializeField] private string _fitStatId = "stat.strength";

        [Header("Package")]
        [SerializeField] private int _statPoints;
        [SerializeField] private StatValueDefinition[] _statBonuses = Array.Empty<StatValueDefinition>();
        [SerializeField] private StatValueDefinition[] _statCapBonuses = Array.Empty<StatValueDefinition>();
        [SerializeField] private BodyPartDefinition[] _freeMutations = Array.Empty<BodyPartDefinition>();
        [SerializeField] private BodyPartDefinition[] _unlockedParts = Array.Empty<BodyPartDefinition>();
        [SerializeField] private SkillDefinition[] _extraSkills = Array.Empty<SkillDefinition>();
        [SerializeField, HideInInspector] private int _contentVersion;

        private const int CurrentContentVersion = 2;

        /// <summary>Stable content id.</summary>
        public string Id => _id;

        /// <summary>True for an asset written before evolutions carried bound stat gains (D-067); the generator rewrites it from the placeholder content once.</summary>
        internal bool NeedsPackageDefaults => _contentVersion < CurrentContentVersion;

        /// <summary>Builds the immutable spec; throws for invalid content or a missing reference.</summary>
        public EvolutionSpec ToSpec()
        {
            var caps = new StatValue[_statCapBonuses.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = _statCapBonuses[i].ToSpec();
            }

            var bonuses = new StatValue[_statBonuses.Length];
            for (int i = 0; i < bonuses.Length; i++)
            {
                bonuses[i] = _statBonuses[i].ToSpec();
            }

            var spec = new EvolutionSpec
            {
                Id = _id,
                Name = _displayName,
                Description = _description,
                Stage = _stage,
                StatPoints = _statPoints,
                StatCapBonuses = caps,
                StatBonuses = bonuses,
                FreeMutationPartIds = Ids(_freeMutations, "free mutation"),
                UnlockedPartIds = Ids(_unlockedParts, "unlocked part"),
                ExtraSkillIds = Ids(_extraSkills, "extra skill"),
                FitStat = new StatId(_fitStatId),
            };
            spec.Validate();
            return spec;
        }

        internal void Configure(EvolutionSpec spec, BodyPartDefinition[] freeMutations, BodyPartDefinition[] unlockedParts, SkillDefinition[] extraSkills)
        {
            _id = spec.Id;
            _displayName = spec.Name;
            _description = spec.Description;
            _stage = spec.Stage;
            _fitStatId = spec.FitStat.Value;
            _statPoints = spec.StatPoints;
            _statBonuses = new StatValueDefinition[spec.StatBonuses.Count];
            for (int i = 0; i < _statBonuses.Length; i++)
            {
                _statBonuses[i] = new StatValueDefinition();
                _statBonuses[i].Configure(spec.StatBonuses[i]);
            }

            _statCapBonuses = new StatValueDefinition[spec.StatCapBonuses.Count];
            for (int i = 0; i < _statCapBonuses.Length; i++)
            {
                _statCapBonuses[i] = new StatValueDefinition();
                _statCapBonuses[i].Configure(spec.StatCapBonuses[i]);
            }

            _freeMutations = freeMutations;
            _unlockedParts = unlockedParts;
            _extraSkills = extraSkills;
            _contentVersion = CurrentContentVersion;
        }

        private string[] Ids(BodyPartDefinition[] parts, string role)
        {
            var ids = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null)
                {
                    throw new ContentException("Evolution " + _id + " has an empty " + role + " slot.");
                }

                ids[i] = parts[i].Id;
            }

            return ids;
        }

        private string[] Ids(SkillDefinition[] skills, string role)
        {
            var ids = new string[skills.Length];
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] == null)
                {
                    throw new ContentException("Evolution " + _id + " has an empty " + role + " slot.");
                }

                ids[i] = skills[i].Id;
            }

            return ids;
        }

        private void OnValidate()
        {
            try
            {
                ToSpec();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.Content, "Evolution asset " + name + " is invalid: " + exception.Message, this);
            }
        }
    }
}
