#nullable enable
using System;
using UnityEngine;

namespace DemonFighter.Presentation.Audio
{
    /// <summary>
    /// The four volume knobs of the game (D-084): master, effects, ambient and music, each 0 to 1. Plain state the
    /// composition root owns and the settings persist; players apply the levels when they play or when a knob moves,
    /// which replaces an AudioMixer asset that cannot be built from code.
    /// </summary>
    public sealed class AudioMix
    {
        private float _master = 0.8f;
        private float _effects = 1f;
        private float _ambient = 1f;
        private float _music = 0.7f;

        /// <summary>Raised after any knob changed, so looping sources can follow.</summary>
        public event Action? Changed;

        public float Master => _master;

        public float Effects => _effects;

        public float Ambient => _ambient;

        public float Music => _music;

        /// <summary>The level a one-shot effect plays at.</summary>
        public float EffectsLevel => _master * _effects;

        /// <summary>The level ambient loops play at.</summary>
        public float AmbientLevel => _master * _ambient;

        /// <summary>The level music plays at.</summary>
        public float MusicLevel => _master * _music;

        /// <summary>Sets every knob at once, clamped to 0 to 1, and tells the listeners.</summary>
        public void Set(float master, float effects, float ambient, float music)
        {
            _master = Mathf.Clamp01(master);
            _effects = Mathf.Clamp01(effects);
            _ambient = Mathf.Clamp01(ambient);
            _music = Mathf.Clamp01(music);
            Changed?.Invoke();
        }
    }
}
