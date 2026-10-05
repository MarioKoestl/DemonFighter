#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.World
{
    /// <summary>Makes a lava or fissure light breathe (D-083): its intensity wanders around the base value with smooth noise, so the cavern glow is alive and never strobes.</summary>
    [RequireComponent(typeof(Light))]
    public sealed class LightFlicker : MonoBehaviour
    {
        private Light _light = null!;
        private float _baseIntensity;
        private float _amplitude;
        private float _speed;
        private float _seed;

        /// <summary>Sets how far (a fraction of the base intensity) and how fast the light wanders.</summary>
        public void Configure(float amplitude, float speed)
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            _amplitude = Mathf.Max(0f, amplitude);
            _speed = Mathf.Max(0f, speed);
            _seed = Random.Range(0f, 1000f);
        }

        private void Update()
        {
            if (_amplitude <= 0f)
            {
                return;
            }

            float noise = Mathf.PerlinNoise(_seed + Time.time * _speed, _seed * 0.37f) * 2f - 1f;
            _light.intensity = _baseIntensity * (1f + _amplitude * noise);
        }
    }
}
