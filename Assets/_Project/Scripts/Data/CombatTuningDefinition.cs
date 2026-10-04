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
        // Playtest pace (D-062): a level comes quicker than with the design baseline the CombatTuning record keeps.
        [SerializeField] private float _levelXpBase = 30f;
        [SerializeField] private float _levelXpExponent = 1.2f;
        [SerializeField] private int _statPointsPerLevel = 3;
        [SerializeField] private float _characterXpPerSkillXp = 0.5f;

        [Header("Food")]
        [SerializeField] private float _eatBiomassPerSecond = 15f;
        [SerializeField] private float _eatReachPerMeter = 1.5f;
        [SerializeField] private float _corpseBiomassPerTier = 20f;
        [SerializeField] private float _foodDecaySeconds = 90f;

        [Header("Body")]
        [SerializeField] private int _tierInvestmentStep = 4;
        [SerializeField] private float _sizeStepPerTier = 0.5f;
        [SerializeField] private float _partHpPerUpgradeLevel = 0.15f;

        [Header("Mutation")]
        [SerializeField] private float _transformationSeconds = 2f;
        [SerializeField] private float _repeatCostMultiplier = 1.5f;
        [SerializeField] private float _upgradeCostFraction = 0.5f;
        [SerializeField] private float _regrowCostFraction = 0.5f;
        [SerializeField] private int _characterLevelPerUpgradeLevel = 2;
        [SerializeField] private float _knockbackSeconds = 0.3f;

        [Header("Evolution")]
        [SerializeField] private int _firstEvolutionLevel = 5;
        [SerializeField] private int _secondEvolutionLevel = 10;
        [SerializeField] private int _baseStatCap = 10;
        [SerializeField] private float _perceptionPerSenseLevel = 0.3f;

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
                TierInvestmentStep = _tierInvestmentStep,
                SizeStepPerTier = _sizeStepPerTier,
                PartHpPerUpgradeLevel = _partHpPerUpgradeLevel,
                TransformationSeconds = _transformationSeconds,
                RepeatCostMultiplier = _repeatCostMultiplier,
                UpgradeCostFraction = _upgradeCostFraction,
                RegrowCostFraction = _regrowCostFraction,
                CharacterLevelPerUpgradeLevel = _characterLevelPerUpgradeLevel,
                KnockbackSeconds = _knockbackSeconds,
                EvolutionLevels = new[] { _firstEvolutionLevel, _secondEvolutionLevel },
                BaseStatCap = _baseStatCap,
                PerceptionPerSenseLevel = _perceptionPerSenseLevel,
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
            _tierInvestmentStep = spec.TierInvestmentStep;
            _sizeStepPerTier = spec.SizeStepPerTier;
            _partHpPerUpgradeLevel = spec.PartHpPerUpgradeLevel;
            _transformationSeconds = spec.TransformationSeconds;
            _repeatCostMultiplier = spec.RepeatCostMultiplier;
            _upgradeCostFraction = spec.UpgradeCostFraction;
            _regrowCostFraction = spec.RegrowCostFraction;
            _characterLevelPerUpgradeLevel = spec.CharacterLevelPerUpgradeLevel;
            _knockbackSeconds = spec.KnockbackSeconds;
            _firstEvolutionLevel = spec.EvolutionLevels.Count > 0 ? spec.EvolutionLevels[0] : 5;
            _secondEvolutionLevel = spec.EvolutionLevels.Count > 1 ? spec.EvolutionLevels[1] : 10;
            _baseStatCap = spec.BaseStatCap;
            _perceptionPerSenseLevel = spec.PerceptionPerSenseLevel;
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
