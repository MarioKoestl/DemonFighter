#nullable enable
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// The sounds that belong to no single skill, part or biome (D-084): footsteps, eating, the cues of mutation,
    /// evolution, level and threat, deaths, the fallbacks for hits and lost parts, and the menu music. Generated once
    /// with the synthesized placeholders; afterwards the asset is the truth and Mario swaps clips in the events.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Audio/Audio Catalog", fileName = "AudioCatalog")]
    public sealed class AudioCatalogDefinition : ScriptableObject
    {
        [Header("Body")]
        [SerializeField] private AudioEventDefinition? _footstep;
        [SerializeField] private AudioEventDefinition? _eat;
        [SerializeField] private float _footstepStridePerMeter = 0.8f;

        [Header("Combat fallbacks")]
        [SerializeField] private AudioEventDefinition? _wetImpact;
        [SerializeField] private AudioEventDefinition? _partSevered;
        [SerializeField] private AudioEventDefinition? _partDestroyed;
        [SerializeField] private AudioEventDefinition? _demonDeath;
        [SerializeField] private AudioEventDefinition? _playerDeath;

        [Header("Progress")]
        [SerializeField] private AudioEventDefinition? _mutationStart;
        [SerializeField] private AudioEventDefinition? _mutationComplete;
        [SerializeField] private AudioEventDefinition? _evolved;
        [SerializeField] private AudioEventDefinition? _levelUp;
        [SerializeField] private AudioEventDefinition? _threatRise;

        [Header("Fire")]
        [SerializeField] private AudioEventDefinition? _burn;

        [Header("Music")]
        [SerializeField] private AudioClip? _menuTrack;
        [SerializeField] private float _musicFadeSeconds = 2f;

        public AudioEventDefinition? Footstep => _footstep;

        public AudioEventDefinition? Eat => _eat;

        /// <summary>Distance a body travels between two footsteps, per meter of its size.</summary>
        public float FootstepStridePerMeter => _footstepStridePerMeter;

        /// <summary>A hit that lands on flesh, when the skill has no hit sound of its own.</summary>
        public AudioEventDefinition? WetImpact => _wetImpact;

        /// <summary>A part torn off, when the part has no sound of its own.</summary>
        public AudioEventDefinition? PartSevered => _partSevered;

        public AudioEventDefinition? PartDestroyed => _partDestroyed;

        public AudioEventDefinition? DemonDeath => _demonDeath;

        public AudioEventDefinition? PlayerDeath => _playerDeath;

        public AudioEventDefinition? MutationStart => _mutationStart;

        public AudioEventDefinition? MutationComplete => _mutationComplete;

        public AudioEventDefinition? Evolved => _evolved;

        public AudioEventDefinition? LevelUp => _levelUp;

        public AudioEventDefinition? ThreatRise => _threatRise;

        /// <summary>Flesh sizzling in lava or on a fissure (D-086).</summary>
        public AudioEventDefinition? Burn => _burn;

        /// <summary>The loop behind the main menu; the biome brings the tracks of a run.</summary>
        public AudioClip? MenuTrack => _menuTrack;

        /// <summary>Seconds a music change takes to cross over.</summary>
        public float MusicFadeSeconds => _musicFadeSeconds;

        internal void SetBurn(AudioEventDefinition burn)
        {
            _burn = burn;
        }

        internal void Configure(
            AudioEventDefinition footstep,
            AudioEventDefinition eat,
            AudioEventDefinition wetImpact,
            AudioEventDefinition partSevered,
            AudioEventDefinition partDestroyed,
            AudioEventDefinition demonDeath,
            AudioEventDefinition playerDeath,
            AudioEventDefinition mutationStart,
            AudioEventDefinition mutationComplete,
            AudioEventDefinition evolved,
            AudioEventDefinition levelUp,
            AudioEventDefinition threatRise,
            AudioClip menuTrack)
        {
            _footstep = footstep;
            _eat = eat;
            _wetImpact = wetImpact;
            _partSevered = partSevered;
            _partDestroyed = partDestroyed;
            _demonDeath = demonDeath;
            _playerDeath = playerDeath;
            _mutationStart = mutationStart;
            _mutationComplete = mutationComplete;
            _evolved = evolved;
            _levelUp = levelUp;
            _threatRise = threatRise;
            _menuTrack = menuTrack;
        }
    }
}
