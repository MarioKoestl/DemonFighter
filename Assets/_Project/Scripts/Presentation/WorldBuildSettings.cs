#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation
{
    /// <summary>
    /// Tuning of how a layout becomes Unity objects: wall size, mesh chunking, the glowing-ceiling light and ambient,
    /// fog and the lit haze behind it, the point lights of lava and fissures (D-015), how many of them cast shadows,
    /// how they flicker, how the lava flows and how many embers rise from it (D-083, D-087). Values live here, not in
    /// behaviour code.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Settings/World Build Settings", fileName = "WorldBuildSettings")]
    public sealed class WorldBuildSettings : ScriptableObject
    {
        private const int CurrentLightingVersion = 3;

        [Header("Walls")]
        [SerializeField] private float _wallHeight = 40f;
        [SerializeField] private float _wallThickness = 4f;

        [Header("Terrain mesh")]
        [SerializeField] private int _chunkCells = 30;

        [Header("Light")]
        [SerializeField] private Color _ambientColor = new Color(0.62f, 0.52f, 0.46f);
        [SerializeField] private float _ambientIntensity = 2.5f;
        [SerializeField, Range(0f, 1f)] private float _ambientEquatorFraction = 0.85f;
        [SerializeField, Range(0f, 1f)] private float _ambientGroundFraction = 0.35f;
        [SerializeField] private Color _ceilingLightColor = new Color(1f, 0.9f, 0.8f);
        [SerializeField] private float _ceilingLightIntensity = 1.6f;
        [SerializeField, Range(0f, 90f)] private float _ceilingLightPitch = 62f;
        [SerializeField] private float _ceilingLightYaw = 35f;
        [SerializeField, Range(0f, 1f)] private float _ceilingShadowStrength = 0.6f;
        [SerializeField] private Color _lavaLightColor = new Color(1f, 0.35f, 0.05f);
        [SerializeField] private float _lavaLightIntensity = 12f;
        [SerializeField] private float _lavaLightRangePerMeter = 3f;
        [SerializeField] private Color _fissureLightColor = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private float _fissureLightIntensity = 5f;
        [SerializeField] private float _fissureLightRange = 14f;

        [Header("Point light shadows and flicker (D-083)")]
        [SerializeField] private int _shadowedPointLights = 2;
        [SerializeField] private float _lavaFlickerAmplitude = 0.12f;
        [SerializeField] private float _lavaFlickerSpeed = 1.5f;
        [SerializeField] private float _fissureFlickerAmplitude = 0.2f;
        [SerializeField] private float _fissureFlickerSpeed = 3f;

        [Header("Lava surface")]
        [SerializeField] private float _lavaTilesPerMeter = 0.12f;
        [SerializeField] private Vector2 _lavaFlowTilesPerSecond = new Vector2(0.012f, 0.006f);
        [SerializeField] private float _lavaEmbersPerSquareMeter = 0.12f;

        [Header("Fog and haze")]
        [SerializeField] private Color _fogColor = new Color(0.45f, 0.32f, 0.25f);
        [SerializeField] private float _fogStartDistance = 40f;
        [SerializeField] private float _fogEndDistance = 230f;

        // Zero on an asset written before the field existed, so the generator knows to bring it up to date.
        [SerializeField, HideInInspector] private int _lightingVersion;

        public float WallHeight => _wallHeight;

        public float WallThickness => _wallThickness;

        /// <summary>Heightfield cells per terrain chunk along each axis; smaller chunks cull better.</summary>
        public int ChunkCells => _chunkCells;

        public Color AmbientColor => _ambientColor;

        /// <summary>Scales the ambient color in linear light, like the intensity of a light: 2 is twice the light.</summary>
        public float AmbientIntensity => _ambientIntensity;

        /// <summary>Ambient from the sides as a fraction of the ambient color; the ceiling glow is the sky side.</summary>
        public float AmbientEquatorFraction => _ambientEquatorFraction;

        /// <summary>Ambient from below as a fraction of the ambient color; low keeps the floor and undersides dark.</summary>
        public float AmbientGroundFraction => _ambientGroundFraction;

        /// <summary>A warm white. An orange light starves the green channel, which carries most of the brightness the eye reads.</summary>
        public Color CeilingLightColor => _ceilingLightColor;

        public float CeilingLightIntensity => _ceilingLightIntensity;

        /// <summary>How steeply the ceiling light falls, in degrees: 90 is straight down; a little less shows shapes.</summary>
        public float CeilingLightPitch => _ceilingLightPitch;

        /// <summary>From which side the ceiling light leans, in degrees around the vertical.</summary>
        public float CeilingLightYaw => _ceilingLightYaw;

        /// <summary>How dark the shadows of the ceiling glow are; 0 switches them off.</summary>
        public float CeilingShadowStrength => _ceilingShadowStrength;

        public Color LavaLightColor => _lavaLightColor;

        public float LavaLightIntensity => _lavaLightIntensity;

        /// <summary>Light range of a lava pool per meter of pool diameter.</summary>
        public float LavaLightRangePerMeter => _lavaLightRangePerMeter;

        public Color FissureLightColor => _fissureLightColor;

        public float FissureLightIntensity => _fissureLightIntensity;

        public float FissureLightRange => _fissureLightRange;

        /// <summary>How many point lights cast shadows at once, the nearest to the camera.</summary>
        public int ShadowedPointLights => _shadowedPointLights;

        public float LavaFlickerAmplitude => _lavaFlickerAmplitude;

        public float LavaFlickerSpeed => _lavaFlickerSpeed;

        public float FissureFlickerAmplitude => _fissureFlickerAmplitude;

        public float FissureFlickerSpeed => _fissureFlickerSpeed;

        /// <summary>Texture tiles across a meter of lava pool; low, so the crust plates are about a meter and the repeat hides.</summary>
        public float LavaTilesPerMeter => _lavaTilesPerMeter;

        /// <summary>How fast the lava crust drifts, in tiles per second.</summary>
        public Vector2 LavaFlowTilesPerSecond => _lavaFlowTilesPerSecond;

        /// <summary>Embers rising over a lava pool per second and square meter of its surface (D-086).</summary>
        public float LavaEmbersPerSquareMeter => _lavaEmbersPerSquareMeter;

        /// <summary>The glowing ash haze: the fog and, behind everything, the color of the cave above.</summary>
        public Color FogColor => _fogColor;

        public float FogStartDistance => _fogStartDistance;

        public float FogEndDistance => _fogEndDistance;

        /// <summary>True for an asset written before the current lighting of D-087; the generator applies it once.</summary>
        internal bool NeedsLightingDefaults => _lightingVersion < CurrentLightingVersion;

        /// <summary>The lighting of D-087: a strong warm-white ceiling light leaning a little, a bright ambient fill, a glowing haze, hotter lava with larger crust plates.</summary>
        internal void ApplyLightingDefaults()
        {
            var defaults = CreateInstance<WorldBuildSettings>();
            try
            {
                _ambientColor = defaults._ambientColor;
                _ambientIntensity = defaults._ambientIntensity;
                _ambientEquatorFraction = defaults._ambientEquatorFraction;
                _ambientGroundFraction = defaults._ambientGroundFraction;
                _ceilingLightColor = defaults._ceilingLightColor;
                _ceilingLightIntensity = defaults._ceilingLightIntensity;
                _ceilingLightPitch = defaults._ceilingLightPitch;
                _ceilingLightYaw = defaults._ceilingLightYaw;
                _ceilingShadowStrength = defaults._ceilingShadowStrength;
                _lavaLightIntensity = defaults._lavaLightIntensity;
                _lavaLightRangePerMeter = defaults._lavaLightRangePerMeter;
                _fissureLightIntensity = defaults._fissureLightIntensity;
                _shadowedPointLights = defaults._shadowedPointLights;
                _lavaEmbersPerSquareMeter = defaults._lavaEmbersPerSquareMeter;
                _lavaTilesPerMeter = defaults._lavaTilesPerMeter;
                _fogColor = defaults._fogColor;
                _fogStartDistance = defaults._fogStartDistance;
                _fogEndDistance = defaults._fogEndDistance;
                _lightingVersion = CurrentLightingVersion;
            }
            finally
            {
                DestroyImmediate(defaults);
            }
        }
    }
}
