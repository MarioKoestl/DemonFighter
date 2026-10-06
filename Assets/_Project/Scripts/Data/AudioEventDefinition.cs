#nullable enable
using System;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// One sound the game makes (GAME_DESIGN, "Audio"; D-084): a few clips to pick from, a volume and a pitch with
    /// their variance so repeats never sound stamped, whether it plays in 3D at a point or flat in both ears, and a
    /// cooldown that keeps a burst of hits from stacking. Skills, parts, biomes and the audio catalog reference these
    /// assets; nothing in the simulation knows them.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Audio/Audio Event", fileName = "AE_NewSound")]
    public sealed class AudioEventDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "sound.new";
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _volumeVariance = 0.1f;
        [SerializeField] private float _pitch = 1f;
        [SerializeField, Range(0f, 1f)] private float _pitchVariance = 0.1f;
        [SerializeField] private bool _spatial = true;
        [SerializeField] private float _minDistance = 2f;
        [SerializeField] private float _maxDistance = 40f;
        [SerializeField] private float _cooldownSeconds = 0.05f;

        /// <summary>Stable content id, lowercase and dotted, for example sound.bite.</summary>
        public string Id => _id;

        public float Volume => _volume;

        /// <summary>How far a play may stray from the volume, as a fraction.</summary>
        public float VolumeVariance => _volumeVariance;

        public float Pitch => _pitch;

        /// <summary>How far a play may stray from the pitch, as a fraction.</summary>
        public float PitchVariance => _pitchVariance;

        /// <summary>True when the sound sits at a point in the world; false for sounds in the player's head.</summary>
        public bool Spatial => _spatial;

        /// <summary>Full volume within this distance.</summary>
        public float MinDistance => _minDistance;

        /// <summary>Silent beyond this distance.</summary>
        public float MaxDistance => _maxDistance;

        /// <summary>Plays of this sound closer together than this are dropped.</summary>
        public float CooldownSeconds => _cooldownSeconds;

        /// <summary>True when at least one clip is assigned.</summary>
        public bool HasClips
        {
            get
            {
                for (int i = 0; i < _clips.Length; i++)
                {
                    if (_clips[i] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>One of the clips by a number from 0 to 1; null when none is assigned.</summary>
        public AudioClip? PickClip(float random01)
        {
            if (_clips.Length == 0)
            {
                return null;
            }

            int index = Mathf.Clamp((int)(Mathf.Clamp01(random01) * _clips.Length), 0, _clips.Length - 1);
            if (_clips[index] != null)
            {
                return _clips[index];
            }

            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] != null)
                {
                    return _clips[i];
                }
            }

            return null;
        }

        internal void Configure(string id, AudioClip[] clips, float volume, float volumeVariance, float pitch, float pitchVariance, bool spatial, float minDistance, float maxDistance, float cooldownSeconds)
        {
            _id = id;
            _clips = clips ?? throw new ArgumentNullException(nameof(clips));
            _volume = volume;
            _volumeVariance = volumeVariance;
            _pitch = pitch;
            _pitchVariance = pitchVariance;
            _spatial = spatial;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _cooldownSeconds = cooldownSeconds;
        }

        internal void SetClips(AudioClip[] clips)
        {
            _clips = clips ?? throw new ArgumentNullException(nameof(clips));
        }
    }
}
