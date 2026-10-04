#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Anatomy;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// The Unity body of one demon (ARCHITECTURE, "Presentation"). Every frame it moves the CharacterController with
    /// the velocity the simulation decided, lets the controller resolve collisions and gravity, turns the body, and
    /// writes the resulting pose back into the simulation, which trusts it. Its part views show the condition of
    /// every part; on death it becomes a corpse that lies flat and no longer moves. Per frame rather than per physics
    /// step, so the camera that follows it never stutters. Capsule and controller scale with the size of the demon,
    /// so a 1 m blob and a 15 m elder use the same prefab.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemonView : MonoBehaviour
    {
        private const float CapsuleMeshHeight = 2f;
        private const float CapsuleMeshRadius = 0.5f;
        private const float EyeHeightFraction = 0.9f;
        private const float PulseSeconds = 0.2f;
        private const float PulseScale = 0.15f;

        [SerializeField] private Transform _body = null!;

        private CharacterController _controller = null!;
        private BodyPartView[] _parts = Array.Empty<BodyPartView>();
        private Renderer[] _decorations = Array.Empty<Renderer>();
        private Demon? _demon;
        private DemonViewSettings? _settings;
        private float _verticalVelocity;
        private float _pulseLeft;

        /// <summary>The demon this body belongs to; null before binding.</summary>
        public Demon? Demon => _demon;

        /// <summary>When set, the body faces this heading instead of its movement direction (first person).</summary>
        public IHeadingProvider? HeadingOverride { get; set; }

        /// <summary>World position of the eyes: near the top of the body.</summary>
        public Vector3 EyePosition => transform.position + Vector3.up * (_controller.height * EyeHeightFraction);

        /// <summary>True once the demon died and this body lies on the ground as food.</summary>
        public bool IsCorpse { get; private set; }

        /// <summary>The part views, one per body part in part order.</summary>
        public IReadOnlyList<BodyPartView> Parts => _parts;

        /// <summary>Shows or hides the body and its decorations; first person hides the player's own capsule.</summary>
        public void SetBodyVisible(bool visible)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].SetVisible(visible);
            }

            for (int i = 0; i < _decorations.Length; i++)
            {
                _decorations[i].enabled = visible;
            }
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
            if (_parts.Length != demon.Body.Parts.Count)
            {
                throw new InvalidOperationException("The prefab has " + _parts.Length + " part views but the body has " + demon.Body.Parts.Count + " parts.");
            }

            ApplySize(demon.SizeMeters);
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].Initialize(this, demon.Body.Parts[i].Index, material);
            }

            // Moving a CharacterController by transform only takes effect while it is disabled.
            _controller.enabled = false;
            transform.SetPositionAndRotation(demon.Position.ToUnity(), SimulationVectors.YawToRotation(demon.Yaw));
            _controller.enabled = true;
            _verticalVelocity = 0f;
            demon.AttachBody();
        }

        /// <summary>Punches the body scale for a moment, so a bite reads even without animation.</summary>
        public void PlayAttackPulse()
        {
            _pulseLeft = PulseSeconds;
        }

        /// <summary>The view of a body part by its simulation index, or null when there is none.</summary>
        public BodyPartView? FindPart(int partIndex)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i].PartIndex == partIndex)
                {
                    return _parts[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Turns the body into a corpse: it stops moving, lies flat with the snout up, darkens and joins the Food
        /// layer. Parts lost before death stay gone; the core, destroyed or not, is the corpse the eat aim finds.
        /// </summary>
        public void BecomeCorpse(Material corpseMaterial)
        {
            if (IsCorpse || _demon == null)
            {
                return;
            }

            IsCorpse = true;
            HeadingOverride = null;
            _controller.enabled = false;
            float radius = _settings != null ? _demon.SizeMeters * _settings.RadiusPerMeter : _demon.SizeMeters * 0.3f;
            _body.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            _body.localPosition = Vector3.up * radius;
            for (int i = 0; i < _parts.Length; i++)
            {
                BodyPartView view = _parts[i];
                if (_demon.Body.HasPart(view.PartIndex))
                {
                    BodyPart part = _demon.Body.GetPart(view.PartIndex);
                    if (part.IsLost && !part.Spec.IsCore)
                    {
                        continue;
                    }
                }

                view.ShowAsCorpse(corpseMaterial);
            }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _parts = GetComponentsInChildren<BodyPartView>(true);
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            var decorations = new List<Renderer>(renderers.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].GetComponent<BodyPartView>() == null)
                {
                    decorations.Add(renderers[i]);
                }
            }

            _decorations = decorations.ToArray();
        }

        private void Update()
        {
            if (_demon == null || _settings == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            RefreshParts(deltaTime);
            if (IsCorpse)
            {
                return;
            }

            _verticalVelocity = _controller.isGrounded
                ? -_settings.GroundStickSpeed
                : _verticalVelocity + _settings.Gravity * deltaTime;
            Vector3 motion = (_demon.Velocity.ToUnity() + Vector3.up * _verticalVelocity) * deltaTime;
            _controller.Move(motion);

            float targetYaw = HeadingOverride != null
                ? HeadingOverride.YawRadians
                : _demon.Intent.HasFacing ? _demon.YawToward(_demon.Intent.Facing)
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

        // Conditions are polled, which costs a comparison per part and needs no bookkeeping of missed events.
        private void RefreshParts(float deltaTime)
        {
            float pulse = 1f;
            if (_pulseLeft > 0f)
            {
                _pulseLeft = Mathf.Max(0f, _pulseLeft - deltaTime);
                float progress = 1f - _pulseLeft / PulseSeconds;
                pulse = 1f + PulseScale * Mathf.Sin(progress * Mathf.PI);
            }

            IReadOnlyList<BodyPart> parts = _demon!.Body.Parts;
            for (int i = 0; i < _parts.Length; i++)
            {
                BodyPart? part = i < parts.Count ? parts[i] : null;
                if (part != null)
                {
                    _parts[i].ShowCondition(part.Condition);
                }

                _parts[i].SetPulse(pulse);
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
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].RememberBaseScale();
            }
        }
    }
}
