#nullable enable
using System.IO;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Presentation;
using DemonFighter.Presentation.Cameras;
using DemonFighter.Presentation.Demons;
using DemonFighter.Simulation.Worldgen;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Creates the primitive-stage assets from code so they can be regenerated after a change (ASSET_PIPELINE): the
    /// URP materials, the palette that maps roles to them, the world build settings and the ash cavern biome asset.
    /// Existing assets are updated in place, so references and GUIDs survive a re-run.
    /// Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class PlaceholderAssetGenerator
    {
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string MaterialsFolder = "Assets/_Project/Art/Materials/Placeholder";
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string BiomesFolder = "Assets/_Project/Content/Biomes";
        internal const string PalettePath = SettingsFolder + "/PlaceholderPalette.asset";
        internal const string WorldBuildSettingsPath = SettingsFolder + "/WorldBuildSettings.asset";
        internal const string AshCavernPath = BiomesFolder + "/BI_AshCavern.asset";
        internal const string DemonViewSettingsPath = SettingsFolder + "/DemonViewSettings.asset";
        internal const string CameraRigSettingsPath = SettingsFolder + "/CameraRigSettings.asset";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        internal const string DemonPrefabPath = PrefabsFolder + "/P_Demon.prefab";
        private const string DemonBodyProperty = "_body";
        private const string EmissionKeyword = "_EMISSION";
        private const string EmissionColorProperty = "_EmissionColor";
        private const string SmoothnessProperty = "_Smoothness";

        [MenuItem("Demon Fighter/Generate/Placeholder Assets")]
        public static void Generate()
        {
            EnsureFolder(MaterialsFolder);
            EnsureFolder(SettingsFolder);
            EnsureFolder(BiomesFolder);

            // Colors from ASSET_PIPELINE "Placeholder standard": player teal, AI by tier, elders near black.
            Material ground = Lit("M_Ground", new Color(0.16f, 0.12f, 0.11f), 0.15f);
            Material wall = Lit("M_Wall", new Color(0.09f, 0.07f, 0.07f), 0.1f);
            Material rock = Lit("M_Rock", new Color(0.24f, 0.21f, 0.2f), 0.2f);
            Material fissure = Emissive("M_Fissure", new Color(0.3f, 0.1f, 0.02f), new Color(1f, 0.45f, 0.08f) * 3f);
            Material lava = Emissive("M_Lava", new Color(0.4f, 0.08f, 0.02f), new Color(1f, 0.3f, 0.05f) * 4f);
            Material water = Lit("M_Water", new Color(0.05f, 0.09f, 0.14f), 0.9f);
            Material bone = Lit("M_Bone", new Color(0.75f, 0.72f, 0.62f), 0.3f);
            Material player = Lit("M_DemonPlayer", new Color(0.1f, 0.65f, 0.6f), 0.4f);
            Material[] tiers =
            {
                Lit("M_DemonTier0", new Color(0.45f, 0.45f, 0.45f), 0.4f),
                Lit("M_DemonTier1", new Color(0.42f, 0.45f, 0.2f), 0.4f),
                Lit("M_DemonTier2", new Color(0.55f, 0.28f, 0.12f), 0.4f),
                Lit("M_DemonTier3", new Color(0.4f, 0.06f, 0.06f), 0.4f),
            };
            Material elder = Lit("M_DemonElder", new Color(0.06f, 0.05f, 0.05f), 0.5f);

            PlaceholderPalette palette = LoadOrCreate<PlaceholderPalette>(PalettePath);
            palette.SetMaterials(ground, wall, rock, fissure, lava, water, bone, player, tiers, elder);
            EditorUtility.SetDirty(palette);

            WorldBuildSettings settings = LoadOrCreate<WorldBuildSettings>(WorldBuildSettingsPath);
            EditorUtility.SetDirty(settings);

            BiomeDefinition biome = LoadOrCreate<BiomeDefinition>(AshCavernPath);
            biome.ApplyDefaults(BiomeSpec.AshCavern);
            EditorUtility.SetDirty(biome);

            DemonViewSettings viewSettings = LoadOrCreate<DemonViewSettings>(DemonViewSettingsPath);
            EditorUtility.SetDirty(viewSettings);

            CameraRigSettings cameraSettings = LoadOrCreate<CameraRigSettings>(CameraRigSettingsPath);
            EditorUtility.SetDirty(cameraSettings);

            GenerateDemonPrefab();

            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Generated placeholder materials, palette, settings, " + AshCavernPath + " and " + DemonPrefabPath + ".");
        }

        // Root with the controller and the view, one capsule child as the body; materials come at bind time.
        private static void GenerateDemonPrefab()
        {
            EnsureFolder(PrefabsFolder);
            var root = new GameObject("Demon");
            try
            {
                root.AddComponent<CharacterController>();
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                Object.DestroyImmediate(body.GetComponent<Collider>());

                DemonView view = root.AddComponent<DemonView>();
                using (var serialized = new SerializedObject(view))
                {
                    SerializedProperty? bodyProperty = serialized.FindProperty(DemonBodyProperty);
                    if (bodyProperty == null)
                    {
                        throw new IOException("DemonView has no serialized field " + DemonBodyProperty + ".");
                    }

                    bodyProperty.objectReferenceValue = body.transform;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, DemonPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Material Lit(string name, Color color, float smoothness)
        {
            Material material = LoadOrCreateMaterial(name);
            material.color = color;
            material.SetFloat(SmoothnessProperty, smoothness);
            material.DisableKeyword(EmissionKeyword);
            material.SetColor(EmissionColorProperty, Color.black);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Emissive(string name, Color color, Color emission)
        {
            Material material = Lit(name, color, 0.3f);
            material.EnableKeyword(EmissionKeyword);
            material.SetColor(EmissionColorProperty, emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string name)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find(LitShader);
            if (shader == null)
            {
                throw new IOException("Shader not found: " + LitShader + ". Is URP installed?");
            }

            var material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
