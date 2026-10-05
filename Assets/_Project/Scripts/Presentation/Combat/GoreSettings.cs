#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Combat
{
    /// <summary>
    /// Tuning of the gore views (GAME_DESIGN, "Visible damage and gore", stage 2; D-081): blood decals on the ground,
    /// the pools that spread under corpses, the viscera bursts and the detached parts. Look only; the simulation knows
    /// nothing of it. Values live here, not in behaviour code.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Settings/Gore Settings", fileName = "GoreSettings")]
    public sealed class GoreSettings : ScriptableObject
    {
        [Header("Ground blood")]
        [SerializeField] private int _groundDecalCap = 256;
        [SerializeField] private float _splatSizeMinPerMeter = 0.4f;
        [SerializeField] private float _splatSizeMaxPerMeter = 1f;
        [SerializeField] private float _decalDepth = 0.6f;
        [SerializeField] private float _splatFadeInSeconds = 0.2f;
        [SerializeField] private int _bleedDripEveryTicks = 10;

        [Header("Corpse pools")]
        [SerializeField] private int _poolCap = 64;
        [SerializeField] private float _poolStartPerMeter = 0.7f;
        [SerializeField] private float _poolMaxPerMeter = 1.6f;
        [SerializeField] private float _poolMetersPerBiomass = 0.015f;
        [SerializeField] private float _poolGrowSeconds = 20f;

        [Header("Body blood")]
        [SerializeField] private float _bloodPerHpFraction = 2f;
        [SerializeField] private float _bleedBloodPerSecond = 0.12f;

        [Header("Fire (D-086)")]
        [SerializeField] private float _burnCharPerHpFraction = 3f;
        [SerializeField] private float _sparksPerSecondPerMeter = 30f;

        [Header("Viscera")]
        [SerializeField] private int _visceraCap = 96;
        [SerializeField] private float _severBurstPerMeter = 6f;
        [SerializeField] private float _destroyBurstPerMeter = 4f;
        [SerializeField] private float _deathBurstPerMeter = 10f;
        [SerializeField] private float _visceraSizePerMeter = 0.12f;
        [SerializeField] private float _visceraSpeed = 3.5f;
        [SerializeField] private float _visceraGravity = -14f;
        [SerializeField] private float _visceraLifeSeconds = 25f;
        [SerializeField] private float _visceraShrinkSeconds = 4f;

        [Header("Detached parts")]
        [SerializeField] private float _severedImpulse = 2.5f;
        [SerializeField] private float _severedFallbackSizePerMeter = 0.25f;

        /// <summary>How many ground decals a run keeps; the oldest is replaced when the cap is reached.</summary>
        public int GroundDecalCap => _groundDecalCap;

        public float SplatSizeMinPerMeter => _splatSizeMinPerMeter;

        public float SplatSizeMaxPerMeter => _splatSizeMaxPerMeter;

        /// <summary>How deep a decal projects, so splats still land on slopes and steps.</summary>
        public float DecalDepth => _decalDepth;

        public float SplatFadeInSeconds => _splatFadeInSeconds;

        /// <summary>A bleeding demon drips onto the ground every this many ticks.</summary>
        public int BleedDripEveryTicks => _bleedDripEveryTicks;

        public int PoolCap => _poolCap;

        /// <summary>Diameter of a fresh corpse pool per meter of body size.</summary>
        public float PoolStartPerMeter => _poolStartPerMeter;

        /// <summary>Diameter a pool spreads to per meter of body size, before the Biomass bonus.</summary>
        public float PoolMaxPerMeter => _poolMaxPerMeter;

        /// <summary>Extra pool diameter per unit of Biomass the corpse holds; a fat corpse bleeds more.</summary>
        public float PoolMetersPerBiomass => _poolMetersPerBiomass;

        public float PoolGrowSeconds => _poolGrowSeconds;

        /// <summary>Blood on a part per fraction of its HP a hit takes; 2 means a hit for half the HP soaks the part.</summary>
        public float BloodPerHpFraction => _bloodPerHpFraction;

        /// <summary>Blood a bleeding part gathers per second.</summary>
        public float BleedBloodPerSecond => _bleedBloodPerSecond;

        /// <summary>Char on a part per fraction of its health the fire takes.</summary>
        public float BurnCharPerHpFraction => _burnCharPerHpFraction;

        /// <summary>Sparks a burning body throws per second, per meter of its size.</summary>
        public float SparksPerSecondPerMeter => _sparksPerSecondPerMeter;

        public int VisceraCap => _visceraCap;

        public float SeverBurstPerMeter => _severBurstPerMeter;

        public float DestroyBurstPerMeter => _destroyBurstPerMeter;

        public float DeathBurstPerMeter => _deathBurstPerMeter;

        public float VisceraSizePerMeter => _visceraSizePerMeter;

        public float VisceraSpeed => _visceraSpeed;

        /// <summary>Downward acceleration of a flying piece; negative.</summary>
        public float VisceraGravity => _visceraGravity;

        public float VisceraLifeSeconds => _visceraLifeSeconds;

        /// <summary>The last seconds of a piece's life, over which it shrinks away.</summary>
        public float VisceraShrinkSeconds => _visceraShrinkSeconds;

        /// <summary>Velocity change a freshly severed part receives.</summary>
        public float SeveredImpulse => _severedImpulse;

        /// <summary>Size of the fallback sphere for a severed part that has no view to copy, per meter of body size.</summary>
        public float SeveredFallbackSizePerMeter => _severedFallbackSizePerMeter;
    }
}
