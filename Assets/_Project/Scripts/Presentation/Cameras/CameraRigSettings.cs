#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Cameras
{
    /// <summary>
    /// Tuning of the player cameras: mouse sensitivity, pitch limits, how the orbit grows with the demon (D-018) and
    /// the blend between the two views. Values live here, not in behaviour code.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Settings/Camera Rig Settings", fileName = "CameraRigSettings")]
    public sealed class CameraRigSettings : ScriptableObject
    {
        [Header("Mouse")]
        [SerializeField] private float _lookSensitivityDegreesPerPixel = 0.12f;
        [SerializeField] private bool _invertY;

        [Header("Pitch")]
        [SerializeField] private float _pitchMinDegrees = -20f;
        [SerializeField] private float _pitchMaxDegrees = 60f;
        [SerializeField] private float _defaultPitchDegrees = 15f;

        [Header("Third person orbit")]
        [SerializeField] private float _orbitRadiusBase = 2f;
        [SerializeField] private float _orbitRadiusPerMeter = 2.5f;
        [SerializeField] private float _lookHeightPerMeter = 0.6f;

        [Header("Blending")]
        [SerializeField] private float _blendSeconds = 0.35f;

        public float LookSensitivityDegreesPerPixel => _lookSensitivityDegreesPerPixel;

        public bool InvertY => _invertY;

        public float PitchMinDegrees => _pitchMinDegrees;

        public float PitchMaxDegrees => _pitchMaxDegrees;

        public float DefaultPitchDegrees => _defaultPitchDegrees;

        /// <summary>Orbit distance for a demon of zero size; the size term is added on top.</summary>
        public float OrbitRadiusBase => _orbitRadiusBase;

        public float OrbitRadiusPerMeter => _orbitRadiusPerMeter;

        /// <summary>Height of the orbit look target as a fraction of the body size.</summary>
        public float LookHeightPerMeter => _lookHeightPerMeter;

        public float BlendSeconds => _blendSeconds;
    }
}
