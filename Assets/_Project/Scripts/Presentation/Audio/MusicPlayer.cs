#nullable enable
using System;
using UnityEngine;

namespace DemonFighter.Presentation.Audio
{
    /// <summary>
    /// Plays one music track at a time and crosses over to the next (D-084): two looping sources trade places over
    /// the fade time, so menu, calm and combat tracks blend instead of cutting. Lives on the persistent bootstrap
    /// object, so the music survives scene loads. Levels follow the mix.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        private const float MinimumFadeSeconds = 0.05f;

        private AudioMix? _mix;
        private AudioSource? _front;
        private AudioSource? _back;
        private float _fadeSeconds = 2f;
        private float _blend = 1f;

        /// <summary>The track playing or fading in right now; null when silent.</summary>
        public AudioClip? Current => _front != null ? _front.clip : null;

        /// <summary>Wires the two sources and the mix; called once by the composition root.</summary>
        public void Configure(AudioMix mix, float fadeSeconds)
        {
            _mix = mix ?? throw new ArgumentNullException(nameof(mix));
            _fadeSeconds = Mathf.Max(MinimumFadeSeconds, fadeSeconds);
            _front = CreateSource("Music A");
            _back = CreateSource("Music B");
            _mix.Changed += ApplyLevels;
            ApplyLevels();
        }

        /// <summary>Crosses over to a track, or fades out when the track is null; the same track again changes nothing.</summary>
        public void Play(AudioClip? track)
        {
            if (_front == null || _back == null)
            {
                return;
            }

            if (_front.clip == track && (track == null || _front.isPlaying))
            {
                return;
            }

            AudioSource fadingOut = _front;
            _front = _back;
            _back = fadingOut;
            _front.clip = track;
            if (track != null)
            {
                _front.Play();
            }

            _blend = 0f;
            ApplyLevels();
        }

        private void Update()
        {
            if (_front == null || _back == null || _blend >= 1f)
            {
                return;
            }

            _blend = Mathf.Min(1f, _blend + Time.deltaTime / _fadeSeconds);
            ApplyLevels();
            if (_blend >= 1f && _back.isPlaying)
            {
                _back.Stop();
                _back.clip = null;
            }
        }

        private void OnDestroy()
        {
            if (_mix != null)
            {
                _mix.Changed -= ApplyLevels;
            }
        }

        private void ApplyLevels()
        {
            if (_mix == null || _front == null || _back == null)
            {
                return;
            }

            float level = _mix.MusicLevel;
            _front.volume = level * _blend;
            _back.volume = level * (1f - _blend);
        }

        private AudioSource CreateSource(string name)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }
    }
}
