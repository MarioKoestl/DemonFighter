#nullable enable
using System.IO;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Setup;
using DemonFighter.Presentation;
using DemonFighter.Presentation.Cameras;
using DemonFighter.Presentation.Demons;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Worldgen;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Creates the primitive-stage assets from code so they can be regenerated after a change (ASSET_PIPELINE): the
    /// URP materials, the palette that maps roles to them, the settings assets, the first content assets (core,
    /// Bite, two demon kinds, combat tuning, the ash cavern biome) and the demon prefab, then rebuilds the content
    /// catalog. Materials are rewritten every run; every other asset is created once and keeps its Inspector values.
    /// Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class PlaceholderAssetGenerator
    {
        internal const string PalettePath = SettingsFolder + "/PlaceholderPalette.asset";
        internal const string WorldBuildSettingsPath = SettingsFolder + "/WorldBuildSettings.asset";
        internal const string DemonViewSettingsPath = SettingsFolder + "/DemonViewSettings.asset";
        internal const string CameraRigSettingsPath = SettingsFolder + "/CameraRigSettings.asset";
        internal const string AshCavernPath = BiomesFolder + "/BI_AshCavern.asset";
        internal const string DemonPrefabPath = PrefabsFolder + "/P_Demon.prefab";
        internal const string MenuPath = "Demon Fighter/Generate/Placeholder Assets";

        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string MaterialsFolder = "Assets/_Project/Art/Materials/Placeholder";
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string ContentFolder = "Assets/_Project/Content";
        private const string BiomesFolder = ContentFolder + "/Biomes";
        private const string BodyPartsFolder = ContentFolder + "/BodyParts";
        private const string SkillsFolder = ContentFolder + "/Skills";
        private const string DemonsFolder = ContentFolder + "/Demons";
        private const string CorePartPath = BodyPartsFolder + "/BP_Core.asset";
        private const string BiteSkillPath = SkillsFolder + "/SK_Bite.asset";
        private const string BlobDemonPath = DemonsFolder + "/DM_Blob.asset";
        private const string ElderDemonPath = DemonsFolder + "/DM_Elder.asset";
        private const string CombatTuningPath = ContentCatalogRebuilder.CatalogFolder + "/CombatTuning.asset";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string DemonBodyProperty = "_body";
        private const string EmissionKeyword = "_EMISSION";
        private const string EmissionColorProperty = "_EmissionColor";
        private const string SmoothnessProperty = "_Smoothness";

        // The snout sits on the front of the capsule mesh (2 units tall, radius 0.5), in mesh units.
        private static readonly Vector3 SnoutLocalPosition = new Vector3(0f, 0.3f, 0.45f);
        private static readonly Vector3 SnoutLocalScale = new Vector3(0.45f, 0.3f, 0.35f);

        // The first content: the blob is born as this core, which grants Bite (GAME_DESIGN, "The run").
        private static readonly SkillSpec BiteSpec = new SkillSpec { Id = "skill.bite", Name = "Bite" };

        private static readonly BodyPartSpec CoreSpec = new BodyPartSpec
        {
            Id = "part.core",
            Name = "Core",
            Socket = SocketKind.Core,
            MaxHp = 100f,
            Fate = PartFate.Destroyed,
            GrantedSkillIds = new[] { BiteSpec.Id },
            BiomassValue = 20f,
        };

        [MenuItem(MenuPath)]
        public static void Generate()
        {
            ProjectLayers.EnsureLayers();
            EditorAssets.EnsureFolder(MaterialsFolder);
            EditorAssets.EnsureFolder(SettingsFolder);
            EditorAssets.EnsureFolder(BiomesFolder);
            EditorAssets.EnsureFolder(BodyPartsFolder);
            EditorAssets.EnsureFolder(SkillsFolder);
            EditorAssets.EnsureFolder(DemonsFolder);
            EditorAssets.EnsureFolder(ContentCatalogRebuilder.CatalogFolder);

            PlaceholderPalette palette = GenerateMaterials();
            EditorAssets.LoadOrCreate<WorldBuildSettings>(WorldBuildSettingsPath);
            EditorAssets.LoadOrCreate<DemonViewSettings>(DemonViewSettingsPath);
            EditorAssets.LoadOrCreate<CameraRigSettings>(CameraRigSettingsPath);
            GenerateContent();
            GenerateDemonPrefab(palette);

            AssetDatabase.SaveAssets();
            ContentCatalogRebuilder.Rebuild();
            Log.Info(LogCategory.Editor, "Generated placeholder materials, palette, settings, content assets and " + DemonPrefabPath + ".");
        }

        private static PlaceholderPalette GenerateMaterials()
        {
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
            Material corpse = Lit("M_Corpse", new Color(0.13f, 0.09f, 0.08f), 0.2f);
            Material blood = Lit("M_Blood", new Color(0.28f, 0.01f, 0.01f), 0.65f);
            Material maw = Lit("M_DemonMaw", new Color(0.35f, 0.03f, 0.03f), 0.3f);

            PlaceholderPalette palette = EditorAssets.LoadOrCreate<PlaceholderPalette>(PalettePath);
            palette.SetMaterials(ground, wall, rock, fissure, lava, water, bone, player, tiers, elder, corpse, blood, maw);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        // Each asset is initialized from the spec defaults once; afterwards the asset is the truth.
        private static void GenerateContent()
        {
            SkillDefinition bite = EditorAssets.LoadOrCreate<SkillDefinition>(BiteSkillPath, skill => skill.Configure(BiteSpec));
            BodyPartDefinition core = EditorAssets.LoadOrCreate<BodyPartDefinition>(CorePartPath, part => part.Configure(CoreSpec, new[] { bite }));
            EditorAssets.LoadOrCreate<CombatTuningDefinition>(CombatTuningPath, tuning => tuning.Configure(new CombatTuning()));
            DemonDefinition blob = EditorAssets.LoadOrCreate<DemonDefinition>(BlobDemonPath, demon => demon.Configure(BiomeSpec.AshCavern.BlobDemon, core));
            DemonDefinition elder = EditorAssets.LoadOrCreate<DemonDefinition>(ElderDemonPath, demon => demon.Configure(BiomeSpec.AshCavern.ElderDemon, core));
            EditorAssets.LoadOrCreate<BiomeDefinition>(AshCavernPath, biome =>
            {
                biome.ApplyDefaults(BiomeSpec.AshCavern);
                biome.SetDemons(blob, elder);
            });

            // An older biome asset predates the demon references; give it the two demons without touching its numbers.
            var existingBiome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(AshCavernPath);
            if (existingBiome != null)
            {
                existingBiome.SetDemons(blob, elder);
                EditorUtility.SetDirty(existingBiome);
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

        // Root with the controller and the view, one capsule child as the body and core part with a trigger collider
        // on the Demon layer for hit detection; materials come at bind time.
        private static void GenerateDemonPrefab(PlaceholderPalette palette)
        {
            EditorAssets.EnsureFolder(PrefabsFolder);
            var root = new GameObject("Demon");
            try
            {
                root.AddComponent<CharacterController>();
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.GetComponent<Collider>().isTrigger = true;
                body.layer = Layers.Demon;
                body.AddComponent<BodyPartView>();

                // A snout marks the front, so a blob reads as facing somewhere even before real models arrive.
                GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                snout.name = "Snout";
                snout.transform.SetParent(body.transform, false);
                snout.transform.localPosition = SnoutLocalPosition;
                snout.transform.localScale = SnoutLocalScale;
                Object.DestroyImmediate(snout.GetComponent<Collider>());
                snout.GetComponent<Renderer>().sharedMaterial = palette.Maw;

                DemonView view = root.AddComponent<DemonView>();
                EditorAssets.SetReference(view, DemonBodyProperty, body.transform);
                PrefabUtility.SaveAsPrefabAsset(root, DemonPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
