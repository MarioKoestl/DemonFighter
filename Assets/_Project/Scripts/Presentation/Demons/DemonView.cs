#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// The Unity body of one demon (ARCHITECTURE, "Presentation"). Every frame it moves the CharacterController with
    /// the velocity the simulation decided, lets the controller resolve collisions and gravity, turns the body, and
    /// writes the resulting pose back into the simulation, which trusts it. Per frame rather than per physics step,
    /// so the camera that follows it never stutters. Capsule and controller scale with the demon's size, so a 1 m
    /// blob and a 15 m elder use the same prefab.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemonView : MonoBehaviour
    {
        private const float CapsuleMeshHeight = 2f;
        private const float CapsuleMeshRadius = 0.5f;
        private const float EyeHeightFraction = 0.9f;

        [SerializeField] private Transform _body = null!;

        private CharacterController _controller = null!;
        private Renderer _bodyRenderer = null!;
        private Demon? _demon;
        private DemonViewSettings? _settings;
        private float _verticalVelocity;

        /// <summary>The demon this body belongs to; null before binding.</summary>
        public Demon? Demon => _demon;

        /// <summary>When set, the body faces this heading instead of its movement direction (first person).</summary>
        public IHeadingProvider? HeadingOverride { get; set; }

        /// <summary>World position of the eyes: near the top of the body.</summary>
        public Vector3 EyePosition => transform.position + Vector3.up * (_controller.height * EyeHeightFraction);

        /// <summary>Shows or hides the body mesh; first person hides the player's own capsule.</summary>
        public void SetBodyVisible(bool visible)
        {
            _bodyRenderer.enabled = visible;
        }

        /// <summary>Takes over the demon: sizes the body, places it at the simulation pose and starts moving it.</summary>
        public void Bind(Demon demon, DemonViewSettings settings, Material material)
        {
            if (_demon != null)
            {
                throw new InvalidOperationException("This view is already bound to " + _demon.Id + ".");
            }

            _demon = demon ?? throw new ArgumentNullException(nameof(demon));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _bodyRenderer.sharedMaterial = material;
            ApplySize(demon.SizeMeters);

            // Moving a CharacterController by transform only takes effect while it is disabled.
            _controller.enabled = false;
            transform.SetPositionAndRotation(demon.Position.ToUnity(), SimulationVectors.YawToRotation(demon.Yaw));
            _controller.enabled = true;
            _verticalVelocity = 0f;
            demon.AttachBody();
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _bodyRenderer = _body.GetComponent<Renderer>();
        }

        private void Update()
        {
            if (_demon == null || _settings == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            _verticalVelocity = _controller.isGrounded
                ? -_settings.GroundStickSpeed
                : _verticalVelocity + _settings.Gravity * deltaTime;
            Vector3 motion = (_demon.Velocity.ToUnity() + Vector3.up * _verticalVelocity) * deltaTime;
            _controller.Move(motion);

            float targetYaw = HeadingOverride != null
                ? HeadingOverride.YawRadians
                : _demon.Intent.IsMoving ? _demon.YawToward(_demon.Intent.Direction) : _demon.Yaw;
            Quaternion target = SimulationVectors.YawToRotation(targetYaw);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _settings.TurnSpeedDegreesPerSecond * deltaTime);

            _demon.SetPose(transform.position.ToSimulation(), SimulationVectors.RotationToYaw(transform.rotation));
        }

        private void OnDestroy()
        {
            if (_demon != null)
            {
                _demon.DetachBody();
                _demon = null;
            }
        }

        private void ApplySize(float sizeMeters)
        {
            if (_settings == null)
            {
                return;
            }

            float radius = sizeMeters * _settings.RadiusPerMeter;
            _controller.height = sizeMeters;
            _controller.radius = radius;
            _controller.center = Vector3.up * (sizeMeters * 0.5f);
            _controller.stepOffset = Mathf.Min(sizeMeters * _settings.StepOffsetPerMeter, sizeMeters * 0.5f);
            _controller.skinWidth = sizeMeters * _settings.SkinWidthPerMeter;
            _controller.slopeLimit = _settings.SlopeLimitDegrees;

            // The capsule mesh is 2 units tall with radius 0.5; scale it to the controller's shape.
            _body.localScale = new Vector3(radius / CapsuleMeshRadius, sizeMeters / CapsuleMeshHeight, radius / CapsuleMeshRadius);
            _body.localPosition = Vector3.up * (sizeMeters * 0.5f);
        }
    }
}
