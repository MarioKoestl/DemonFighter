#nullable enable
using System;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>Inspector-editable perk of a skill, nested in a skill asset; disabled means the skill has none.</summary>
    [Serializable]
    public sealed class SkillPerkDefinition
    {
        [SerializeField] private bool _enabled;
        [SerializeField] private int _level = 10;
        [SerializeField] private string _name = "Perk";
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private float _damageMultiplier = 1f;
        [SerializeField] private float _bleedDurationMultiplier = 1f;
        [SerializeField] private float _recoveryMultiplier = 1f;
        [SerializeField] private float _cooldownMultiplier = 1f;
        [SerializeField] private float _staminaMultiplier = 1f;
        [SerializeField] private float _reachMultiplier = 1f;
        [SerializeField] private float _effectMultiplier = 1f;

        public SkillPerkSpec? ToSpec()
        {
            if (!_enabled)
            {
                return null;
            }

            return new SkillPerkSpec
            {
                Level = _level,
                Name = _name,
                Description = _description,
                DamageMultiplier = _damageMultiplier,
                BleedDurationMultiplier = _bleedDurationMultiplier,
                RecoveryMultiplier = _recoveryMultiplier,
                CooldownMultiplier = _cooldownMultiplier,
                StaminaMultiplier = _staminaMultiplier,
                ReachMultiplier = _reachMultiplier,
                EffectMultiplier = _effectMultiplier,
            };
        }

        internal void Configure(SkillPerkSpec? spec)
        {
            _enabled = spec != null;
            if (spec == null)
            {
                return;
            }

            _level = spec.Level;
            _name = spec.Name;
            _description = spec.Description;
            _damageMultiplier = spec.DamageMultiplier;
            _bleedDurationMultiplier = spec.BleedDurationMultiplier;
            _recoveryMultiplier = spec.RecoveryMultiplier;
            _cooldownMultiplier = spec.CooldownMultiplier;
            _staminaMultiplier = spec.StaminaMultiplier;
            _reachMultiplier = spec.ReachMultiplier;
            _effectMultiplier = spec.EffectMultiplier;
        }
    }
}
