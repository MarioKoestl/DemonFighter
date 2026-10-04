#nullable enable
using System.Collections.Generic;
using System.IO;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Setup;
using DemonFighter.Presentation;
using DemonFighter.Presentation.Cameras;
using DemonFighter.Presentation.Demons;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Worldgen;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Creates the primitive-stage assets from code so they can be regenerated after a change (ASSET_PIPELINE): the
    /// URP materials, the palette that maps roles to them, the settings assets, the content assets (parts, skills,
    /// evolutions, two demon kinds, combat tuning, the ash cavern biome) and the demon prefab, then rebuilds the
    /// content catalog. Materials are rewritten every run; every other asset is created once and keeps its Inspector
    /// values, except that assets from an earlier milestone receive the fields a later one added, once.
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
        private const string EvolutionsFolder = ContentFolder + "/Evolutions";
        private const string ArchetypesFolder = ContentFolder + "/Archetypes";
        private const string CombatTuningPath = ContentCatalogRebuilder.CatalogFolder + "/CombatTuning.asset";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string DemonBodyProperty = "_body";
        private const string EmissionKeyword = "_EMISSION";
        private const string EmissionColorProperty = "_EmissionColor";
        private const string SmoothnessProperty = "_Smoothness";

        // The snout sits on the front of the capsule mesh (2 units tall, radius 0.5), in mesh units.
        private static readonly Vector3 SnoutLocalPosition = new Vector3(0f, 0.3f, 0.45f);
        private static readonly Vector3 SnoutLocalScale = new Vector3(0.45f, 0.3f, 0.35f);

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
            EditorAssets.EnsureFolder(EvolutionsFolder);
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
            Material eye = Lit("M_Eye", new Color(0.9f, 0.88f, 0.8f), 0.7f);
            Material plate = Lit("M_Plate", new Color(0.2f, 0.2f, 0.22f), 0.55f);

            PlaceholderPalette palette = EditorAssets.LoadOrCreate<PlaceholderPalette>(PalettePath);
            palette.SetMaterials(ground, wall, rock, fissure, lava, water, bone, player, tiers, elder, corpse, blood, maw, eye, plate);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        // Each asset is initialized from its spec once; afterwards the asset is the truth. Parts are created in the
        // order of PlaceholderContent so a part that requires another finds it, and M2 assets get the M3 fields once.
        private static void GenerateContent()
        {
            var skills = new Dictionary<string, SkillDefinition>();
            foreach (SkillSpec spec in PlaceholderContent.Skills)
            {
                SkillDefinition skill = EditorAssets.LoadOrCreate<SkillDefinition>(SkillsFolder + "/SK_" + FileName(spec.Name) + ".asset", s => s.Configure(spec));
                if (skill.NeedsM3Defaults)
                {
                    skill.ApplyM3Defaults(spec);
                    EditorUtility.SetDirty(skill);
                }

                skills[spec.Id] = skill;
            }

            var parts = new Dictionary<string, BodyPartDefinition>();
            foreach (BodyPartSpec spec in PlaceholderContent.Parts)
            {
                SkillDefinition[] granted = Resolve(skills, spec.GrantedSkillIds);
                var bonusSkills = new SkillDefinition[spec.SkillDamageBonusesPerLevel.Count];
                for (int i = 0; i < bonusSkills.Length; i++)
                {
                    bonusSkills[i] = Resolve(skills, new[] { spec.SkillDamageBonusesPerLevel[i].SkillId })[0];
                }

                BodyPartDefinition[] required = Resolve(parts, spec.RequiredPartIds);
                PlaceholderContent.PartVisual visual = PlaceholderContent.Visuals[spec.Id];
                BodyPartDefinition part = EditorAssets.LoadOrCreate<BodyPartDefinition>(BodyPartsFolder + "/BP_" + FileName(spec.Name) + ".asset", p =>
                {
                    p.Configure(spec, granted, bonusSkills, required);
                    p.ConfigureVisual(visual.Kind, visual.Material, visual.Position, visual.Scale, visual.Euler, visual.MirrorSecondCopy);
                });
                if (part.NeedsM3Defaults)
                {
                    part.ApplyM3Defaults(spec, bonusSkills, required);
                    part.ConfigureVisual(visual.Kind, visual.Material, visual.Position, visual.Scale, visual.Euler, visual.MirrorSecondCopy);
                    EditorUtility.SetDirty(part);
                }

                parts[spec.Id] = part;
            }

            var evolutions = new Dictionary<string, EvolutionDefinition>();
            foreach (EvolutionSpec spec in PlaceholderContent.Evolutions)
            {
                EvolutionDefinition evolution = EditorAssets.LoadOrCreate<EvolutionDefinition>(EvolutionsFolder + "/EV_" + FileName(spec.Name) + spec.Stage + ".asset", e =>
                    e.Configure(spec, Resolve(parts, spec.FreeMutationPartIds), Resolve(parts, spec.UnlockedPartIds), Resolve(skills, spec.ExtraSkillIds)));
                if (evolution.NeedsPackageDefaults)
                {
                    // Evolutions written before D-067 carried a free pool only; they get the bound package once.
                    evolution.Configure(spec, Resolve(parts, spec.FreeMutationPartIds), Resolve(parts, spec.UnlockedPartIds), Resolve(skills, spec.ExtraSkillIds));
                    EditorUtility.SetDirty(evolution);
                }

                evolutions[spec.Id] = evolution;
            }

            EditorAssets.LoadOrCreate<CombatTuningDefinition>(CombatTuningPath, tuning => tuning.Configure(new CombatTuning()));
            BodyPartDefinition core = parts[PlaceholderContent.CoreId];
            BiomeSpec ashCavern = BiomeSpec.AshCavern;
            DemonDefinition blob = Demon(ashCavern.BlobDemon, core, parts, evolutions);
            DemonDefinition elder = Demon(ashCavern.ElderDemon, core, parts, evolutions);
            IReadOnlyList<SpawnEntry> table = BiomeSpec.DefaultSpawnTable(ashCavern);
            var spawnTable = new SpawnEntryDefinition[table.Count];
            for (int i = 0; i < spawnTable.Length; i++)
            {
                spawnTable[i] = new SpawnEntryDefinition();
                spawnTable[i].Configure(Demon(table[i].Demon, core, parts, evolutions), table[i]);
            }

            EditorAssets.EnsureFolder(ArchetypesFolder);
            IReadOnlyList<ArchetypeChoice> personalities = BiomeSpec.DefaultBlobArchetypes(ashCavern);
            var blobArchetypes = new ArchetypeChoiceDefinition[personalities.Count];
            for (int i = 0; i < blobArchetypes.Length; i++)
            {
                ArchetypeSpec archetype = personalities[i].Archetype;
                ArchetypeAsset asset = EditorAssets.LoadOrCreate<ArchetypeAsset>(ArchetypesFolder + "/AR_" + FileName(archetype.Name) + ".asset", a => a.Configure(archetype, Resolve(parts, archetype.PreferredPartIds)));
                blobArchetypes[i] = new ArchetypeChoiceDefinition();
                blobArchetypes[i].Configure(asset, personalities[i]);
            }

            EditorAssets.LoadOrCreate<BiomeDefinition>(AshCavernPath, biome =>
            {
                biome.ApplyDefaults(ashCavern);
                biome.SetDemons(blob, elder);
                biome.SetSpawnTable(spawnTable);
                biome.SetBlobArchetypes(blobArchetypes);
            });

            // An older biome asset predates the demon references and the spawn table (D-070); it gets both once. The
            // numbers it carries equal the spec defaults, so applying them again changes nothing a player would notice.
            var existingBiome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(AshCavernPath);
            if (existingBiome != null)
            {
                existingBiome.SetDemons(blob, elder);
                if (!existingBiome.HasSpawnTable)
                {
                    existingBiome.SetSpawnTable(spawnTable);
                }

                // The pacing numbers changed after the first playtest (D-078); older assets get the spec numbers once.
                if (existingBiome.NeedsPacingDefaults)
                {
                    existingBiome.ApplyDefaults(ashCavern);
                }

                if (!existingBiome.HasBlobArchetypes)
                {
                    existingBiome.SetBlobArchetypes(blobArchetypes);
                }

                EditorUtility.SetDirty(existingBiome);
            }
        }

        // One asset per demon kind, named after the kind; the starting package resolves to part and evolution assets.
        private static DemonDefinition Demon(DemonSpec spec, BodyPartDefinition core, Dictionary<string, BodyPartDefinition> parts, Dictionary<string, EvolutionDefinition> evolutions)
        {
            BodyPartDefinition[] startingParts = Resolve(parts, spec.StartingPartIds);
            EvolutionDefinition? startingEvolution = null;
            if (spec.StartingEvolutionId.Length > 0 && !evolutions.TryGetValue(spec.StartingEvolutionId, out startingEvolution))
            {
                throw new IOException("Demon " + spec.Id + " starts with unknown evolution " + spec.StartingEvolutionId + ".");
            }

            return EditorAssets.LoadOrCreate<DemonDefinition>(DemonsFolder + "/DM_" + FileName(spec.Name) + ".asset", demon => demon.Configure(spec, core, startingParts, startingEvolution));
        }

        private static T[] Resolve<T>(Dictionary<string, T> byId, IReadOnlyList<string> ids)
            where T : Object
        {
            var result = new T[ids.Count];
            for (int i = 0; i < result.Length; i++)
            {
                if (!byId.TryGetValue(ids[i], out T definition))
                {
                    throw new IOException("Content refers to " + ids[i] + " before it exists; order PlaceholderContent so dependencies come first.");
                }

                result[i] = definition;
            }

            return result;
        }

        private static string FileName(string displayName)
        {
            return displayName.Replace(" ", string.Empty);
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
