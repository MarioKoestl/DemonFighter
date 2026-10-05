#nullable enable
using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DemonFighter.Presentation.Combat
{
    /// <summary>
    /// Fire you can see (D-086): sparks that fly up from a burning body, and the slow embers that rise over every lava
    /// pool. Built from code with the additive ember material of the palette; one particle system for all burning
    /// bodies, one per pool. Look only.
    /// </summary>
    public static class EmberEffects
    {
        private const int MaxSparks = 1500;
        private static readonly Color HotColor = new Color(1f, 0.75f, 0.3f, 1f);
        private static readonly Color DeepColor = new Color(1f, 0.32f, 0.05f, 1f);

        /// <summary>A world-space particle system for sparks thrown by <see cref="Spark"/>; it emits nothing by itself.</summary>
        public static ParticleSystem CreateSparks(Material material, Transform parent)
        {
            ParticleSystem system = Create("Burning Sparks", material, parent, MaxSparks);
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            system.Play();
            return system;
        }

        /// <summary>Throws sparks from a burning body: they rise, drift and fade within a second or two.</summary>
        public static void Spark(ParticleSystem system, Vector3 center, float sizeMeters, int count)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            float spread = sizeMeters * 0.35f;
            for (int i = 0; i < count; i++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = center + new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread) * 0.5f, Random.Range(-spread, spread)),
                    velocity = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(1.2f, 3f), Random.Range(-0.4f, 0.4f)) * Mathf.Sqrt(Mathf.Max(0.5f, sizeMeters)),
                    startLifetime = Random.Range(0.6f, 1.4f),
                    startSize = Random.Range(0.05f, 0.14f) * Mathf.Sqrt(Mathf.Max(0.5f, sizeMeters)),
                    startColor = Color.Lerp(HotColor, DeepColor, Random.value),
                    applyShapeToPosition = false,
                };
                system.Emit(emit, 1);
            }
        }

        /// <summary>Embers that rise slowly over a lava pool for as long as the pool exists.</summary>
        public static ParticleSystem CreateLavaEmbers(Material material, Transform parent, Vector3 center, float radius, float perSquareMeter)
        {
            ParticleSystem system = Create("Lava Embers", material, parent, 600);
            system.transform.position = center;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 1f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            ParticleSystem.MainModule main = system.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.2f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.PI * radius * radius * perSquareMeter;
            system.Play();
            return system;
        }

        private static ParticleSystem Create(string name, Material material, Transform parent, int maxParticles)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            ParticleSystem system = gameObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startColor = new ParticleSystem.MinMaxGradient(HotColor, DeepColor);
            main.gravityModifier = -0.05f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colors = system.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.35f, 0.1f), 0.6f), new GradientColorKey(new Color(0.4f, 0.05f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colors.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystem.SizeOverLifetimeModule sizes = system.sizeOverLifetime;
            sizes.enabled = true;
            sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        /// <summary>Removes a particle system created here.</summary>
        public static void Destroy(ParticleSystem? system)
        {
            if (system != null)
            {
                Object.Destroy(system.gameObject);
            }
        }
    }
}
