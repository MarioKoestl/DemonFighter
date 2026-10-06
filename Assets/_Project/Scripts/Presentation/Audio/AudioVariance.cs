#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Audio
{
    /// <summary>The small random spread that keeps repeated sounds from stamping (D-084).</summary>
    public static class AudioVariance
    {
        /// <summary>The value moved by up to the variance fraction either way, steered by a number from 0 to 1; 0.5 leaves it alone.</summary>
        public static float Apply(float value, float variance, float random01)
        {
            float spread = (Mathf.Clamp01(random01) * 2f - 1f) * Mathf.Max(0f, variance);
            return value * (1f + spread);
        }

        /// <summary>Pitch scale for a body of this size: bigger bodies sound lower, a one meter body sounds as recorded.</summary>
        public static float PitchForSize(float sizeMeters)
        {
            return 1f / Mathf.Sqrt(Mathf.Max(0.25f, sizeMeters));
        }
    }
}
