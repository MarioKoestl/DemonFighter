#nullable enable
using System;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Tuning of the procedural body motion (D-082), nested in the demon view settings: how a body breathes,
    /// stretches when it runs, bobs with its gait, how far legs, limbs, jaws and tails move, and how hits, attacks
    /// and transformations shake it. Plain values so the animator can be tested without an asset.
    /// </summary>
    [Serializable]
    public sealed class BodyAnimationTuning
    {
        [Header("Body")]
        [SerializeField] private float _breathAmplitude = 0.02f;
        [SerializeField] private float _breathSeconds = 2.6f;
        [SerializeField] private float _stretchPerSpeed = 0.12f;
        [SerializeField] private float _bobAmplitude = 0.03f;
        [SerializeField] private float _strideLengthPerMeter = 0.8f;
        [SerializeField] private float _speedSmoothing = 4f;

        [Header("Parts")]
        [SerializeField] private float _legSwingDegrees = 35f;
        [SerializeField] private float _armSwingDegrees = 20f;
        [SerializeField] private float _armIdleDegrees = 4f;
        [SerializeField] private float _armRaiseDegrees = 35f;
        [SerializeField] private float _armStrikeDegrees = 60f;
        [SerializeField] private float _jawOpenDegrees = 25f;
        [SerializeField] private float _jawSnapDegrees = 8f;
        [SerializeField] private float _tailSwayDegrees = 15f;
        [SerializeField] private float _tailSwaySeconds = 1.8f;
        [SerializeField] private float _tailSwingDegrees = 60f;

        [Header("Events")]
        [SerializeField] private float _hitWobbleDegrees = 12f;
        [SerializeField] private float _hitWobbleSeconds = 0.4f;
        [SerializeField] private float _attackContraction = 0.15f;
        [SerializeField] private float _biteNodDegrees = 8f;
        [SerializeField] private float _lungeStretch = 0.25f;
        [SerializeField] private float _transformationPulse = 0.25f;
        [SerializeField] private int _transformationPulses = 3;

        /// <summary>Scale swing of a resting body as it breathes; a fraction of its size.</summary>
        public float BreathAmplitude => _breathAmplitude;

        public float BreathSeconds => _breathSeconds;

        /// <summary>How much the body stretches along its run at full speed, with the squash across it following.</summary>
        public float StretchPerSpeed => _stretchPerSpeed;

        /// <summary>Vertical bob per gait step at full speed, in body units.</summary>
        public float BobAmplitude => _bobAmplitude;

        /// <summary>Distance one gait cycle covers, per meter of body size; sets how fast legs cycle for a speed.</summary>
        public float StrideLengthPerMeter => _strideLengthPerMeter;

        /// <summary>How quickly the smoothed speed follows the real one, per second.</summary>
        public float SpeedSmoothing => _speedSmoothing;

        public float LegSwingDegrees => _legSwingDegrees;

        public float ArmSwingDegrees => _armSwingDegrees;

        public float ArmIdleDegrees => _armIdleDegrees;

        public float ArmRaiseDegrees => _armRaiseDegrees;

        public float ArmStrikeDegrees => _armStrikeDegrees;

        public float JawOpenDegrees => _jawOpenDegrees;

        public float JawSnapDegrees => _jawSnapDegrees;

        public float TailSwayDegrees => _tailSwayDegrees;

        public float TailSwaySeconds => _tailSwaySeconds;

        public float TailSwingDegrees => _tailSwingDegrees;

        public float HitWobbleDegrees => _hitWobbleDegrees;

        public float HitWobbleSeconds => _hitWobbleSeconds;

        /// <summary>How much the body swells over a bite, a fraction of its size.</summary>
        public float AttackContraction => _attackContraction;

        public float BiteNodDegrees => _biteNodDegrees;

        /// <summary>How much the body stretches forward over a lunge, a fraction of its size.</summary>
        public float LungeStretch => _lungeStretch;

        /// <summary>Scale swing of the transformation throb (D-014), a fraction of its size.</summary>
        public float TransformationPulse => _transformationPulse;

        public int TransformationPulses => _transformationPulses;
    }
}
