#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation
{
    /// <summary>
    /// Tuning of how a layout becomes Unity objects: wall size, mesh chunking, the glowing-ceiling ambient light,
    /// fog, and the point lights of lava and fissures (D-015). Values live here, not in behaviour code.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Settings/World Build Settings", fileName = "WorldBuildSettings")]
    public sealed class WorldBuildSettings : ScriptableObject
    {
        [Header("Walls")]
        [SerializeField] private float _wallHeight = 40f;
        [SerializeField] private float _wallThickness = 4f;

        [Header("Terrain mesh")]
        [SerializeField] private int _chunkCells = 30;

        [Header("Light")]
        [SerializeField] private Color _ambientColor = new Color(0.55f, 0.32f, 0.22f);
        [SerializeField] private float _ambientIntensity = 1.1f;
        [SerializeField] private Color _ceilingLightColor = new Color(1f, 0.62f, 0.4f);
        [SerializeField] private float _ceilingLightIntensity = 0.35f;
        [SerializeField] private Color _lavaLightColor = new Color(1f, 0.35f, 0.05f);
        [SerializeField] private float _lavaLightIntensity = 8f;
        [SerializeField] private float _lavaLightRangePerMeter = 2.5f;
        [SerializeField] private Color _fissureLightColor = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private float _fissureLightIntensity = 4f;
        [SerializeField] private float _fissureLightRange = 14f;

        [Header("Fog")]
        [SerializeField] private Color _fogColor = new Color(0.12f, 0.06f, 0.05f);
        [SerializeField] private float _fogStartDistance = 60f;
        [SerializeField] private float _fogEndDistance = 260f;

        public float WallHeight => _wallHeight;

        public float WallThickness => _wallThickness;

        /// <summary>Heightfield cells per terrain chunk along each axis; smaller chunks cull better.</summary>
        public int ChunkCells => _chunkCells;

        public Color AmbientColor => _ambientColor;

        public float AmbientIntensity => _ambientIntensity;

        public Color CeilingLightColor => _ceilingLightColor;

        public float CeilingLightIntensity => _ceilingLightIntensity;

        public Color LavaLightColor => _lavaLightColor;

        public float LavaLightIntensity => _lavaLightIntensity;

        /// <summary>Light range of a lava pool per meter of pool diameter.</summary>
        public float LavaLightRangePerMeter => _lavaLightRangePerMeter;

        public Color FissureLightColor => _fissureLightColor;

        public float FissureLightIntensity => _fissureLightIntensity;

        public float FissureLightRange => _fissureLightRange;

        public Color FogColor => _fogColor;

        public float FogStartDistance => _fogStartDistance;

        public float FogEndDistance => _fogEndDistance;
    }
}
