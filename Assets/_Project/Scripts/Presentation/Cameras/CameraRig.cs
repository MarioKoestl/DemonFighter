#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Presentation.Demons;
using Unity.Cinemachine;
using UnityEngine;

namespace DemonFighter.Presentation.Cameras
{
    /// <summary>
    /// The player's cameras (D-013): a third-person orbit by default and a first-person view at the eyes, both turned
    /// by the mouse deltas the input adapter forwards; Cinemachine's own input reading stays off so only one class
    /// reads input. V swaps them by priority with a short blend. The orbit radius follows the demon's size (D-018).
    /// Runs after the demon bodies, so the first-person camera sits on this frame's position.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class CameraRig : MonoBehaviour, IHeadingProvider, ICameraControl
    {
        private const int ActivePriority = 20;
        private const int InactivePriority = 10;

        [SerializeField] private CinemachineBrain _brain = null!;
        [SerializeField] private CinemachineCamera _thirdPerson = null!;
        [SerializeField] private CinemachineOrbitalFollow _orbit = null!;
        [SerializeField] private CinemachineRotationComposer _composer = null!;
        [SerializeField] private CinemachineCamera _firstPerson = null!;

        private CameraRigSettings? _settings;
        private DemonView? _target;
        private float _yawDegrees;
        private float _pitchDegrees;

        /// <inheritdoc />
        public float YawRadians => _yawDegrees * Mathf.Deg2Rad;

        /// <summary>True while the first-person camera is the live one.</summary>
        public bool IsFirstPerson { get; private set; }

        /// <summary>Points both cameras at a bound demon body and starts in third person behind it.</summary>
        public void Follow(DemonView target, CameraRigSettings settings)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.Demon == null)
            {
                throw new ArgumentException("Bind the view to a demon before the camera follows it.", nameof(target));
            }

            _target = target;
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));

            float size = target.Demon.SizeMeters;
            Vector3 lookOffset = Vector3.up * (size * settings.LookHeightPerMeter);
            _thirdPerson.Target.TrackingTarget = target.transform;
            _thirdPerson.Target.CustomLookAtTarget = false;
            _orbit.Radius = settings.OrbitRadiusBase + size * settings.OrbitRadiusPerMeter;
            _orbit.TargetOffset = lookOffset;
            _orbit.VerticalAxis.Range = new Vector2(settings.PitchMinDegrees, settings.PitchMaxDegrees);
            _composer.TargetOffset = lookOffset;
            _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, settings.BlendSeconds);

            _yawDegrees = WrapDegrees(target.transform.eulerAngles.y);
            _pitchDegrees = settings.DefaultPitchDegrees;
            SetFirstPerson(false);
            ApplyAxes();
        }

        /// <inheritdoc />
        public void AddLook(Vector2 deltaPixels)
        {
            if (_settings == null)
            {
                return;
            }

            float sensitivity = _settings.LookSensitivityDegreesPerPixel;
            _yawDegrees = WrapDegrees(_yawDegrees + deltaPixels.x * sensitivity);
            float pitchDelta = deltaPixels.y * sensitivity * (_settings.InvertY ? 1f : -1f);
            _pitchDegrees = Mathf.Clamp(_pitchDegrees + pitchDelta, _settings.PitchMinDegrees, _settings.PitchMaxDegrees);
        }

        /// <inheritdoc />
        public void ToggleView()
        {
            SetFirstPerson(!IsFirstPerson);
        }

        private void Update()
        {
            if (_target == null || _settings == null)
            {
                return;
            }

            ApplyAxes();
        }

        private void ApplyAxes()
        {
            if (_target == null)
            {
                return;
            }

            _orbit.HorizontalAxis.Value = _yawDegrees;
            _orbit.VerticalAxis.Value = _pitchDegrees;
            _firstPerson.transform.SetPositionAndRotation(_target.EyePosition, Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f));
        }

        private void SetFirstPerson(bool firstPerson)
        {
            IsFirstPerson = firstPerson;
            _firstPerson.Priority = firstPerson ? ActivePriority : InactivePriority;
            _thirdPerson.Priority = firstPerson ? InactivePriority : ActivePriority;
            if (_target != null)
            {
                _target.HeadingOverride = firstPerson ? this : null;
                _target.SetBodyVisible(!firstPerson);
            }
        }

        private static float WrapDegrees(float degrees)
        {
            return Mathf.Repeat(degrees + 180f, 360f) - 180f;
        }
    }
}
