#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Presentation.Combat;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Worldgen;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DemonFighter.Presentation.World
{
    /// <summary>
    /// Builds the Unity side of a <see cref="WorldLayout"/> at run start (ARCHITECTURE, "World generation"): chunked
    /// terrain meshes with colliders, the cavern walls, primitive features, the glowing-ceiling light with its soft
    /// shadows and three-color ambient, fog and the lit haze behind it (D-087), and the lava and fissure lights (D-015)
    /// that flicker and share a shadow budget (D-083), with embers rising over the lava (D-086). Holds no game rules; everything it needs is in the layout and the settings.
    /// </summary>
    public sealed class WorldBuilder
    {
        private const float RockEmbedDepth = 0.3f;
        private const float FlatFeatureLift = 0.05f;
        private const float LightHeight = 2.5f;
        private const float WallBaseMargin = 1f;
        private const float CylinderMeshHeight = 2f;
        private const float FissureStripLift = 0.08f;
        private const float FissureSegmentMeters = 1f;

        private readonly PlaceholderPalette _palette;
        private readonly WorldBuildSettings _settings;
        private readonly List<Light> _pointLights = new List<Light>();

        public WorldBuilder(PlaceholderPalette palette, WorldBuildSettings settings)
        {
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Creates every object of the world under a new root and applies the scene lighting.</summary>
        public WorldView Build(WorldLayout layout, Transform? parent)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var root = new GameObject("World");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            _pointLights.Clear();
            BuildTerrain(layout, root.transform);
            BuildWalls(layout, root.transform);
            BuildFeatures(layout, root.transform);
            ApplyLighting(root.transform);
            root.AddComponent<ShadowBudget>().Configure(_pointLights, _settings.ShadowedPointLights);
            return new WorldView(root);
        }

        private void BuildTerrain(WorldLayout layout, Transform root)
        {
            Heightfield field = layout.Heightfield;
            int cellsX = field.VertexCountX - 1;
            int cellsZ = field.VertexCountZ - 1;
            int chunk = Mathf.Max(1, _settings.ChunkCells);
            Transform terrain = Child("Terrain", root);

            for (int z = 0; z < cellsZ; z += chunk)
            {
                for (int x = 0; x < cellsX; x += chunk)
                {
                    Mesh mesh = TerrainMeshBuilder.BuildChunk(field, x, z, Mathf.Min(chunk, cellsX - x), Mathf.Min(chunk, cellsZ - z));
                    var chunkObject = new GameObject("Chunk " + x + " " + z);
                    chunkObject.transform.SetParent(terrain, false);
                    chunkObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    chunkObject.AddComponent<MeshRenderer>().sharedMaterial = _palette.Ground;
                    chunkObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
            }
        }

        private void BuildWalls(WorldLayout layout, Transform root)
        {
            GroundBounds bounds = layout.Bounds;
            float thickness = _settings.WallThickness;
            float height = _settings.WallHeight;
            float baseY = MinHeight(layout.Heightfield) - WallBaseMargin;
            float centerY = baseY + height * 0.5f;
            float centerX = (bounds.MinX + bounds.MaxX) * 0.5f;
            float centerZ = (bounds.MinZ + bounds.MaxZ) * 0.5f;
            float lengthX = bounds.MaxX - bounds.MinX + 2f * thickness;
            float lengthZ = bounds.MaxZ - bounds.MinZ + 2f * thickness;
            Transform walls = Child("Walls", root);

            Primitive(PrimitiveType.Cube, "Wall North", new Vector3(centerX, centerY, bounds.MaxZ + thickness * 0.5f), 0f, new Vector3(lengthX, height, thickness), _palette.Wall, walls, true);
            Primitive(PrimitiveType.Cube, "Wall South", new Vector3(centerX, centerY, bounds.MinZ - thickness * 0.5f), 0f, new Vector3(lengthX, height, thickness), _palette.Wall, walls, true);
            Primitive(PrimitiveType.Cube, "Wall East", new Vector3(bounds.MaxX + thickness * 0.5f, centerY, centerZ), 0f, new Vector3(thickness, height, lengthZ), _palette.Wall, walls, true);
            Primitive(PrimitiveType.Cube, "Wall West", new Vector3(bounds.MinX - thickness * 0.5f, centerY, centerZ), 0f, new Vector3(thickness, height, lengthZ), _palette.Wall, walls, true);
        }

        private void BuildFeatures(WorldLayout layout, Transform root)
        {
            Transform features = Child("Features", root);
            for (int i = 0; i < layout.Features.Count; i++)
            {
                FeaturePlacement placement = layout.Features[i];
                Vector3 position = placement.Position.ToUnity();
                Vector3 size = placement.Size.ToUnity();
                switch (placement.Kind)
                {
                    case FeatureKind.Rock:
                        Primitive(PrimitiveType.Cube, "Rock", position + Vector3.up * (size.y * 0.5f - RockEmbedDepth), placement.Yaw, size, _palette.Rock, features, true);
                        break;
                    case FeatureKind.Fissure:
                        FissureStrip(layout.Heightfield, placement, _palette.Fissure, features);
                        PointLight("Fissure Light", position + Vector3.up * LightHeight, _settings.FissureLightColor, _settings.FissureLightIntensity, _settings.FissureLightRange, _settings.FissureFlickerAmplitude, _settings.FissureFlickerSpeed, features);
                        break;
                    case FeatureKind.LavaPool:
                        GameObject pool = Disc("Lava Pool", position, placement.Yaw, size, _palette.Lava, features);
                        pool.AddComponent<LavaFlow>().Configure(_settings.LavaFlowTilesPerSecond, size.x * _settings.LavaTilesPerMeter);
                        if (_palette.Embers != null)
                        {
                            EmberEffects.CreateLavaEmbers(_palette.Embers, features, position + Vector3.up * FlatFeatureLift, size.x * 0.5f, _settings.LavaEmbersPerSquareMeter);
                        }
                        PointLight("Lava Light", position + Vector3.up * LightHeight, _settings.LavaLightColor, _settings.LavaLightIntensity, size.x * _settings.LavaLightRangePerMeter, _settings.LavaFlickerAmplitude, _settings.LavaFlickerSpeed, features);
                        break;
                    case FeatureKind.WaterPool:
                        Disc("Water Pool", position, placement.Yaw, size, _palette.Water, features);
                        break;
                    case FeatureKind.BonePile:
                        Primitive(PrimitiveType.Cylinder, "Bone Pile", position + Vector3.up * (size.y * 0.5f - RockEmbedDepth), placement.Yaw, new Vector3(size.x, size.y / CylinderMeshHeight, size.z), _palette.Bone, features, true);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown feature kind " + placement.Kind + ".");
                }
            }
        }

        // Pools are flat cylinders whose top sits just above the ground; nothing collides with them.
        // The surface of a pool: the round disc mesh scaled to the pool, 5 cm above its flat bed (D-086).
        private static GameObject Disc(string name, Vector3 groundPosition, float yaw, Vector3 size, Material material, Transform parent)
        {
            var disc = new GameObject(name);
            disc.transform.SetParent(parent, false);
            disc.transform.SetPositionAndRotation(groundPosition + (Vector3.up * FlatFeatureLift), SimulationVectors.YawToRotation(yaw));
            disc.transform.localScale = new Vector3(size.x, 1f, size.z);
            disc.AddComponent<MeshFilter>().sharedMesh = DiscMesh.Shared;
            MeshRenderer renderer = disc.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return disc;
        }

        // The glowing ceiling lights from above with soft shadows; the ambient comes from three sides so the undersides
        // of bodies stay dark (D-083). Fog hides the far walls.
        private void ApplyLighting(Transform root)
        {
            // The intensity scales the ambient in linear light, as it does for a light; RenderSettings takes gamma colors.
            Color ambient = _settings.AmbientColor.linear * _settings.AmbientIntensity;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambient.gamma;
            RenderSettings.ambientEquatorColor = (ambient * _settings.AmbientEquatorFraction).gamma;
            RenderSettings.ambientGroundColor = (ambient * _settings.AmbientGroundFraction).gamma;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = _settings.FogColor;
            RenderSettings.fogStartDistance = _settings.FogStartDistance;
            RenderSettings.fogEndDistance = _settings.FogEndDistance;

            // The haze color is also what lies behind everything, so the open cave above reads as glowing ash, not black (D-087).
            Camera? camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = _settings.FogColor;
            }

            var glow = new GameObject("Ceiling Glow");
            glow.transform.SetParent(root, false);
            glow.transform.rotation = Quaternion.Euler(_settings.CeilingLightPitch, _settings.CeilingLightYaw, 0f);
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = _settings.CeilingLightColor;
            light.intensity = _settings.CeilingLightIntensity;
            light.shadows = _settings.CeilingShadowStrength > 0f ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = _settings.CeilingShadowStrength;
        }

        private static GameObject Primitive(
            PrimitiveType type, string name, Vector3 position, float yaw, Vector3 scale, Material material, Transform parent, bool keepCollider)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.SetPositionAndRotation(position, SimulationVectors.YawToRotation(yaw));
            primitive.transform.localScale = scale;
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Object.Destroy(primitive.GetComponent<Collider>());
            }

            return primitive;
        }

        // Point lights start without shadows; the shadow budget hands shadows to the nearest few.
        private void PointLight(string name, Vector3 position, Color color, float intensity, float range, float flickerAmplitude, float flickerSpeed, Transform parent)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;

            lightObject.AddComponent<LightFlicker>().Configure(flickerAmplitude, flickerSpeed);
            _pointLights.Add(light);
        }

        // A glowing strip that follows the ground, so the whole crack that burns is visible on any slope (D-086); a flat box
        // used to sink into rising ground and float over falling ground.
        private static void FissureStrip(Heightfield field, FeaturePlacement placement, Material material, Transform parent)
        {
            int segments = Mathf.Max(1, Mathf.CeilToInt(placement.Size.Z / FissureSegmentMeters));
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            Quaternion rotation = SimulationVectors.YawToRotation(placement.Yaw);
            Vector3 center = placement.Position.ToUnity();
            float halfWidth = placement.Size.X * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float along = -placement.Size.Z * 0.5f + placement.Size.Z * i / segments;
                for (int side = 0; side < 2; side++)
                {
                    Vector3 point = center + rotation * new Vector3(side == 0 ? -halfWidth : halfWidth, 0f, along);
                    point.y = field.SampleHeight(point.x, point.z) + FissureStripLift;
                    vertices[i * 2 + side] = point;
                    uvs[i * 2 + side] = new Vector2(side, along / placement.Size.X);
                }
            }

            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                triangles[i * 6] = a;
                triangles[i * 6 + 1] = a + 2;
                triangles[i * 6 + 2] = a + 1;
                triangles[i * 6 + 3] = a + 1;
                triangles[i * 6 + 4] = a + 2;
                triangles[i * 6 + 5] = a + 3;
            }

            var mesh = new Mesh { name = "Fissure" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var strip = new GameObject("Fissure");
            strip.transform.SetParent(parent, false);
            strip.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = strip.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static float MinHeight(Heightfield field)
        {
            float min = float.MaxValue;
            for (int i = 0; i < field.Heights.Count; i++)
            {
                min = Mathf.Min(min, field.Heights[i]);
            }

            return min;
        }
    }
}
