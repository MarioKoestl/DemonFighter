#nullable enable
using System;
using DemonFighter.Data;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Procedural motion of one demon body (ASSET_PIPELINE, "Animation"; D-082): the figure breathes, stretches along
    /// its run and bobs with its gait, swells on a bite, stretches on a lunge, shakes when hit and throbs through a
    /// transformation; legs step with the gait, limbs swing and strike, jaws open and snap, tails sway and whip.
    /// Pure arithmetic over clocks the view advances, so it runs without a scene. Attack timing comes from the skill
    /// (windup, active, recovery), never from a clip, so placeholder and real bodies hit on the same frame.
    /// </summary>
    public sealed class BodyAnimator
    {
        private const float MinimumSeconds = 0.01f;
        private const float MinimumStride = 0.05f;
        private const float WobbleCycles = 3f;
        private const float WobbleScaleJitter = 0.1f;
        private const float RollPerPitch = 0.6f;

        private readonly BodyAnimationTuning _tuning;
        private float _time;
        private float _gaitPhase;
        private float _speedFraction;
        private float _speed;
        private SkillMotion _attack;
        private float _attackTime;
        private float _windup;
        private float _active;
        private float _recovery;
        private float _wobbleLeft;
        private float _wobbleStrength;
        private float _pulseLeft;
        private float _pulseSeconds;
        private int _pulseCycles;

        public BodyAnimator(BodyAnimationTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>The attack being animated, None between attacks.</summary>
        public SkillMotion Attacking => _attack;

        /// <summary>Smoothed speed as a fraction of the top speed, 0 to 1.</summary>
        public float SpeedFraction => _speedFraction;

        /// <summary>Advances the clocks: time, the smoothed speed, the gait phase from speed over stride, and the running events.</summary>
        public void Advance(float deltaTime, float speedMetersPerSecond, float maxSpeedMetersPerSecond, float sizeMeters)
        {
            _time += deltaTime;
            float targetFraction = maxSpeedMetersPerSecond > 0f ? Mathf.Clamp01(speedMetersPerSecond / maxSpeedMetersPerSecond) : 0f;
            float smoothing = deltaTime * _tuning.SpeedSmoothing;
            _speedFraction = Mathf.MoveTowards(_speedFraction, targetFraction, smoothing);
            _speed = Mathf.MoveTowards(_speed, speedMetersPerSecond, smoothing * Mathf.Max(maxSpeedMetersPerSecond, 1f));
            float stride = Mathf.Max(MinimumStride, sizeMeters * _tuning.StrideLengthPerMeter);
            _gaitPhase += deltaTime * (_speed / stride) * Mathf.PI * 2f;
            if (_gaitPhase > Mathf.PI * 2f)
            {
                _gaitPhase -= Mathf.PI * 2f;
            }

            if (_attack != SkillMotion.None)
            {
                _attackTime += deltaTime;
                if (_attackTime >= _windup + _active + _recovery)
                {
                    _attack = SkillMotion.None;
                }
            }

            if (_wobbleLeft > 0f)
            {
                _wobbleLeft = Mathf.Max(0f, _wobbleLeft - deltaTime);
            }

            if (_pulseLeft > 0f)
            {
                _pulseLeft = Mathf.Max(0f, _pulseLeft - deltaTime);
            }
        }

        /// <summary>Starts the motion of a skill use with the timing of the skill; a later attack replaces a running one.</summary>
        public void Attack(SkillMotion motion, float windupSeconds, float activeSeconds, float recoverySeconds)
        {
            if (motion == SkillMotion.None)
            {
                return;
            }

            _attack = motion;
            _attackTime = 0f;
            _windup = Mathf.Max(MinimumSeconds, windupSeconds);
            _active = Mathf.Max(MinimumSeconds, activeSeconds);
            _recovery = Mathf.Max(MinimumSeconds, recoverySeconds);
        }

        /// <summary>Shakes the body for a hit; the strength (0 to 1) sets how far, and a harder hit overrides a fading one.</summary>
        public void Hit(float strength)
        {
            float remaining = _wobbleLeft > 0f ? _wobbleStrength * (_wobbleLeft / Mathf.Max(_tuning.HitWobbleSeconds, MinimumSeconds)) : 0f;
            _wobbleStrength = Mathf.Clamp01(Mathf.Max(remaining, strength));
            _wobbleLeft = _tuning.HitWobbleSeconds;
        }

        /// <summary>Throbs the body for the transformation time of a mutation or evolution (D-014).</summary>
        public void Transform(float seconds)
        {
            _pulseSeconds = Mathf.Max(0.1f, seconds);
            _pulseLeft = _pulseSeconds;
            _pulseCycles = Mathf.Max(1, _tuning.TransformationPulses);
        }

        /// <summary>Scale of the whole figure: breathing, stretch along the run with squash across it, the bite swell, the lunge stretch, the transformation throb.</summary>
        public Vector3 FigureScale()
        {
            float breath = Mathf.Sin(_time / Mathf.Max(_tuning.BreathSeconds, MinimumSeconds) * Mathf.PI * 2f) * _tuning.BreathAmplitude;
            float stretch = _tuning.StretchPerSpeed * _speedFraction;
            float x = 1f + breath * 0.5f - stretch * 0.4f;
            float y = 1f + breath - stretch * 0.6f;
            float z = 1f - breath * 0.5f + stretch;
            float uniform = 1f;
            if (_attack == SkillMotion.Bite)
            {
                uniform += _tuning.AttackContraction * AttackEnvelope();
            }
            else if (_attack == SkillMotion.Lunge)
            {
                z += _tuning.LungeStretch * AttackEnvelope();
            }

            if (_pulseLeft > 0f)
            {
                float progress = 1f - _pulseLeft / _pulseSeconds;
                uniform += _tuning.TransformationPulse * Mathf.Abs(Mathf.Sin(progress * Mathf.PI * _pulseCycles));
            }

            uniform += Wobble() * WobbleScaleJitter;
            return new Vector3(x, y, z) * uniform;
        }

        /// <summary>Tilt of the whole figure: the hit wobble, and the nod of a bite.</summary>
        public Quaternion FigureRotation()
        {
            float wobble = Wobble();
            float pitch = wobble * _tuning.HitWobbleDegrees;
            float roll = wobble * _tuning.HitWobbleDegrees * RollPerPitch;
            if (_attack == SkillMotion.Bite)
            {
                pitch += _tuning.BiteNodDegrees * AttackEnvelope();
            }

            return Quaternion.Euler(pitch, 0f, roll);
        }

        /// <summary>
        /// How far the arms reach, 0 to 1 of their length (D-098): a little bent at rest, pulled in while a swipe winds
        /// up, straight on the strike, bent again as it settles.
        /// </summary>
        public float LimbFlex()
        {
            const float rest = 0.9f;
            const float woundUp = 0.6f;
            const float struck = 1f;
            if (_attack != SkillMotion.Swipe)
            {
                return rest;
            }

            if (_attackTime < _windup)
            {
                return Mathf.Lerp(rest, woundUp, Ease(_attackTime / Mathf.Max(_windup, MinimumSeconds)));
            }

            if (_attackTime < _windup + _active)
            {
                return Mathf.Lerp(woundUp, struck, Ease((_attackTime - _windup) / Mathf.Max(_active, MinimumSeconds)));
            }

            return Mathf.Lerp(struck, rest, Ease((_attackTime - _windup - _active) / Mathf.Max(_recovery, MinimumSeconds)));
        }

        /// <summary>Vertical bob of the whole figure in body units: a bounce per step, scaled by how fast it moves.</summary>
        public float FigureBob()
        {
            return Mathf.Abs(Mathf.Sin(_gaitPhase)) * _tuning.BobAmplitude * _speedFraction;
        }

        /// <summary>Rotation of a part relative to its rest pose, by what the part is and which copy it is; the second copy runs half a cycle behind.</summary>
        public Quaternion PartRotation(PartMotion motion, int copyIndex)
        {
            float phase = _gaitPhase + (copyIndex % 2) * Mathf.PI;
            switch (motion)
            {
                case PartMotion.Legs:
                    return Quaternion.Euler(Mathf.Sin(phase) * _tuning.LegSwingDegrees * _speedFraction, 0f, 0f);
                case PartMotion.Limb:
                    {
                        float swing = -Mathf.Sin(phase) * _tuning.ArmSwingDegrees * _speedFraction
                            + Mathf.Sin(_time * 1.3f + copyIndex) * _tuning.ArmIdleDegrees * (1f - _speedFraction);
                        if (_attack == SkillMotion.Swipe)
                        {
                            swing += Strike(_tuning.ArmRaiseDegrees, _tuning.ArmStrikeDegrees);
                        }

                        return Quaternion.Euler(swing, 0f, 0f);
                    }

                case PartMotion.Jaws:
                    return _attack == SkillMotion.Bite ? Quaternion.Euler(Strike(_tuning.JawOpenDegrees, _tuning.JawSnapDegrees), 0f, 0f) : Quaternion.identity;
                case PartMotion.Tail:
                    {
                        float sway = Mathf.Sin(_time / Mathf.Max(_tuning.TailSwaySeconds, MinimumSeconds) * Mathf.PI * 2f) * _tuning.TailSwayDegrees * (1f + _speedFraction);
                        if (_attack == SkillMotion.TailSwing)
                        {
                            sway += Strike(_tuning.TailSwingDegrees * 0.5f, _tuning.TailSwingDegrees);
                        }

                        return Quaternion.Euler(0f, sway, 0f);
                    }

                default:
                    return Quaternion.identity;
            }
        }

        // A strike in degrees: it winds up against the blow, snaps to the blow over the active window and settles back.
        private float Strike(float anticipationDegrees, float strikeDegrees)
        {
            if (_attack == SkillMotion.None)
            {
                return 0f;
            }

            if (_attackTime < _windup)
            {
                return -anticipationDegrees * Ease(_attackTime / _windup);
            }

            if (_attackTime < _windup + _active)
            {
                return Mathf.Lerp(-anticipationDegrees, strikeDegrees, Ease((_attackTime - _windup) / _active));
            }

            return Mathf.Lerp(strikeDegrees, 0f, Ease((_attackTime - _windup - _active) / _recovery));
        }

        // One bump over the whole attack, strongest in the middle.
        private float AttackEnvelope()
        {
            float total = _windup + _active + _recovery;
            return Mathf.Sin(Mathf.Clamp01(_attackTime / total) * Mathf.PI);
        }

        // A decaying shake, -1 to 1 times the strength; zero once the wobble time is over.
        private float Wobble()
        {
            if (_wobbleLeft <= 0f)
            {
                return 0f;
            }

            float t = 1f - _wobbleLeft / Mathf.Max(_tuning.HitWobbleSeconds, MinimumSeconds);
            return Mathf.Sin(t * Mathf.PI * WobbleCycles) * (1f - t) * _wobbleStrength;
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
