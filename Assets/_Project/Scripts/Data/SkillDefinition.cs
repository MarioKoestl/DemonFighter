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
        private const int M3ContentVersion = 3;
        private const int CurrentContentVersion = 5;

        [Header("Identity")]
        [SerializeField] private string _id = "skill.new";
        [SerializeField] private string _displayName = "New Skill";
        [SerializeField] private string _behaviourId = "melee-strike";
        [SerializeField] private SkillSlot _inputSlot = SkillSlot.Primary;
        [SerializeField] private int _slotPriority;
        [SerializeField] private bool _isPassive;
        [SerializeField] private bool _enablesSprint;

        [Header("Effect")]
        [SerializeField] private DamageType _damageType = DamageType.Pierce;
        [SerializeField] private float _baseDamage = 12f;
        [SerializeField] private float _staminaCost = 15f;
        [SerializeField] private float _staminaCostPerSecond;
        [SerializeField] private float _reachPerMeter = 1.4f;
        [SerializeField] private float _arcDegrees = 70f;
        [SerializeField] private float _bleedSeconds = 3f;
        [SerializeField] private float _bleedDamagePerSecond = 2f;
        [SerializeField] private float _staggerSeconds;
        [SerializeField] private float _holdSeconds;
        [SerializeField] private float _dashMeters;
        [SerializeField] private float _knockbackMeters;

        [Header("Timing in seconds")]
        [SerializeField] private float _windupSeconds = 0.15f;
        [SerializeField] private float _activeSeconds = 0.2f;
        [SerializeField] private float _recoverySeconds = 0.35f;
        [SerializeField] private float _cooldownSeconds = 0.6f;

        [Header("Growth")]
        [SerializeField] private float _skillXpPerHit = 10f;
        [SerializeField] private float _skillXpPerSecond;
        [SerializeField] private float _levelBaseXp = 100f;
        [SerializeField] private float _levelExponent = 1.5f;
        [SerializeField] private int _maxLevel = 20;
        [SerializeField] private float _damageBonusPerLevel = 0.05f;
        [SerializeField] private float _staminaCostReductionPerLevel = 0.03f;
        [SerializeField] private float _minStaminaCostFraction = 0.5f;
        [SerializeField] private float _cooldownReductionPerLevel = 0.02f;
        [SerializeField] private float _minCooldownFraction = 0.5f;
        [SerializeField] private float _speedBonusPerLevel = 0.02f;
        [SerializeField] private float _reachBonusPerLevel = 0.01f;
        [SerializeField] private SkillPerkDefinition _perk = new SkillPerkDefinition();

        [Header("Look")]
        [SerializeField] private SkillMotion _motion = SkillMotion.None;
        [SerializeField] private AudioEventDefinition? _useSound;

        [SerializeField, HideInInspector] private int _contentVersion;

        /// <summary>Stable content id.</summary>
        public string Id => _id;

        /// <summary>How the view animates a use of this skill (D-082); look only, the simulation never reads it.</summary>
        public SkillMotion Motion => _motion;

        /// <summary>The sound a use of this skill makes (D-084); null for a silent skill.</summary>
        public AudioEventDefinition? UseSound => _useSound;

        /// <summary>True for an asset created before M3 that still lacks the M3 fields.</summary>
        internal bool NeedsM3Defaults => _contentVersion < M3ContentVersion;

        /// <summary>True for an asset created before M5 that still lacks the motion.</summary>
        internal bool NeedsM5Defaults => _contentVersion < CurrentContentVersion;

        /// <summary>Builds the immutable spec; throws for invalid content.</summary>
        public SkillSpec ToSpec()
        {
            var spec = new SkillSpec
            {
                Id = _id,
                Name = _displayName,
                BehaviourId = _behaviourId,
                InputSlot = _inputSlot,
                SlotPriority = _slotPriority,
                IsPassive = _isPassive,
                EnablesSprint = _enablesSprint,
                DamageType = _damageType,
                BaseDamage = _baseDamage,
                StaminaCost = _staminaCost,
                StaminaCostPerSecond = _staminaCostPerSecond,
                ReachPerMeter = _reachPerMeter,
                ArcDegrees = _arcDegrees,
                WindupSeconds = _windupSeconds,
                ActiveSeconds = _activeSeconds,
                RecoverySeconds = _recoverySeconds,
                CooldownSeconds = _cooldownSeconds,
                BleedSeconds = _bleedSeconds,
                BleedDamagePerSecond = _bleedDamagePerSecond,
                StaggerSeconds = _staggerSeconds,
                HoldSeconds = _holdSeconds,
                DashMeters = _dashMeters,
                KnockbackMeters = _knockbackMeters,
                SkillXpPerHit = _skillXpPerHit,
                SkillXpPerSecond = _skillXpPerSecond,
                LevelCurve = new SkillLevelCurve { BaseXp = _levelBaseXp, Exponent = _levelExponent, MaxLevel = _maxLevel },
                DamageBonusPerLevel = _damageBonusPerLevel,
                StaminaCostReductionPerLevel = _staminaCostReductionPerLevel,
                MinStaminaCostFraction = _minStaminaCostFraction,
                CooldownReductionPerLevel = _cooldownReductionPerLevel,
                MinCooldownFraction = _minCooldownFraction,
                SpeedBonusPerLevel = _speedBonusPerLevel,
                ReachBonusPerLevel = _reachBonusPerLevel,
                Perk = _perk.ToSpec(),
            };
            spec.Validate();
            return spec;
        }

        /// <summary>Fills a new asset from a spec; the generator uses it once, afterwards the asset is the truth.</summary>
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
            ApplyM3Defaults(spec);
        }

        /// <summary>Gives an M2 asset the fields M3 added without touching the numbers it already had.</summary>
        internal void ApplyM3Defaults(SkillSpec spec)
        {
            _inputSlot = spec.InputSlot;
            _slotPriority = spec.SlotPriority;
            _isPassive = spec.IsPassive;
            _enablesSprint = spec.EnablesSprint;
            _staminaCostPerSecond = spec.StaminaCostPerSecond;
            _holdSeconds = spec.HoldSeconds;
            _dashMeters = spec.DashMeters;
            _knockbackMeters = spec.KnockbackMeters;
            _skillXpPerSecond = spec.SkillXpPerSecond;
            _cooldownReductionPerLevel = spec.CooldownReductionPerLevel;
            _minCooldownFraction = spec.MinCooldownFraction;
            _speedBonusPerLevel = spec.SpeedBonusPerLevel;
            _reachBonusPerLevel = spec.ReachBonusPerLevel;
            _perk.Configure(spec.Perk);
            _contentVersion = M3ContentVersion;
        }

        /// <summary>Gives an older asset its motion and its sound once; the generator picks them by skill id.</summary>
        internal void ApplyM5Defaults(SkillMotion motion, AudioEventDefinition? useSound)
        {
            _motion = motion;
            _useSound = useSound;
            _contentVersion = CurrentContentVersion;
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
