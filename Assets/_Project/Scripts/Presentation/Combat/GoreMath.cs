#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Combat
{
    /// <summary>The plain arithmetic of the gore views, kept apart from the pools so it can be tested without a scene.</summary>
    public static class GoreMath
    {
        private const float MinimumSeconds = 0.0001f;

        /// <summary>
        /// Diameter of the pool under a corpse at a growth progress from 0 to 1: it starts at a fraction of the body
        /// size and spreads, fast first and slower later, toward a maximum that grows with the Biomass of the corpse.
        /// </summary>
        public static float PoolDiameter(float sizeMeters, float biomass, float startPerMeter, float maxPerMeter, float metersPerBiomass, float progress)
        {
            float start = sizeMeters * startPerMeter;
            float max = sizeMeters * maxPerMeter + Mathf.Max(0f, biomass) * metersPerBiomass;
            float remaining = 1f - Mathf.Clamp01(progress);
            float eased = 1f - remaining * remaining;
            return Mathf.Lerp(start, Mathf.Max(start, max), eased);
        }

        /// <summary>How many pieces a burst throws for a body size; at least one.</summary>
        public static int BurstCount(float sizeMeters, float perMeter)
        {
            return Mathf.Max(1, Mathf.RoundToInt(sizeMeters * perMeter));
        }

        /// <summary>Scale of a piece by its age: full until the shrink begins, then down to nothing at the end of its life.</summary>
        public static float ShrinkFactor(float age, float lifeSeconds, float shrinkSeconds)
        {
            float start = lifeSeconds - shrinkSeconds;
            if (age <= start)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (age - start) / Mathf.Max(shrinkSeconds, MinimumSeconds));
        }
    }
}
