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
    /// writes the resulting pose back into the simulation, which trusts it. Parts the demon grows get a primitive
    /// each, sized and placed by their definition; the body, the controller, the material and the parts follow the
    /// size and tier of the demon as it mutates. On death it becomes a corpse that lies flat and no longer moves.
    /// Per frame rather than per physics step, so the camera that follows it never stutters.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemonView : MonoBehaviour
    {
        private const float CapsuleMeshHeight = 2f;
        private const float CapsuleMeshRadius = 0.5f;
        private const float EyeHeightFraction = 0.9f;
        private const float AttackPulseSeconds = 0.2f;
        private const float AttackPulseScale = 0.15f;
        private const float TransformationPulseScale = 0.25f;
        private const int TransformationPulses = 3;

        [SerializeField] private Transform _body = null!;

        private readonly List<BodyPartView> _parts = new List<BodyPartView>();
        private readonly List<bool> _ownerColored = new List<bool>();
        private CharacterController _controller = null!;
        private Renderer[] _decorations = Array.Empty<Renderer>();
        private Demon? _demon;
        private DemonViewSettings? _settings;
        private PartVisuals? _visuals;
        private Material? _ownerMaterial;
        private float _verticalVelocity;
        private float _appliedSize;
        private int _appliedTier;
        private bool _bodyVisible = true;
        private float _pulseLeft;
        private float _pulseSeconds;
        private float _pulseScale;
        private int _pulseCycles;

        /// <summary>The demon this body belongs to; null before binding.</summary>
        public Demon? Demon => _demon;

        /// <summary>When set, the body faces this heading instead of its movement direction (first person).</summary>
        public IHeadingProvider? HeadingOverride { get; set; }

        /// <summary>World position of the eyes: near the top of the body.</summary>
        public Vector3 EyePosition => transform.position + Vector3.up * (_controller.height * EyeHeightFraction);

        /// <summary>True once the demon died and this body lies on the ground as food.</summary>
        public bool IsCorpse { get; private set; }

        /// <summary>The part views that exist right now, the core first.</summary>
        public IReadOnlyList<BodyPartView> Parts => _parts;

        /// <summary>Shows or hides the body and its decorations; first person hides the player's own capsule.</summary>
        public void SetBodyVisible(bool visible)
        {
            _bodyVisible = visible;
            for (int i = 0; i < _parts.Count; i++)
            {
                _parts[i].SetVisible(visible);
            }

            for (int i = 0; i < _decorations.Length; i++)
            {
                _decorations[i].enabled = visible;
            }
        }

        /// <summary>Takes over the demon: sizes the body, colors it, grows its parts, places it at the simulation pose and starts moving it.</summary>
        public void Bind(Demon demon, DemonViewSettings settings, PartVisuals visuals)
        {
            if (_demon != null)
            {
                throw new InvalidOperationException("This view is already bound to " + _demon.Id + ".");
            }

            _demon = demon ?? throw new ArgumentNullException(nameof(demon));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            if (_parts.Count != 1)
            {
                throw new InvalidOperationException("The demon prefab needs exactly one part view, the core; found " + _parts.Count + ".");
            }

            _ownerMaterial = visuals.Palette.ForDemon(demon);
            _appliedTier = demon.Tier;
            ApplySize(demon.SizeMeters);
            _parts[0].Initialize(this, demon.Body.Core.Index, _ownerMaterial);
            _parts[0].RememberBaseScale();
            SyncParts();

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
            PlayPulse(AttackPulseSeconds, AttackPulseScale, 1);
        }

        /// <summary>Throbs the body for the transformation time of a mutation or evolution (D-014).</summary>
        public void PlayTransformation(float seconds)
        {
            PlayPulse(Mathf.Max(0.1f, seconds), TransformationPulseScale, TransformationPulses);
        }

        /// <summary>The view of a body part by its simulation index, or null when there is none.</summary>
        public BodyPartView? FindPart(int partIndex)
        {
            for (int i = 0; i < _parts.Count; i++)
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
            for (int i = 0; i < _parts.Count; i++)
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
            _parts.AddRange(GetComponentsInChildren<BodyPartView>(true));
            for (int i = 0; i < _parts.Count; i++)
            {
                _ownerColored.Add(true);
            }

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
            if (!IsCorpse)
            {
                FollowSizeAndTier();
            }

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

        // A grown demon is bigger and, past the player, wears the color of its tier (D-018).
        private void FollowSizeAndTier()
        {
            if (!Mathf.Approximately(_demon!.SizeMeters, _appliedSize))
            {
                ApplySize(_demon.SizeMeters);
            }

            if (_demon.Tier != _appliedTier && _visuals != null)
            {
                _appliedTier = _demon.Tier;
                _ownerMaterial = _visuals.Palette.ForDemon(_demon);
                for (int i = 0; i < _parts.Count; i++)
                {
                    if (_ownerColored[i])
                    {
                        _parts[i].ReplaceMaterial(_ownerMaterial);
                    }
                }
            }
        }

        // Conditions are polled, which costs a comparison per part and needs no bookkeeping of missed events.
        private void RefreshParts(float deltaTime)
        {
            SyncParts();
            float pulse = 1f;
            if (_pulseLeft > 0f)
            {
                _pulseLeft = Mathf.Max(0f, _pulseLeft - deltaTime);
                float progress = 1f - _pulseLeft / _pulseSeconds;
                pulse = 1f + _pulseScale * Mathf.Abs(Mathf.Sin(progress * Mathf.PI * _pulseCycles));
            }

            Body body = _demon!.Body;
            for (int i = 0; i < _parts.Count; i++)
            {
                BodyPartView view = _parts[i];
                if (body.HasPart(view.PartIndex))
                {
                    view.ShowCondition(body.GetPart(view.PartIndex).Condition);
                }

                view.SetPulse(pulse);
            }
        }

        // Every simulation part gets a primitive once; the core is the capsule the prefab already has.
        private void SyncParts()
        {
            if (_visuals == null || _ownerMaterial == null)
            {
                return;
            }

            IReadOnlyList<BodyPart> parts = _demon!.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                if (part.Spec.IsCore || FindPart(part.Index) != null)
                {
                    continue;
                }

                int copyIndex = 0;
                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(parts[j].Spec.Id, part.Spec.Id, StringComparison.Ordinal))
                    {
                        copyIndex++;
                    }
                }

                BodyPartView? view = _visuals.Create(part, _body, _ownerMaterial, copyIndex, out Material material, out bool ownerColored);
                if (view == null)
                {
                    continue;
                }

                view.Initialize(this, part.Index, material);
                view.RememberBaseScale();
                view.SetVisible(_bodyVisible);
                _parts.Add(view);
                _ownerColored.Add(ownerColored);
            }
        }

        private void PlayPulse(float seconds, float scale, int cycles)
        {
            _pulseSeconds = seconds;
            _pulseLeft = seconds;
            _pulseScale = scale;
            _pulseCycles = cycles;
        }

        private void ApplySize(float sizeMeters)
        {
            if (_settings == null)
            {
                return;
            }

            _appliedSize = sizeMeters;
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
            if (_parts.Count > 0)
            {
                _parts[0].RememberBaseScale();
            }
        }
    }
}
