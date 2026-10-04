#nullable enable
using System;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Worldgen;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DemonFighter.Presentation.World
{
    /// <summary>
    /// Builds the Unity side of a <see cref="WorldLayout"/> at run start (ARCHITECTURE, "World generation"): chunked
    /// terrain meshes with colliders, the cavern walls, primitive features, the glowing-ceiling light, fog, and the
    /// lava and fissure lights (D-015). Holds no game rules; everything it needs is in the layout and the settings.
    /// </summary>
    public sealed class WorldBuilder
    {
        private const float RockEmbedDepth = 0.3f;
        private const float FlatFeatureLift = 0.05f;
        private const float LightHeight = 2.5f;
        private const float WallBaseMargin = 1f;
        private const float CylinderMeshHeight = 2f;

        private readonly PlaceholderPalette _palette;
        private readonly WorldBuildSettings _settings;

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

            BuildTerrain(layout, root.transform);
            BuildWalls(layout, root.transform);
            BuildFeatures(layout, root.transform);
            ApplyLighting(root.transform);
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
                        Primitive(PrimitiveType.Cube, "Fissure", position + Vector3.up * (FlatFeatureLift - size.y * 0.5f), placement.Yaw, size, _palette.Fissure, features, false);
                        PointLight("Fissure Light", position + Vector3.up * LightHeight, _settings.FissureLightColor, _settings.FissureLightIntensity, _settings.FissureLightRange, features);
                        break;
                    case FeatureKind.LavaPool:
                        Disc("Lava Pool", position, placement.Yaw, size, _palette.Lava, features);
                        PointLight("Lava Light", position + Vector3.up * LightHeight, _settings.LavaLightColor, _settings.LavaLightIntensity, size.x * _settings.LavaLightRangePerMeter, features);
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
        private void Disc(string name, Vector3 groundPosition, float yaw, Vector3 size, Material material, Transform parent)
        {
            var scale = new Vector3(size.x, size.y / CylinderMeshHeight, size.z);
            Vector3 center = groundPosition + Vector3.up * (FlatFeatureLift - size.y * 0.5f);
            Primitive(PrimitiveType.Cylinder, name, center, yaw, scale, material, parent, false);
        }

        private void ApplyLighting(Transform root)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _settings.AmbientColor * _settings.AmbientIntensity;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = _settings.FogColor;
            RenderSettings.fogStartDistance = _settings.FogStartDistance;
            RenderSettings.fogEndDistance = _settings.FogEndDistance;

            var glow = new GameObject("Ceiling Glow");
            glow.transform.SetParent(root, false);
            glow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = _settings.CeilingLightColor;
            light.intensity = _settings.CeilingLightIntensity;
            light.shadows = LightShadows.None;
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

        private static void PointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
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
