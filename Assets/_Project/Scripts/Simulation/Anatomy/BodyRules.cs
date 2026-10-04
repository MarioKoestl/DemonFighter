#nullable enable
using System;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Anatomy
{
    /// <summary>The tuning a body needs to compute part health: the wounded threshold and the health each upgrade adds.</summary>
    public readonly struct BodyRules
    {
        public BodyRules(float woundedThreshold, float upgradeHpPerLevel)
        {
            if (woundedThreshold <= 0f || woundedThreshold >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(woundedThreshold), woundedThreshold, "The wounded threshold lies in (0, 1).");
            }

            if (upgradeHpPerLevel < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(upgradeHpPerLevel), upgradeHpPerLevel, "Upgrade health is never negative.");
            }

            WoundedThreshold = woundedThreshold;
            UpgradeHpPerLevel = upgradeHpPerLevel;
        }

        /// <summary>Fraction of max HP below which a part counts as wounded.</summary>
        public float WoundedThreshold { get; }

        /// <summary>Fraction of base HP every upgrade level adds to a part.</summary>
        public float UpgradeHpPerLevel { get; }

        public static BodyRules From(CombatTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return new BodyRules(tuning.WoundedThreshold, tuning.PartHpPerUpgradeLevel);
        }
    }
}
