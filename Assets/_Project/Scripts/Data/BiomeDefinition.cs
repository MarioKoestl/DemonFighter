#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;
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

        [Header("Hazards (D-086)")]
        [SerializeField] private float _lavaBurnFractionPerSecond = 0.2f;
        [SerializeField] private float _fissureBurnFractionPerSecond = 0.05f;
        [SerializeField] private float _hazardAvoidMarginMeters = 1.5f;

        [Header("Spawning")]
        [SerializeField] private int _initialBlobs = 16;
        [SerializeField] private float _spawnClusterRadius = 30f;
        [SerializeField] private float _spawnClearRadius = 30f;
        [SerializeField] private float _respawnSeconds = 6f;
        [SerializeField] private float _respawnMinDistance = 25f;
        [SerializeField] private float _respawnMaxDistance = 55f;
        [SerializeField] private float _threatPerMinute = 0.5f;
        [SerializeField] private int _threatMaxLevel = 10;
        [SerializeField] private int _blobsPerThreatLevel = 2;
        [SerializeField] private float _respawnSpeedupPerThreat = 0.2f;
        [SerializeField, HideInInspector] private int _contentVersion;

        private const int PacingContentVersion = 3;
        private const int CurrentContentVersion = 4;
        [SerializeField] private SpawnEntryDefinition[] _spawnTable = Array.Empty<SpawnEntryDefinition>();
        [SerializeField] private DemonDefinition _blobDemon = null!;
        [SerializeField] private ArchetypeDefinition _blobArchetype = new ArchetypeDefinition();
        [SerializeField] private ArchetypeChoiceDefinition[] _blobArchetypes = Array.Empty<ArchetypeChoiceDefinition>();

        [Header("Audio")]
        [SerializeField] private AudioClip? _ambientLoop;
        [SerializeField] private AudioClip? _lavaLoop;
        [SerializeField] private AudioClip? _calmTrack;
        [SerializeField] private AudioClip? _combatTrack;

        [Header("Elder")]
        [SerializeField] private float _elderRouteRadius = 100f;
        [SerializeField] private int _elderRouteWaypoints = 12;
        [SerializeField] private float _elderRouteJitter = 12f;
        [SerializeField] private float _elderRouteClearance = 12f;
        [SerializeField] private float _elderMinSpawnDistance = 40f;
        [SerializeField] private DemonDefinition _elderDemon = null!;
        [SerializeField] private ArchetypeDefinition _elderArchetype = new ArchetypeDefinition();

        /// <summary>Stable content id.</summary>
        public string Id => _id;

        /// <summary>True for an asset written before the pacing of D-078; the generator applies the spec numbers once.</summary>
        internal bool NeedsPacingDefaults => _contentVersion < PacingContentVersion;

        /// <summary>True for an asset written before lava and fissures burned (D-086); the generator adds the burn numbers once.</summary>
        internal bool NeedsHazardDefaults => _contentVersion < CurrentContentVersion;

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
                LavaBurnFractionPerSecond = _lavaBurnFractionPerSecond,
                FissureBurnFractionPerSecond = _fissureBurnFractionPerSecond,
                HazardAvoidMarginMeters = _hazardAvoidMarginMeters,
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
                RespawnSeconds = _respawnSeconds,
                RespawnMinDistance = _respawnMinDistance,
                RespawnMaxDistance = _respawnMaxDistance,
                ThreatPerMinute = _threatPerMinute,
                ThreatMaxLevel = _threatMaxLevel,
                BlobsPerThreatLevel = _blobsPerThreatLevel,
                RespawnSpeedupPerThreat = _respawnSpeedupPerThreat,
                SpawnTable = SpawnTableSpecs(),
                ElderRouteRadius = _elderRouteRadius,
                ElderRouteWaypoints = _elderRouteWaypoints,
                ElderRouteJitter = _elderRouteJitter,
                ElderRouteClearance = _elderRouteClearance,
                ElderMinSpawnDistance = _elderMinSpawnDistance,
                BlobDemon = RequireDemon(_blobDemon, "blob").ToSpec(),
                BlobArchetype = _blobArchetype.ToSpec(),
                BlobArchetypes = BlobArchetypeSpecs(),
                ElderDemon = RequireDemon(_elderDemon, "elder").ToSpec(),
                ElderArchetype = _elderArchetype.ToSpec(),
            };
            spec.Validate();
            return spec;
        }

        /// <summary>Gives the asset the burn numbers of the spec without touching anything else.</summary>
        internal void ApplyHazardDefaults(BiomeSpec spec)
        {
            _lavaBurnFractionPerSecond = spec.LavaBurnFractionPerSecond;
            _fissureBurnFractionPerSecond = spec.FissureBurnFractionPerSecond;
            _hazardAvoidMarginMeters = spec.HazardAvoidMarginMeters;
            _contentVersion = CurrentContentVersion;
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
            ApplyHazardDefaults(spec);
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
            _respawnSeconds = spec.RespawnSeconds;
            _respawnMinDistance = spec.RespawnMinDistance;
            _respawnMaxDistance = spec.RespawnMaxDistance;
            _threatPerMinute = spec.ThreatPerMinute;
            _threatMaxLevel = spec.ThreatMaxLevel;
            _blobsPerThreatLevel = spec.BlobsPerThreatLevel;
            _respawnSpeedupPerThreat = spec.RespawnSpeedupPerThreat;
            _contentVersion = CurrentContentVersion;
            _elderRouteRadius = spec.ElderRouteRadius;
            _elderRouteWaypoints = spec.ElderRouteWaypoints;
            _elderRouteJitter = spec.ElderRouteJitter;
            _elderRouteClearance = spec.ElderRouteClearance;
            _elderMinSpawnDistance = spec.ElderMinSpawnDistance;
            _blobArchetype.ApplyDefaults(spec.BlobArchetype);
            _elderArchetype.ApplyDefaults(spec.ElderArchetype);
        }

        internal void SetDemons(DemonDefinition blob, DemonDefinition elder)
        {
            _blobDemon = blob;
            _elderDemon = elder;
        }

        /// <summary>True once the asset lists blob personalities; an older asset gets the default three from the generator (D-071).</summary>
        internal bool HasBlobArchetypes => _blobArchetypes.Length > 0;

        internal void SetBlobArchetypes(ArchetypeChoiceDefinition[] choices)
        {
            _blobArchetypes = choices;
        }

        private ArchetypeChoice[] BlobArchetypeSpecs()
        {
            var choices = new ArchetypeChoice[_blobArchetypes.Length];
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i] = _blobArchetypes[i].ToSpec(_id);
            }

            return choices;
        }

        /// <summary>True once the asset carries a spawn table; an older asset gets the default one from the generator (D-070).</summary>
        /// <summary>The drone of the cavern, looped flat in both ears (D-084).</summary>
        public AudioClip? AmbientLoop => _ambientLoop;

        /// <summary>The loop that sits on the lava pool nearest the player.</summary>
        public AudioClip? LavaLoop => _lavaLoop;

        /// <summary>Music while the player is out of combat.</summary>
        public AudioClip? CalmTrack => _calmTrack;

        /// <summary>Music while the player fights.</summary>
        public AudioClip? CombatTrack => _combatTrack;

        internal bool HasAudio => _ambientLoop != null;

        internal void SetAudio(AudioClip ambientLoop, AudioClip lavaLoop, AudioClip calmTrack, AudioClip combatTrack)
        {
            _ambientLoop = ambientLoop;
            _lavaLoop = lavaLoop;
            _calmTrack = calmTrack;
            _combatTrack = combatTrack;
        }

        internal bool HasSpawnTable => _spawnTable.Length > 0;

        internal void SetSpawnTable(SpawnEntryDefinition[] entries)
        {
            _spawnTable = entries;
        }

        private SpawnEntry[] SpawnTableSpecs()
        {
            var entries = new SpawnEntry[_spawnTable.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = _spawnTable[i].ToSpec(_id);
            }

            return entries;
        }

        private DemonDefinition RequireDemon(DemonDefinition definition, string role)
        {
            if (definition == null)
            {
                throw new ContentException("Biome " + _id + " has no " + role + " demon.");
            }

            return definition;
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
