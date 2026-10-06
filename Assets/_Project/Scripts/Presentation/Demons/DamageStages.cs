#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Maps the health of a part to the stage it shows (D-080). The thresholds are view tuning from
    /// <see cref="DemonViewSettings"/>; the simulation's own wounded threshold and its three conditions stay as they are.
    /// </summary>
    public static class DamageStages
    {
        /// <summary>
        /// Lost parts are Lost whatever their HP. Below the mangled fraction a part is Mangled, below the wounded
        /// fraction Wounded, otherwise Intact. The mangled fraction never exceeds the wounded one.
        /// </summary>
        public static DamageStage For(float hpFraction, bool lost, float woundedBelow, float mangledBelow)
        {
            if (lost)
            {
                return DamageStage.Lost;
            }

            float mangled = Mathf.Min(mangledBelow, woundedBelow);
            if (hpFraction < mangled)
            {
                return DamageStage.Mangled;
            }

            return hpFraction < woundedBelow ? DamageStage.Wounded : DamageStage.Intact;
        }
    }
}
