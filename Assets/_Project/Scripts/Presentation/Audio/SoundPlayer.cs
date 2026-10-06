#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Data;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DemonFighter.Presentation.Audio
{
    /// <summary>
    /// Plays one-shot sounds from a fixed pool of AudioSources (D-084): a sound event picks a clip, a volume and a
    /// pitch within its variance, sits at a point in the world or in the player's head, and is dropped when its
    /// cooldown has not passed. When every source is busy the oldest one is cut. Levels come from the mix at play time.
    /// </summary>
    public sealed class SoundPlayer : IDisposable
    {
        private const int DefaultSources = 24;

        private readonly AudioMix _mix;
        private readonly Transform _root;
        private readonly AudioSource[] _sources;
        private readonly float[] _started;
        private readonly Dictionary<AudioEventDefinition, float> _lastPlayed = new Dictionary<AudioEventDefinition, float>();

        public SoundPlayer(AudioMix mix, Transform parent, int sources = DefaultSources)
        {
            _mix = mix ?? throw new ArgumentNullException(nameof(mix));
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _root = new GameObject("Sounds").transform;
            _root.SetParent(parent, false);
            _sources = new AudioSource[Mathf.Max(1, sources)];
            _started = new float[_sources.Length];
        }

        /// <summary>Plays a sound at a world point; false when the event is empty or still cooling down.</summary>
        public bool Play(AudioEventDefinition? sound, Vector3 position, float pitchScale = 1f)
        {
            return Play(sound, position, true, pitchScale);
        }

        /// <summary>Plays a sound flat in both ears, for things that happen to the player; false when empty or cooling down.</summary>
        public bool PlayFlat(AudioEventDefinition? sound, float pitchScale = 1f)
        {
            return Play(sound, Vector3.zero, false, pitchScale);
        }

        /// <summary>Stops everything and removes the sources; called when the run ends.</summary>
        public void Dispose()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        private bool Play(AudioEventDefinition? sound, Vector3 position, bool spatial, float pitchScale)
        {
            if (sound == null || !sound.HasClips)
            {
                return false;
            }

            float now = Time.time;
            if (_lastPlayed.TryGetValue(sound, out float last) && now - last < sound.CooldownSeconds)
            {
                return false;
            }

            AudioClip? clip = sound.PickClip(Random.value);
            if (clip == null)
            {
                return false;
            }

            _lastPlayed[sound] = now;
            int slot = Take(now);
            AudioSource source = _sources[slot];
            source.transform.position = position;
            source.clip = clip;
            source.volume = Mathf.Clamp01(AudioVariance.Apply(sound.Volume, sound.VolumeVariance, Random.value) * _mix.EffectsLevel);
            source.pitch = Mathf.Clamp(AudioVariance.Apply(sound.Pitch, sound.PitchVariance, Random.value) * pitchScale, 0.1f, 3f);
            source.spatialBlend = spatial && sound.Spatial ? 1f : 0f;
            source.minDistance = sound.MinDistance;
            source.maxDistance = Mathf.Max(sound.MinDistance + 0.1f, sound.MaxDistance);
            source.Play();
            return true;
        }

        // The first idle source, else the one that has played longest.
        private int Take(float now)
        {
            int oldest = 0;
            for (int i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] == null)
                {
                    _sources[i] = CreateSource();
                    _started[i] = now;
                    return i;
                }

                if (!_sources[i].isPlaying)
                {
                    _started[i] = now;
                    return i;
                }

                if (_started[i] < _started[oldest])
                {
                    oldest = i;
                }
            }

            _sources[oldest].Stop();
            _started[oldest] = now;
            return oldest;
        }

        private AudioSource CreateSource()
        {
            var sourceObject = new GameObject("Sound");
            sourceObject.transform.SetParent(_root, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            return source;
        }
    }
}
