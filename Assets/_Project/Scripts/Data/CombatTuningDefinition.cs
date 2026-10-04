#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// Every combat, growth and eating number as an asset (CODING_GUIDELINES, "Tuning values live in
    /// ScriptableObjects"), converted to an immutable <see cref="CombatTuning"/> at load. Exactly one exists.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Content/Combat Tuning", fileName = "CombatTuning")]
    public sealed class CombatTuningDefinition : ScriptableObject
    {
        [Header("Damage matrix")]
        [SerializeField] private float _strongMultiplier = 1.5f;
        [SerializeField] private float _weakMultiplier = 0.6f;
        [SerializeField] private float _neutralMultiplier = 1f;
        [SerializeField] private float _woundedThreshold = 0.5f;

        [Header("Stat effects")]
        [SerializeField] private float _hpPerConstitution = 10f / 60f;
        [SerializeField] private float _damagePerStrength = 0.05f;
        [SerializeField] private float _speedPerAgility = 0.03f;
        [SerializeField] private float _attackSpeedPerAgility = 0.03f;

        [Header("Stamina and regeneration")]
        [SerializeField] private float _staminaBase = 100f;
        [SerializeField] private float _staminaPerAgility = 10f;
        [SerializeField] private float _staminaRegenPerSecond = 12f;
        [SerializeField] private float _regenBasePerSecond = 0.5f;
        [SerializeField] private float _regenPerConstitution = 0.25f;
        [SerializeField] private float _bleedReductionPerConstitution = 0.05f;
        [SerializeField] private float _minBleedFraction = 0.2f;
        [SerializeField] private float _inCombatSeconds = 5f;

        [Header("Experience")]
        [SerializeField] private float _killXpBase = 50f;
        [SerializeField] private float _rewardBonusPerTierAbove = 0.5f;
        [SerializeField] private float _rewardFactorFarBelow = 0.1f;
        [SerializeField] private float _levelXpBase = 100f;
        [SerializeField] private float _levelXpExponent = 1.5f;
        [SerializeField] private int _statPointsPerLevel = 3;
        [SerializeField] private float _characterXpPerSkillXp = 0.5f;

        [Header("Food")]
        [SerializeField] private float _eatBiomassPerSecond = 15f;
        [SerializeField] private float _eatReachPerMeter = 1.5f;
        [SerializeField] private float _corpseBiomassPerTier = 20f;
        [SerializeField] private float _foodDecaySeconds = 90f;

        [Header("Stats")]
        [SerializeField] private StatDefinition[] _stats = Array.Empty<StatDefinition>();

        /// <summary>Builds the immutable tuning; throws for invalid content.</summary>
        public CombatTuning ToSpec()
        {
            var stats = new StatSpec[_stats.Length];
            for (int i = 0; i < _stats.Length; i++)
            {
                stats[i] = _stats[i].ToSpec();
            }

            var tuning = new CombatTuning
            {
                StrongMultiplier = _strongMultiplier,
                WeakMultiplier = _weakMultiplier,
                NeutralMultiplier = _neutralMultiplier,
                WoundedThreshold = _woundedThreshold,
                HpPerConstitution = _hpPerConstitution,
                DamagePerStrength = _damagePerStrength,
                SpeedPerAgility = _speedPerAgility,
                AttackSpeedPerAgility = _attackSpeedPerAgility,
                StaminaBase = _staminaBase,
                StaminaPerAgility = _staminaPerAgility,
                StaminaRegenPerSecond = _staminaRegenPerSecond,
                RegenBasePerSecond = _regenBasePerSecond,
                RegenPerConstitution = _regenPerConstitution,
                BleedReductionPerConstitution = _bleedReductionPerConstitution,
                MinBleedFraction = _minBleedFraction,
                InCombatSeconds = _inCombatSeconds,
                KillXpBase = _killXpBase,
                RewardBonusPerTierAbove = _rewardBonusPerTierAbove,
                RewardFactorFarBelow = _rewardFactorFarBelow,
                LevelXpBase = _levelXpBase,
                LevelXpExponent = _levelXpExponent,
                StatPointsPerLevel = _statPointsPerLevel,
                CharacterXpPerSkillXp = _characterXpPerSkillXp,
                EatBiomassPerSecond = _eatBiomassPerSecond,
                EatReachPerMeter = _eatReachPerMeter,
                CorpseBiomassPerTier = _corpseBiomassPerTier,
                FoodDecaySeconds = _foodDecaySeconds,
                Stats = stats,
            };
            tuning.Validate();
            return tuning;
        }

        internal void Configure(CombatTuning spec)
        {
            _strongMultiplier = spec.StrongMultiplier;
            _weakMultiplier = spec.WeakMultiplier;
            _neutralMultiplier = spec.NeutralMultiplier;
            _woundedThreshold = spec.WoundedThreshold;
            _hpPerConstitution = spec.HpPerConstitution;
            _damagePerStrength = spec.DamagePerStrength;
            _speedPerAgility = spec.SpeedPerAgility;
            _attackSpeedPerAgility = spec.AttackSpeedPerAgility;
            _staminaBase = spec.StaminaBase;
            _staminaPerAgility = spec.StaminaPerAgility;
            _staminaRegenPerSecond = spec.StaminaRegenPerSecond;
            _regenBasePerSecond = spec.RegenBasePerSecond;
            _regenPerConstitution = spec.RegenPerConstitution;
            _bleedReductionPerConstitution = spec.BleedReductionPerConstitution;
            _minBleedFraction = spec.MinBleedFraction;
            _inCombatSeconds = spec.InCombatSeconds;
            _killXpBase = spec.KillXpBase;
            _rewardBonusPerTierAbove = spec.RewardBonusPerTierAbove;
            _rewardFactorFarBelow = spec.RewardFactorFarBelow;
            _levelXpBase = spec.LevelXpBase;
            _levelXpExponent = spec.LevelXpExponent;
            _statPointsPerLevel = spec.StatPointsPerLevel;
            _characterXpPerSkillXp = spec.CharacterXpPerSkillXp;
            _eatBiomassPerSecond = spec.EatBiomassPerSecond;
            _eatReachPerMeter = spec.EatReachPerMeter;
            _corpseBiomassPerTier = spec.CorpseBiomassPerTier;
            _foodDecaySeconds = spec.FoodDecaySeconds;
            _stats = new StatDefinition[spec.Stats.Count];
            for (int i = 0; i < _stats.Length; i++)
            {
                _stats[i] = new StatDefinition();
                _stats[i].Configure(spec.Stats[i]);
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
                Log.Error(LogCategory.Content, "Combat tuning asset " + name + " is invalid: " + exception.Message, this);
            }
        }
    }
}
