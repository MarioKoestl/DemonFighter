#nullable enable
using System;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Stats
{
    /// <summary>
    /// What the base stats mean in play (GAME_DESIGN, "Base stats"): Strength scales damage, Constitution scales HP,
    /// regeneration and bleed resistance, Agility scales speed, attack speed and stamina. Recomputed whenever a stat
    /// changes and cached on the demon.
    /// </summary>
    public readonly struct DerivedStats
    {
        private DerivedStats(
            float damageMultiplier,
            float moveSpeedMultiplier,
            float attackSpeedMultiplier,
            float maxStamina,
            float staminaRegenPerSecond,
            float hpMultiplier,
            float regenPerSecond,
            float bleedDurationFactor)
        {
            DamageMultiplier = damageMultiplier;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            AttackSpeedMultiplier = attackSpeedMultiplier;
            MaxStamina = maxStamina;
            StaminaRegenPerSecond = staminaRegenPerSecond;
            HpMultiplier = hpMultiplier;
            RegenPerSecond = regenPerSecond;
            BleedDurationFactor = bleedDurationFactor;
        }

        /// <summary>Factor on every damage this demon deals.</summary>
        public float DamageMultiplier { get; }

        /// <summary>Factor on walking and sprinting speed.</summary>
        public float MoveSpeedMultiplier { get; }

        /// <summary>Factor that shortens skill windup, active and recovery times.</summary>
        public float AttackSpeedMultiplier { get; }

        public float MaxStamina { get; }

        public float StaminaRegenPerSecond { get; }

        /// <summary>Factor on the base HP of every body part.</summary>
        public float HpMultiplier { get; }

        /// <summary>Passive HP regeneration per part per second.</summary>
        public float RegenPerSecond { get; }

        /// <summary>Factor on how long this demon bleeds; never below the tuning minimum.</summary>
        public float BleedDurationFactor { get; }

        /// <summary>Computes the derived values from the allocated points and the tuning formulas.</summary>
        public static DerivedStats From(BaseStats stats, CombatTuning tuning)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            int strength = Points(stats, StatIds.Strength);
            int constitution = Points(stats, StatIds.Constitution);
            int agility = Points(stats, StatIds.Agility);

            return new DerivedStats(
                damageMultiplier: 1f + tuning.DamagePerStrength * strength,
                moveSpeedMultiplier: 1f + tuning.SpeedPerAgility * agility,
                attackSpeedMultiplier: 1f + tuning.AttackSpeedPerAgility * agility,
                maxStamina: tuning.StaminaBase + tuning.StaminaPerAgility * agility,
                staminaRegenPerSecond: tuning.StaminaRegenPerSecond,
                hpMultiplier: 1f + tuning.HpPerConstitution * constitution,
                regenPerSecond: tuning.RegenBasePerSecond + tuning.RegenPerConstitution * constitution,
                bleedDurationFactor: MathF.Max(tuning.MinBleedFraction, 1f - tuning.BleedReductionPerConstitution * constitution));
        }

        private static int Points(BaseStats stats, StatId id)
        {
            return stats.Has(id) ? stats.Get(id) : 0;
        }
    }
}
