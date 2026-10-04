#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// A skill as an asset, converted to an immutable <see cref="SkillSpec"/> at load. Adding a skill is a new asset
    /// plus, when its effect is new, one behaviour class found by attribute (CLAUDE.md rule 3).
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Content/Skill", fileName = "SK_NewSkill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = "skill.new";
        [SerializeField] private string _displayName = "New Skill";
        [SerializeField] private string _behaviourId = "melee-strike";

        [Header("Effect")]
        [SerializeField] private DamageType _damageType = DamageType.Pierce;
        [SerializeField] private float _baseDamage = 12f;
        [SerializeField] private float _staminaCost = 15f;
        [SerializeField] private float _reachPerMeter = 1.4f;
        [SerializeField] private float _arcDegrees = 70f;
        [SerializeField] private float _bleedSeconds = 3f;
        [SerializeField] private float _bleedDamagePerSecond = 2f;
        [SerializeField] private float _staggerSeconds;

        [Header("Timing in seconds")]
        [SerializeField] private float _windupSeconds = 0.15f;
        [SerializeField] private float _activeSeconds = 0.2f;
        [SerializeField] private float _recoverySeconds = 0.35f;
        [SerializeField] private float _cooldownSeconds = 0.6f;

        [Header("Growth")]
        [SerializeField] private float _skillXpPerHit = 10f;
        [SerializeField] private float _levelBaseXp = 100f;
        [SerializeField] private float _levelExponent = 1.5f;
        [SerializeField] private int _maxLevel = 20;
        [SerializeField] private float _damageBonusPerLevel = 0.05f;
        [SerializeField] private float _staminaCostReductionPerLevel = 0.03f;
        [SerializeField] private float _minStaminaCostFraction = 0.5f;

        /// <summary>Stable content id.</summary>
        public string Id => _id;

        /// <summary>Builds the immutable spec; throws for invalid content.</summary>
        public SkillSpec ToSpec()
        {
            var spec = new SkillSpec
            {
                Id = _id,
                Name = _displayName,
                BehaviourId = _behaviourId,
                DamageType = _damageType,
                BaseDamage = _baseDamage,
                StaminaCost = _staminaCost,
                ReachPerMeter = _reachPerMeter,
                ArcDegrees = _arcDegrees,
                WindupSeconds = _windupSeconds,
                ActiveSeconds = _activeSeconds,
                RecoverySeconds = _recoverySeconds,
                CooldownSeconds = _cooldownSeconds,
                BleedSeconds = _bleedSeconds,
                BleedDamagePerSecond = _bleedDamagePerSecond,
                StaggerSeconds = _staggerSeconds,
                SkillXpPerHit = _skillXpPerHit,
                LevelCurve = new SkillLevelCurve { BaseXp = _levelBaseXp, Exponent = _levelExponent, MaxLevel = _maxLevel },
                DamageBonusPerLevel = _damageBonusPerLevel,
                StaminaCostReductionPerLevel = _staminaCostReductionPerLevel,
                MinStaminaCostFraction = _minStaminaCostFraction,
            };
            spec.Validate();
            return spec;
        }

        internal void Configure(SkillSpec spec)
        {
            _id = spec.Id;
            _displayName = spec.Name;
            _behaviourId = spec.BehaviourId;
            _damageType = spec.DamageType;
            _baseDamage = spec.BaseDamage;
            _staminaCost = spec.StaminaCost;
            _reachPerMeter = spec.ReachPerMeter;
            _arcDegrees = spec.ArcDegrees;
            _windupSeconds = spec.WindupSeconds;
            _activeSeconds = spec.ActiveSeconds;
            _recoverySeconds = spec.RecoverySeconds;
            _cooldownSeconds = spec.CooldownSeconds;
            _bleedSeconds = spec.BleedSeconds;
            _bleedDamagePerSecond = spec.BleedDamagePerSecond;
            _staggerSeconds = spec.StaggerSeconds;
            _skillXpPerHit = spec.SkillXpPerHit;
            _levelBaseXp = spec.LevelCurve.BaseXp;
            _levelExponent = spec.LevelCurve.Exponent;
            _maxLevel = spec.LevelCurve.MaxLevel;
            _damageBonusPerLevel = spec.DamageBonusPerLevel;
            _staminaCostReductionPerLevel = spec.StaminaCostReductionPerLevel;
            _minStaminaCostFraction = spec.MinStaminaCostFraction;
        }

        private void OnValidate()
        {
            try
            {
                ToSpec();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.Content, "Skill asset " + name + " is invalid: " + exception.Message, this);
            }
        }
    }
}
