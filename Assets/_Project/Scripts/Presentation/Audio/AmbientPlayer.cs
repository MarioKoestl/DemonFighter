#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.Presentation.Audio
{
    /// <summary>
    /// The sound of the cavern itself (D-084): a drone that fills both ears and a lava loop that sits on the pool
    /// nearest the listener, so the bubbling grows as the player walks toward lava and fades as they leave. Levels
    /// follow the ambient knob of the mix.
    /// </summary>
    public sealed class AmbientPlayer : IDisposable
    {
        private const float LavaMinDistance = 4f;
        private const float LavaMaxDistance = 45f;

        private readonly AudioMix _mix;
        private readonly Transform _root;
        private readonly AudioSource? _drone;
        private readonly AudioSource? _lava;
        private readonly List<Vector3> _lavaPositions;

        public AmbientPlayer(AudioMix mix, Transform parent, AudioClip? drone, AudioClip? lava, IReadOnlyList<Vector3> lavaPositions)
        {
            _mix = mix ?? throw new ArgumentNullException(nameof(mix));
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _lavaPositions = new List<Vector3>(lavaPositions ?? throw new ArgumentNullException(nameof(lavaPositions)));
            _root = new GameObject("Ambient").transform;
            _root.SetParent(parent, false);
            if (drone != null)
            {
                _drone = Loop("Cavern Drone", drone, 0f);
            }

            if (lava != null && _lavaPositions.Count > 0)
            {
                _lava = Loop("Lava Loop", lava, 1f);
                _lava.minDistance = LavaMinDistance;
                _lava.maxDistance = LavaMaxDistance;
                _lava.rolloffMode = AudioRolloffMode.Linear;
                _lava.transform.position = _lavaPositions[0];
            }

            _mix.Changed += ApplyLevels;
            ApplyLevels();
        }

        /// <summary>Moves the lava loop to the pool nearest the listener; call once per frame.</summary>
        public void Update(Vector3 listenerPosition)
        {
            if (_lava == null)
            {
                return;
            }

            Vector3 nearest = _lavaPositions[0];
            float nearestDistance = (nearest - listenerPosition).sqrMagnitude;
            for (int i = 1; i < _lavaPositions.Count; i++)
            {
                float distance = (_lavaPositions[i] - listenerPosition).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = _lavaPositions[i];
                    nearestDistance = distance;
                }
            }

            _lava.transform.position = nearest;
        }

        /// <summary>Stops the loops and removes the sources; called when the run ends.</summary>
        public void Dispose()
        {
            _mix.Changed -= ApplyLevels;
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        private void ApplyLevels()
        {
            float level = _mix.AmbientLevel;
            if (_drone != null)
            {
                _drone.volume = level;
            }

            if (_lava != null)
            {
                _lava.volume = level;
            }
        }

        private AudioSource Loop(string name, AudioClip clip, float spatialBlend)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(_root, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.Play();
            return source;
        }
    }
}
