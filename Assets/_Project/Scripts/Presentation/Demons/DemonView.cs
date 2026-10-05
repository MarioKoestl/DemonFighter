#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Anatomy;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// The Unity body of one demon (ARCHITECTURE, "Presentation"). Every frame it moves the CharacterController with
    /// the velocity the simulation decided, lets the controller resolve collisions and gravity, turns the body, and
    /// writes the resulting pose back into the simulation, which trusts it. Parts the demon grows get a view each
    /// through the shared <see cref="DemonFigure"/>: a primitive on the capsule, or a bound mesh on a socket anchor
    /// of the core. The body, the controller, the colors and the parts follow the size, the tier and the wounds of
    /// the demon; the <see cref="BodyAnimator"/> moves the figure and its parts (D-082). On death it becomes a corpse
    /// that lies flat and no longer moves. Per frame rather than per physics step, so the camera that follows it
    /// never stutters.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemonView : MonoBehaviour
    {
        private const float EyeHeightFraction = 0.9f;
        private const float ControllerRefitMeters = 0.005f;

        [SerializeField] private Transform _body = null!;
        [SerializeField] private Transform? _figureRoot;
        [SerializeField] private Transform? _placeholders;
        [SerializeField] private Transform? _rig;

        private readonly List<BodyPartView> _parts = new List<BodyPartView>();
        private readonly List<bool> _ownerColored = new List<bool>();
        private CharacterController _controller = null!;
        private DemonFigure _figure = null!;
        private LODGroup? _lodGroup;
        private bool _lodDirty;
        private BodyAnimator? _animator;
        private Demon? _demon;
        private DemonViewSettings? _settings;
        private PartVisuals? _visuals;
        private Material? _ownerMaterial;
        private float _verticalVelocity;
        private float _appliedSize;
        private int _appliedTier;
        private bool _bodyVisible = true;

        /// <summary>The demon this body belongs to; null before binding.</summary>
        public Demon? Demon => _demon;

        /// <summary>When set, the body faces this heading instead of its movement direction (first person).</summary>
        public IHeadingProvider? HeadingOverride { get; set; }

        /// <summary>World position of the eyes: near the top of the core, which stands on its legs (D-094).</summary>
        public Vector3 EyePosition => transform.position + (Vector3.up * (_figure.Stance + (_appliedSize * EyeHeightFraction)));

        /// <summary>World position of the middle of the core, where attacks start and a death bursts.</summary>
        public Vector3 BodyCenter => transform.position + (Vector3.up * (_figure.Stance + (_appliedSize * 0.5f)));

        /// <summary>How high the core stands on its legs right now, in meters; zero while it crawls (D-094).</summary>
        public float Stance => _figure.Stance;

        /// <summary>True once the demon died and this body lies on the ground as food.</summary>
        public bool IsCorpse { get; private set; }

        /// <summary>The part views that exist right now, the core first.</summary>
        public IReadOnlyList<BodyPartView> Parts => _parts;

        /// <summary>The transforms the body is composed of; the menu preview composes a figure the same way.</summary>
        public DemonFigure Figure => _figure;

        /// <summary>Shows or hides the body and its decorations; first person hides the player's own capsule.</summary>
        public void SetBodyVisible(bool visible)
        {
            _bodyVisible = visible;
            for (int i = 0; i < _parts.Count; i++)
            {
                _parts[i].SetVisible(visible);
            }

            _figure.SetDecorationsVisible(visible);
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

            _animator = new BodyAnimator(settings.Animation);
            _ownerMaterial = visuals.Palette.ForDemon(demon);
            _appliedTier = demon.Tier;
            _figure.Prepare(visuals, demon.Body.Core.Spec.Id, settings);
            ApplySize(demon.SizeMeters);
            _figure.InitializeCore(_parts[0], this, demon.Body.Core.Index, _ownerMaterial);
            _parts[0].RememberBasePose();
            _figure.SetDecorationsVisible(_bodyVisible);
            _lodDirty = true;
            SyncParts();
            _figure.SnapStance();
            FitController(force: true);

            // Moving a CharacterController by transform only takes effect while it is disabled.
            _controller.enabled = false;
            transform.SetPositionAndRotation(demon.Position.ToUnity(), SimulationVectors.YawToRotation(demon.Yaw));
            _controller.enabled = true;
            _verticalVelocity = 0f;
            demon.AttachBody();
        }

        /// <summary>Plays the motion of a skill use with the timing of the skill, so the strike lands on the active frames (D-082).</summary>
        public void PlayAttack(string skillId, float windupSeconds, float activeSeconds, float recoverySeconds)
        {
            if (_animator != null && _visuals != null)
            {
                _animator.Attack(_visuals.MotionForSkill(skillId), windupSeconds, activeSeconds, recoverySeconds);
            }
        }

        /// <summary>Shakes the body for a hit; the strength (0 to 1) sets how far.</summary>
        public void PlayHit(float strength)
        {
            _animator?.Hit(strength);
        }

        /// <summary>Throbs the body for the transformation time of a mutation or evolution (D-014).</summary>
        public void PlayTransformation(float seconds)
        {
            _animator?.Transform(seconds);
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
        /// Turns the body into a corpse: it stops moving, lies flat, darkens and joins the Food layer. Parts lost
        /// before death stay gone; the core, destroyed or not, is the corpse the eat aim finds.
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

            _figure.LieFlat();
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _figure = new DemonFigure(transform, _figureRoot, _body, _placeholders, _rig);
            _parts.AddRange(GetComponentsInChildren<BodyPartView>(true));
            for (int i = 0; i < _parts.Count; i++)
            {
                _ownerColored.Add(true);
            }
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
                _figure.UpdateStance(deltaTime);
                FitController(force: false);
                Animate(deltaTime);
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
                Color tint = _figure.OwnerTint(_ownerMaterial);
                for (int i = 0; i < _parts.Count; i++)
                {
                    if (_ownerColored[i])
                    {
                        _parts[i].ApplyOwner(_ownerMaterial, tint);
                    }
                }
            }
        }

        // The animator reads the speed the simulation decided and poses the whole figure; parts follow in RefreshParts.
        private void Animate(float deltaTime)
        {
            if (_animator == null)
            {
                return;
            }

            Demon demon = _demon!;
            _animator.Advance(deltaTime, demon.Velocity.ToUnity().magnitude, demon.MaxSpeed, demon.SizeMeters);
            _figure.Animate(Vector3.up * (_animator.FigureBob() * demon.SizeMeters), _animator.FigureRotation(), _animator.FigureScale());
        }

        // Stages are polled, which costs a division per part and needs no bookkeeping of missed events (D-080).
        private void RefreshParts(float deltaTime)
        {
            SyncParts();
            Body body = _demon!.Body;
            for (int i = 0; i < _parts.Count; i++)
            {
                BodyPartView view = _parts[i];
                if (body.HasPart(view.PartIndex))
                {
                    BodyPart part = body.GetPart(view.PartIndex);
                    view.ShowDamage(DamageStages.For(part.Hp / part.MaxHp, part.IsLost, _settings!.WoundedBelowHpFraction, _settings.MangledBelowHpFraction));
                    view.Dry(deltaTime, _settings.BloodDrySeconds);
                    view.Cool(deltaTime, _settings.BurnCoolSeconds);
                }

                if (!IsCorpse && _animator != null && !view.HasClip && view.Motion != PartMotion.None)
                {
                    view.SetAnimation(_animator.PartRotation(view.Motion, view.CopyIndex));
                }
            }
        }

        // Every simulation part gets a view once; the core is the body the prefab already has.
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

                BodyPartView? view = _figure.CreatePart(_visuals, part, this, _ownerMaterial, copyIndex, out bool ownerColored);
                if (view == null)
                {
                    continue;
                }

                view.RememberBasePose();
                view.SetVisible(_bodyVisible);
                _parts.Add(view);
                _ownerColored.Add(ownerColored);
                _lodDirty = true;
            }

            if (_lodDirty)
            {
                RebuildLods();
            }
        }

        // Two levels of detail without extra meshes (D-083): far away the parts drop and the core alone is drawn,
        // farther still nothing. Rebuilt when a part is added; the group filters at culling and leaves the
        // renderer flags the views toggle alone.
        private void RebuildLods()
        {
            _lodDirty = false;
            if (_settings == null || _parts.Count == 0)
            {
                return;
            }

            if (_lodGroup == null)
            {
                _lodGroup = gameObject.AddComponent<LODGroup>();
            }

            Renderer[] all = _figure.Root.GetComponentsInChildren<Renderer>(true);
            var body = _parts[0].GetComponent<Renderer>();
            _lodGroup.SetLODs(new[]
            {
                new LOD(_settings.LodPartsScreenHeight, all),
                new LOD(_settings.LodBodyScreenHeight, new[] { body }),
            });
            _lodGroup.RecalculateBounds();
        }

        private void ApplySize(float sizeMeters)
        {
            if (_settings == null)
            {
                return;
            }

            _appliedSize = sizeMeters;
            float radius = sizeMeters * _settings.RadiusPerMeter;
            _controller.radius = radius;
            _controller.stepOffset = Mathf.Min(sizeMeters * _settings.StepOffsetPerMeter, sizeMeters * 0.5f);
            _controller.skinWidth = sizeMeters * _settings.SkinWidthPerMeter;
            _controller.slopeLimit = _settings.SlopeLimitDegrees;

            _figure.ApplySize(sizeMeters, _settings.RadiusPerMeter);
            if (_parts.Count > 0)
            {
                _parts[0].RememberBasePose();
            }

            FitController(force: true);
        }

        // The capsule reaches from the feet to the top of the core, which stands on its legs (D-094); refit only on a
        // real change, since the stance eases every frame for a moment after legs grow or are lost.
        private void FitController(bool force)
        {
            float height = _appliedSize + _figure.Stance;
            if (!force && Mathf.Abs(height - _controller.height) < ControllerRefitMeters)
            {
                return;
            }

            _controller.height = height;
            _controller.center = Vector3.up * (height * 0.5f);
        }
    }
}
