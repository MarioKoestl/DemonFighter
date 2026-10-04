#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Worldgen;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// The first content asset: everything that shapes one biome and what spawns in it, edited in the Inspector and
    /// converted to an immutable <see cref="BiomeSpec"/> at load (ARCHITECTURE, "Content pipeline"). Data only.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Biomes/Biome", fileName = "BI_NewBiome")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = "biome.ash.cavern";
        [SerializeField] private string _displayName = "Ash Cavern";

        [Header("Ground")]
        [SerializeField] private float _sizeMeters = 300f;
        [SerializeField] private float _cellSize = 2f;
        [SerializeField] private float _heightAmplitude = 3f;
        [SerializeField] private float _heightWavelength = 60f;
        [SerializeField] private float _wallInset = 4f;
        [SerializeField] private float _featureMargin = 10f;
        [SerializeField] private float _featureSpacing = 4f;

        [Header("Rocks")]
        [SerializeField] private int _rockCountMin = 40;
        [SerializeField] private int _rockCountMax = 80;
        [SerializeField] private float _rockSizeMin = 1.5f;
        [SerializeField] private float _rockSizeMax = 6f;

        [Header("Fissures")]
        [SerializeField] private int _fissureCountMin = 5;
        [SerializeField] private int _fissureCountMax = 10;
        [SerializeField] private float _fissureLengthMin = 8f;
        [SerializeField] private float _fissureLengthMax = 20f;
        [SerializeField] private float _fissureWidth = 1.5f;

        [Header("Pools and bone piles")]
        [SerializeField] private int _lavaPoolCount = 2;
        [SerializeField] private float _lavaPoolRadiusMin = 6f;
        [SerializeField] private float _lavaPoolRadiusMax = 12f;
        [SerializeField] private int _waterPoolCount = 3;
        [SerializeField] private float _waterPoolRadiusMin = 5f;
        [SerializeField] private float _waterPoolRadiusMax = 9f;
        [SerializeField] private int _bonePileCountMin = 8;
        [SerializeField] private int _bonePileCountMax = 14;
        [SerializeField] private float _bonePileRadiusMin = 1.5f;
        [SerializeField] private float _bonePileRadiusMax = 3f;

        [Header("Spawning")]
        [SerializeField] private int _initialBlobs = 10;
        [SerializeField] private float _spawnClusterRadius = 15f;
        [SerializeField] private float _spawnClearRadius = 30f;
        [SerializeField] private DemonTemplateDefinition _blobTemplate = new DemonTemplateDefinition();
        [SerializeField] private ArchetypeDefinition _blobArchetype = new ArchetypeDefinition();

        [Header("Elder")]
        [SerializeField] private float _elderRouteRadius = 100f;
        [SerializeField] private int _elderRouteWaypoints = 12;
        [SerializeField] private float _elderRouteJitter = 12f;
        [SerializeField] private float _elderRouteClearance = 12f;
        [SerializeField] private float _elderMinSpawnDistance = 40f;
        [SerializeField] private DemonTemplateDefinition _elderTemplate = new DemonTemplateDefinition();
        [SerializeField] private ArchetypeDefinition _elderArchetype = new ArchetypeDefinition();

        /// <summary>Stable content id.</summary>
        public string Id => _id;

        /// <summary>Builds the immutable spec the simulation uses; throws for invalid content.</summary>
        public BiomeSpec ToSpec()
        {
            var spec = new BiomeSpec
            {
                Id = _id,
                Name = _displayName,
                SizeMeters = _sizeMeters,
                CellSize = _cellSize,
                HeightAmplitude = _heightAmplitude,
                HeightWavelength = _heightWavelength,
                WallInset = _wallInset,
                FeatureMargin = _featureMargin,
                FeatureSpacing = _featureSpacing,
                RockCountMin = _rockCountMin,
                RockCountMax = _rockCountMax,
                RockSizeMin = _rockSizeMin,
                RockSizeMax = _rockSizeMax,
                FissureCountMin = _fissureCountMin,
                FissureCountMax = _fissureCountMax,
                FissureLengthMin = _fissureLengthMin,
                FissureLengthMax = _fissureLengthMax,
                FissureWidth = _fissureWidth,
                LavaPoolCount = _lavaPoolCount,
                LavaPoolRadiusMin = _lavaPoolRadiusMin,
                LavaPoolRadiusMax = _lavaPoolRadiusMax,
                WaterPoolCount = _waterPoolCount,
                WaterPoolRadiusMin = _waterPoolRadiusMin,
                WaterPoolRadiusMax = _waterPoolRadiusMax,
                BonePileCountMin = _bonePileCountMin,
                BonePileCountMax = _bonePileCountMax,
                BonePileRadiusMin = _bonePileRadiusMin,
                BonePileRadiusMax = _bonePileRadiusMax,
                InitialBlobs = _initialBlobs,
                SpawnClusterRadius = _spawnClusterRadius,
                SpawnClearRadius = _spawnClearRadius,
                ElderRouteRadius = _elderRouteRadius,
                ElderRouteWaypoints = _elderRouteWaypoints,
                ElderRouteJitter = _elderRouteJitter,
                ElderRouteClearance = _elderRouteClearance,
                ElderMinSpawnDistance = _elderMinSpawnDistance,
                BlobTemplate = _blobTemplate.ToSpec(),
                BlobArchetype = _blobArchetype.ToSpec(),
                ElderTemplate = _elderTemplate.ToSpec(),
                ElderArchetype = _elderArchetype.ToSpec(),
            };
            spec.Validate();
            return spec;
        }

        internal void ApplyDefaults(BiomeSpec spec)
        {
            _id = spec.Id;
            _displayName = spec.Name;
            _sizeMeters = spec.SizeMeters;
            _cellSize = spec.CellSize;
            _heightAmplitude = spec.HeightAmplitude;
            _heightWavelength = spec.HeightWavelength;
            _wallInset = spec.WallInset;
            _featureMargin = spec.FeatureMargin;
            _featureSpacing = spec.FeatureSpacing;
            _rockCountMin = spec.RockCountMin;
            _rockCountMax = spec.RockCountMax;
            _rockSizeMin = spec.RockSizeMin;
            _rockSizeMax = spec.RockSizeMax;
            _fissureCountMin = spec.FissureCountMin;
            _fissureCountMax = spec.FissureCountMax;
            _fissureLengthMin = spec.FissureLengthMin;
            _fissureLengthMax = spec.FissureLengthMax;
            _fissureWidth = spec.FissureWidth;
            _lavaPoolCount = spec.LavaPoolCount;
            _lavaPoolRadiusMin = spec.LavaPoolRadiusMin;
            _lavaPoolRadiusMax = spec.LavaPoolRadiusMax;
            _waterPoolCount = spec.WaterPoolCount;
            _waterPoolRadiusMin = spec.WaterPoolRadiusMin;
            _waterPoolRadiusMax = spec.WaterPoolRadiusMax;
            _bonePileCountMin = spec.BonePileCountMin;
            _bonePileCountMax = spec.BonePileCountMax;
            _bonePileRadiusMin = spec.BonePileRadiusMin;
            _bonePileRadiusMax = spec.BonePileRadiusMax;
            _initialBlobs = spec.InitialBlobs;
            _spawnClusterRadius = spec.SpawnClusterRadius;
            _spawnClearRadius = spec.SpawnClearRadius;
            _elderRouteRadius = spec.ElderRouteRadius;
            _elderRouteWaypoints = spec.ElderRouteWaypoints;
            _elderRouteJitter = spec.ElderRouteJitter;
            _elderRouteClearance = spec.ElderRouteClearance;
            _elderMinSpawnDistance = spec.ElderMinSpawnDistance;
            _blobTemplate.ApplyDefaults(spec.BlobTemplate);
            _blobArchetype.ApplyDefaults(spec.BlobArchetype);
            _elderTemplate.ApplyDefaults(spec.ElderTemplate);
            _elderArchetype.ApplyDefaults(spec.ElderArchetype);
        }

        private void OnValidate()
        {
            try
            {
                ToSpec();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.Content, "Biome asset " + name + " is invalid: " + exception.Message, this);
            }
        }
    }
}
