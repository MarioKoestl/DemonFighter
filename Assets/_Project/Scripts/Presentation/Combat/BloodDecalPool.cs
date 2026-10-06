#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DemonFighter.Presentation.Combat
{
    /// <summary>
    /// Blood on the ground (GAME_DESIGN, "Visible damage and gore", stage 2; D-081): URP decal projectors that wrap
    /// splats over the terrain under a hit and pools under corpses that spread with time and Biomass. Splats pile up
    /// over the run to a cap and the oldest is replaced when it is reached, so a long fight leaves its history on the
    /// floor without ever allocating past the pool size. Blood on bodies is the skin shader's job (BodyPartView).
    /// Look only; the simulation knows nothing about it.
    /// </summary>
    public sealed class BloodDecalPool
    {
        private const float GroundLift = 0.25f;
        private const float GroundProbeHeight = 2f;
        private const float GroundProbeDepth = 6f;
        private const float DrawDistance = 150f;
        private static readonly Quaternion FacingDown = Quaternion.Euler(90f, 0f, 0f);

        private readonly GoreSettings _settings;
        private readonly Material[] _splats;
        private readonly Material _pool;
        private readonly Transform _root;
        private readonly Decal?[] _ground;
        private readonly Decal?[] _pools;
        private int _nextGround;
        private int _nextPool;

        public BloodDecalPool(GoreSettings settings, Material[] splats, Material pool, Transform parent)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _splats = splats ?? throw new ArgumentNullException(nameof(splats));
            if (_splats.Length == 0)
            {
                throw new ArgumentException("At least one splat material is needed.", nameof(splats));
            }

            _pool = pool != null ? pool : throw new ArgumentNullException(nameof(pool));
            _root = new GameObject("Blood").transform;
            _root.SetParent(parent, false);
            _ground = new Decal?[Mathf.Max(1, settings.GroundDecalCap)];
            _pools = new Decal?[Mathf.Max(1, settings.PoolCap)];
        }

        /// <summary>Puts a splat on the ground under a world position, sized by the bleeding body; it fades in over a moment.</summary>
        public void SplashGround(Vector3 position, float sizeMeters)
        {
            Vector3 point = FindGround(position);
            Decal splat = Take(_ground, ref _nextGround);
            float diameter = sizeMeters * Random.Range(_settings.SplatSizeMinPerMeter, _settings.SplatSizeMaxPerMeter);
            Place(splat.Projector, point, diameter, _splats[Random.Range(0, _splats.Length)]);
            splat.Projector.fadeFactor = 0f;
            splat.Born = Time.time;
            splat.Size = sizeMeters;
            splat.Biomass = 0f;
        }

        /// <summary>Lays a pool under a corpse that spreads over the next seconds, wider for a body that held more Biomass.</summary>
        public void PoolUnderCorpse(Vector3 position, float sizeMeters, float biomass)
        {
            Vector3 point = FindGround(position);
            Decal pool = Take(_pools, ref _nextPool);
            Place(pool.Projector, point, GoreMath.PoolDiameter(sizeMeters, biomass, _settings.PoolStartPerMeter, _settings.PoolMaxPerMeter, _settings.PoolMetersPerBiomass, 0f), _pool);
            pool.Projector.fadeFactor = 1f;
            pool.Born = Time.time;
            pool.Size = sizeMeters;
            pool.Biomass = biomass;
        }

        /// <summary>Fades fresh splats in and spreads young pools; call once per frame.</summary>
        public void Update(float now)
        {
            for (int i = 0; i < _ground.Length; i++)
            {
                Decal? splat = _ground[i];
                if (splat == null || splat.Projector.fadeFactor >= 1f)
                {
                    continue;
                }

                splat.Projector.fadeFactor = Mathf.Clamp01((now - splat.Born) / Mathf.Max(_settings.SplatFadeInSeconds, 0.0001f));
            }

            for (int i = 0; i < _pools.Length; i++)
            {
                Decal? pool = _pools[i];
                if (pool == null)
                {
                    continue;
                }

                float progress = (now - pool.Born) / Mathf.Max(_settings.PoolGrowSeconds, 0.0001f);
                if (progress > 1.05f)
                {
                    continue;
                }

                float diameter = GoreMath.PoolDiameter(pool.Size, pool.Biomass, _settings.PoolStartPerMeter, _settings.PoolMaxPerMeter, _settings.PoolMetersPerBiomass, progress);
                pool.Projector.size = new Vector3(diameter, diameter, _settings.DecalDepth);
            }
        }

        /// <summary>Removes every decal; called when the run ends.</summary>
        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        // A projector above the point, looking down, its box reaching the depth below, so slopes and steps are covered.
        private void Place(DecalProjector projector, Vector3 point, float diameter, Material material)
        {
            float depth = _settings.DecalDepth;
            projector.transform.SetPositionAndRotation(point + Vector3.up * GroundLift, FacingDown * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            projector.material = material;
            projector.size = new Vector3(diameter, diameter, depth);
            projector.pivot = new Vector3(0f, 0f, depth * 0.5f);
            projector.gameObject.SetActive(true);
        }

        private Vector3 FindGround(Vector3 position)
        {
            return GroundProbe.TryFind(position, GroundProbeHeight, GroundProbeDepth, out Vector3 ground) ? ground : position;
        }

        private Decal Take(Decal?[] pool, ref int next)
        {
            Decal? decal = pool[next];
            if (decal == null)
            {
                var decalObject = new GameObject("Blood Decal");
                decalObject.transform.SetParent(_root, false);
                DecalProjector projector = decalObject.AddComponent<DecalProjector>();
                projector.drawDistance = DrawDistance;
                decal = new Decal(projector);
                pool[next] = decal;
            }

            next = (next + 1) % pool.Length;
            return decal;
        }

        private sealed class Decal
        {
            public Decal(DecalProjector projector)
            {
                Projector = projector;
            }

            public DecalProjector Projector { get; }

            public float Born { get; set; }

            public float Size { get; set; }

            public float Biomass { get; set; }
        }
    }
}
