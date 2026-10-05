#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Tuning of how a demon body moves and looks in Unity: gravity, turning, how the CharacterController scales with
    /// the body size (D-018), where the damage stages of a part begin (D-080), how fast blood dries off a body
    /// (D-068), the procedural motion (D-082) and when distant bodies drop detail (D-083). Values live here, not in
    /// behaviour code.
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

        [Header("Damage look (D-080)")]
        [SerializeField, Range(0f, 1f)] private float _woundedBelowHpFraction = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _mangledBelowHpFraction = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _textureTintBlend = 0.3f;
        [SerializeField] private float _bloodDrySeconds = 15f;
        [SerializeField] private float _burnCoolSeconds = 12f;

        [Header("Motion (D-082)")]
        [SerializeField] private BodyAnimationTuning _animation = new BodyAnimationTuning();

        [Header("Level of detail (D-083)")]
        [SerializeField, Range(0f, 1f)] private float _lodPartsScreenHeight = 0.04f;
        [SerializeField, Range(0f, 1f)] private float _lodBodyScreenHeight = 0.006f;

        /// <summary>Downward acceleration in meters per second squared; negative.</summary>
        public float Gravity => _gravity;

        /// <summary>Constant downward speed while grounded, so the controller stays on slopes.</summary>
        public float GroundStickSpeed => _groundStickSpeed;

        public float TurnSpeedDegreesPerSecond => _turnSpeedDegreesPerSecond;

        public float RadiusPerMeter => _radiusPerMeter;

        public float StepOffsetPerMeter => _stepOffsetPerMeter;

        public float SkinWidthPerMeter => _skinWidthPerMeter;

        public float SlopeLimitDegrees => _slopeLimitDegrees;

        /// <summary>A part below this fraction of its HP shows its wounded mesh; the default equals the simulation's wounded threshold.</summary>
        public float WoundedBelowHpFraction => _woundedBelowHpFraction;

        /// <summary>A part below this fraction of its HP shows its mangled mesh.</summary>
        public float MangledBelowHpFraction => _mangledBelowHpFraction;

        /// <summary>
        /// How strongly the owner color (player teal, AI tier) tints a part that wears an imported material; 0 keeps the
        /// texture as is. Kept low because a tint multiplies: teal over a red texture leaves it nearly black (D-087).
        /// </summary>
        public float OwnerTintBlend => _textureTintBlend;

        /// <summary>Seconds a drenched part takes to dry clean again (D-068).</summary>
        public float BloodDrySeconds => _bloodDrySeconds;

        /// <summary>Seconds a fully charred part takes to lose its char once out of the fire (D-086).</summary>
        public float BurnCoolSeconds => _burnCoolSeconds;

        /// <summary>The procedural motion of bodies and parts.</summary>
        public BodyAnimationTuning Animation => _animation;

        /// <summary>Below this share of the screen height a body drops its parts and draws the core alone.</summary>
        public float LodPartsScreenHeight => _lodPartsScreenHeight;

        /// <summary>Below this share of the screen height a body is not drawn at all.</summary>
        public float LodBodyScreenHeight => _lodBodyScreenHeight;
    }
}
