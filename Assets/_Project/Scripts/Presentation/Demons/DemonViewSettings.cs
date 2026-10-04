#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Tuning of how a demon body moves in Unity: gravity, turning and how the CharacterController scales with the
    /// body size (D-018). Values live here, not in behaviour code.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Settings/Demon View Settings", fileName = "DemonViewSettings")]
    public sealed class DemonViewSettings : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _groundStickSpeed = 2f;
        [SerializeField] private float _turnSpeedDegreesPerSecond = 540f;

        [Header("Body shape per meter of size")]
        [SerializeField] private float _radiusPerMeter = 0.3f;
        [SerializeField] private float _stepOffsetPerMeter = 0.25f;
        [SerializeField] private float _skinWidthPerMeter = 0.04f;
        [SerializeField] private float _slopeLimitDegrees = 45f;

        /// <summary>Downward acceleration in meters per second squared; negative.</summary>
        public float Gravity => _gravity;

        /// <summary>Constant downward speed while grounded, so the controller stays on slopes.</summary>
        public float GroundStickSpeed => _groundStickSpeed;

        public float TurnSpeedDegreesPerSecond => _turnSpeedDegreesPerSecond;

        public float RadiusPerMeter => _radiusPerMeter;

        public float StepOffsetPerMeter => _stepOffsetPerMeter;

        public float SkinWidthPerMeter => _skinWidthPerMeter;

        public float SlopeLimitDegrees => _slopeLimitDegrees;
    }
}
