#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Art;
using DemonFighter.Editor.Setup;
using DemonFighter.Presentation;
using DemonFighter.Presentation.Cameras;
using DemonFighter.Presentation.Combat;
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
    /// evolutions, the demon kinds, combat tuning, the ash cavern biome), the gore settings, the painted blood textures
    /// with their decal materials, the decal renderer feature, the art folders, and the demon prefab, then
    /// binds whatever art models exist and rebuilds the content catalog. Materials are rewritten every run; every
    /// other asset is created once and keeps its Inspector values, except that assets from an earlier milestone
    /// receive the fields a later one added, once. Public because the -executeMethod command line switch of Unity
    /// has to find it.
    /// </summary>
    public static class PlaceholderAssetGenerator
    {
        internal const string PalettePath = SettingsFolder + "/PlaceholderPalette.asset";
        internal const string WorldBuildSettingsPath = SettingsFolder + "/WorldBuildSettings.asset";
        internal const string DemonViewSettingsPath = SettingsFolder + "/DemonViewSettings.asset";
        internal const string CameraRigSettingsPath = SettingsFolder + "/CameraRigSettings.asset";
        internal const string GoreSettingsPath = SettingsFolder + "/GoreSettings.asset";
        internal const string AshCavernPath = BiomesFolder + "/BI_AshCavern.asset";
        internal const string DemonPrefabPath = PrefabsFolder + "/P_Demon.prefab";
        internal const string MenuPath = "Demon Fighter/Generate/Placeholder Assets";

        /// <summary>
        /// Raise this whenever the generator writes something new or migrates an asset; the editor then reruns the
        /// generator by itself once (GeneratedAssetsGuard), so nobody has to remember the menu after pulling.
        /// </summary>
        internal const int Version = 10;

        // Generator versions that changed generated assets in place; assets written by an older version get the change
        // once: the post exposure and texture tint, then the painted surfaces, brighter and seamless (both D-087).
        private const int BrightLookVersion = 4;
        private const int SurfacePaintVersion = 6;

        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string DemonSkinShader = "DemonFighter/DemonSkin";
        private const string DecalShader = "Shader Graphs/Decal";
        private const string DecalBaseMapProperty = "Base_Map";
        private const string MaterialsFolder = "Assets/_Project/Art/Materials/Placeholder";
        private const string DecalMaterialsFolder = "Assets/_Project/Art/Materials/Decals";
        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        private const string SurfaceProperty = "_Surface";
        private const string BlendProperty = "_Blend";
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
        private const string DemonFigureProperty = "_figureRoot";
        private const string DemonPlaceholdersProperty = "_placeholders";
        private const string DemonRigProperty = "_rig";
        private const string GitKeepFile = ".gitkeep";
        private const string EmissionKeyword = "_EMISSION";
        private const string NormalMapKeyword = "_NORMALMAP";
        private const string BaseMapProperty = "_BaseMap";
        private const string BumpMapProperty = "_BumpMap";
        private const string EmissionMapProperty = "_EmissionMap";
        private const float GroundTiling = 2.5f;
        private const float RockTiling = 2f;
        private const float WallTiling = 6f;
        private const float LavaTiling = 1f;
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
            EnsureArtFolders();

            int stamped = StampedVersion();
            bool brighten = stamped < BrightLookVersion;
            PlaceholderPalette palette = GenerateMaterials(stamped < SurfacePaintVersion);
            WorldBuildSettings worldSettings = EditorAssets.LoadOrCreate<WorldBuildSettings>(WorldBuildSettingsPath);
            if (worldSettings.NeedsLightingDefaults)
            {
                // The world was too dark (D-087); an older asset gets the brighter lighting once.
                worldSettings.ApplyLightingDefaults();
                EditorUtility.SetDirty(worldSettings);
            }

            var viewSettings = EditorAssets.LoadOrCreate<DemonViewSettings>(DemonViewSettingsPath);
            if (brighten)
            {
                // Written again so the asset drops the tint field the lighter texture tint replaced (D-087).
                EditorUtility.SetDirty(viewSettings);
            }

            EditorAssets.LoadOrCreate<CameraRigSettings>(CameraRigSettingsPath);
            EditorAssets.LoadOrCreate<GoreSettings>(GoreSettingsPath);
            RenderPipelineSetup.EnsureDecalFeature();
            RenderLookGenerator.EnsureVolumeProfile(brighten);
            RenderLookGenerator.EnsurePresets();
            GenerateContent();
            int bound = ArtAssetBinder.Bind();
            GenerateDemonPrefab(palette);

            palette.StampGenerator(Version);
            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            ContentCatalogRebuilder.Rebuild();
            Log.Info(LogCategory.Editor, "Generated placeholder materials, palette, settings, content assets and " + DemonPrefabPath + "; bound art for " + bound + " part(s).");
        }

        /// <summary>Why the generated assets are behind the code, or null when they are current.</summary>
        internal static string? OutdatedReason()
        {
            var palette = AssetDatabase.LoadAssetAtPath<PlaceholderPalette>(PalettePath);
            if (palette == null)
            {
                return "the palette is missing";
            }

            return palette.GeneratorVersion < Version ? "written by generator version " + palette.GeneratorVersion + ", the code is at " + Version : null;
        }

        private static int StampedVersion()
        {
            var palette = AssetDatabase.LoadAssetAtPath<PlaceholderPalette>(PalettePath);
            return palette != null ? palette.GeneratorVersion : 0;
        }

        // The art folders exist from the start so there is a place to drop models into; a .gitkeep keeps each empty
        // folder in Git, and Unity ignores files that start with a dot.
        private static void EnsureArtFolders()
        {
            foreach (string folder in ArtFolders.All)
            {
                EditorAssets.EnsureFolder(folder);
                string keep = folder + "/" + GitKeepFile;
                if (!File.Exists(keep))
                {
                    File.WriteAllText(keep, string.Empty);
                }
            }
        }

        private static PlaceholderPalette GenerateMaterials(bool repaintSurfaces)
        {
            // Colors from ASSET_PIPELINE "Placeholder standard": player teal, AI by tier, elders near black.
            // The painted surfaces shade the flat colors; the tiling follows the UV density of each shape (D-083).
            Material ground = Surface(Lit("M_Ground", Color.white, 0.15f), TextureGenerator.EnsureSurface("Ground", TextureGenerator.SurfaceStyle.Ash, 21, repaintSurfaces), GroundTiling);
            Material wall = Surface(Lit("M_Wall", Color.white, 0.1f), TextureGenerator.EnsureSurface("Wall", TextureGenerator.SurfaceStyle.DarkRock, 45, repaintSurfaces), WallTiling);
            Material rock = Surface(Lit("M_Rock", Color.white, 0.2f), TextureGenerator.EnsureSurface("Rock", TextureGenerator.SurfaceStyle.Rock, 33, repaintSurfaces), RockTiling);
            Material fissure = Emissive("M_Fissure", new Color(0.3f, 0.1f, 0.02f), new Color(1f, 0.45f, 0.08f) * 3f);
            Material lava = Surface(Emissive("M_Lava", Color.white, new Color(1f, 0.32f, 0.06f) * 6f), TextureGenerator.EnsureSurface("Lava", TextureGenerator.SurfaceStyle.Lava, 57, repaintSurfaces), LavaTiling);
            Material water = Lit("M_Water", new Color(0.05f, 0.09f, 0.14f), 0.9f);
            Material bone = Lit("M_Bone", new Color(0.75f, 0.72f, 0.62f), 0.3f);
            // Everything a demon wears uses the skin shader, so blood can soak it (D-081).
            Material player = Skin("M_DemonPlayer", new Color(0.1f, 0.65f, 0.6f), 0.4f);
            Material[] tiers =
            {
                Skin("M_DemonTier0", new Color(0.45f, 0.45f, 0.45f), 0.4f),
                Skin("M_DemonTier1", new Color(0.42f, 0.45f, 0.2f), 0.4f),
                Skin("M_DemonTier2", new Color(0.55f, 0.28f, 0.12f), 0.4f),
                Skin("M_DemonTier3", new Color(0.4f, 0.06f, 0.06f), 0.4f),
            };
            Material elder = Skin("M_DemonElder", new Color(0.06f, 0.05f, 0.05f), 0.5f);
            Material corpse = Skin("M_Corpse", new Color(0.13f, 0.09f, 0.08f), 0.2f);
            Material blood = Lit("M_Blood", new Color(0.28f, 0.01f, 0.01f), 0.65f);
            Material maw = Skin("M_DemonMaw", new Color(0.35f, 0.03f, 0.03f), 0.3f);
            Material eye = Skin("M_Eye", new Color(0.9f, 0.88f, 0.8f), 0.7f);
            Material plate = Skin("M_Plate", new Color(0.2f, 0.2f, 0.22f), 0.55f);

            PlaceholderPalette palette = EditorAssets.LoadOrCreate<PlaceholderPalette>(PalettePath);
            palette.SetMaterials(ground, wall, rock, fissure, lava, water, bone, player, tiers, elder, corpse, blood, maw, eye, plate);
            GenerateGoreMaterials(palette);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        // Each asset is initialized from its spec once; afterwards the asset is the truth. Parts are created in the
        // order of PlaceholderContent so a part that requires another finds it, and older assets get the fields of
        // later milestones once.
        private static void GenerateContent()
        {
            AudioGenerator.AudioLibrary audio = AudioGenerator.Ensure();
            var skills = new Dictionary<string, SkillDefinition>();
            foreach (SkillSpec spec in PlaceholderContent.Skills)
            {
                SkillDefinition skill = EditorAssets.LoadOrCreate<SkillDefinition>(SkillsFolder + "/SK_" + FileName(spec.Name) + ".asset", s => s.Configure(spec));
                if (skill.NeedsM3Defaults)
                {
                    skill.ApplyM3Defaults(spec);
                    EditorUtility.SetDirty(skill);
                }

                if (skill.NeedsM5Defaults)
                {
                    skill.ApplyM5Defaults(PlaceholderContent.SkillMotionFor(spec.Id), audio.EventForSkill(spec.Id));
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

                if (part.NeedsM5Defaults)
                {
                    // Assets written before M5 get the socket anchors of the capsule and the default mesh fit once.
                    part.ApplyM5Defaults(DefaultAnchorsFor(spec), PlaceholderContent.MeshOffsetFor(spec.Id), PlaceholderContent.MotionFor(spec.Id), audio.SeverSoundFor(spec.Fate));
                    EditorUtility.SetDirty(part);
                }

                if (part.NeedsSizeDefaults)
                {
                    // Tiers come from evolutions now; bulky parts still make the body bigger (D-091).
                    part.ApplySizeDefaults(spec.SizeBonus);
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

            var combatTuning = EditorAssets.LoadOrCreate<CombatTuningDefinition>(CombatTuningPath, tuning => tuning.Configure(new CombatTuning()));
            if (combatTuning.NeedsProgressionDefaults)
            {
                // Kill XP (D-090) and tiers through evolution (D-091) changed the progression; an older asset gets it once.
                combatTuning.ApplyProgressionDefaults();
                EditorUtility.SetDirty(combatTuning);
            }

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
                biome.SetAudio(audio.Clip(AudioGenerator.DroneClip), audio.Clip(AudioGenerator.LavaClip), audio.Clip(AudioGenerator.CalmTrackClip), audio.Clip(AudioGenerator.CombatTrackClip));
            });

            // An older biome asset predates the demon references and the spawn table (D-070); it gets both once. The
            // numbers it carries equal the spec defaults, so applying them again changes nothing a player would notice.
            var existingBiome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(AshCavernPath);
            if (existingBiome != null)
            {
                existingBiome.SetDemons(blob, elder);
                if (!existingBiome.HasAudio)
                {
                    // The placeholder loops and tracks (D-084); a biome that already has sound keeps it.
                    existingBiome.SetAudio(audio.Clip(AudioGenerator.DroneClip), audio.Clip(AudioGenerator.LavaClip), audio.Clip(AudioGenerator.CalmTrackClip), audio.Clip(AudioGenerator.CombatTrackClip));
                }

                if (!existingBiome.HasSpawnTable)
                {
                    existingBiome.SetSpawnTable(spawnTable);
                }

                // The pacing numbers changed after the first playtest (D-078); older assets get the spec numbers once.
                if (existingBiome.NeedsPacingDefaults)
                {
                    existingBiome.ApplyDefaults(ashCavern);
                }

                // Lava and fissures burn since D-086; older assets get the burn numbers once.
                if (existingBiome.NeedsHazardDefaults)
                {
                    existingBiome.ApplyHazardDefaults(ashCavern);
                }

                if (!existingBiome.HasBlobArchetypes)
                {
                    existingBiome.SetBlobArchetypes(blobArchetypes);
                }

                EditorUtility.SetDirty(existingBiome);
            }
        }

        // Only the core exposes sockets, so only the core gets anchors; they describe the placeholder capsule.
        private static SocketAnchorDefinition[] DefaultAnchorsFor(BodyPartSpec spec)
        {
            if (!spec.IsCore)
            {
                return Array.Empty<SocketAnchorDefinition>();
            }

            var anchors = new SocketAnchorDefinition[PlaceholderContent.DefaultAnchors.Count];
            for (int i = 0; i < anchors.Length; i++)
            {
                PlaceholderContent.SocketAnchor anchor = PlaceholderContent.DefaultAnchors[i];
                anchors[i] = new SocketAnchorDefinition();
                anchors[i].Configure(anchor.Kind, anchor.Position, anchor.Euler);
            }

            return anchors;
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

        // Blood decals wear the URP decal shader over the painted placeholder textures; viscera wear wet flesh.
        private static void GenerateGoreMaterials(PlaceholderPalette palette)
        {
            EditorAssets.EnsureFolder(DecalMaterialsFolder);
            Texture2D[] splatTextures = TextureGenerator.EnsureBloodSplats();
            var splats = new Material[splatTextures.Length];
            for (int i = 0; i < splats.Length; i++)
            {
                splats[i] = Decal("M_Blood_Splat_" + (i + 1).ToString("00"), splatTextures[i]);
            }

            Material pool = Decal("M_Blood_Pool", TextureGenerator.EnsureBloodPool());
            Material viscera = Skin("M_Viscera", new Color(0.32f, 0.03f, 0.03f), 0.8f);
            palette.SetGore(splats, pool, viscera, Embers());
        }

        // Additive glowing particles for sparks and lava embers (D-086); URP's own material setup sets blending and keywords.
        private static Material Embers()
        {
            Material material = LoadOrCreateMaterial("M_Embers", ParticleShader, MaterialsFolder);
            material.SetTexture(BaseMapProperty, TextureGenerator.EnsureEmber());
            material.color = Color.white;
            material.SetFloat(SurfaceProperty, (float)BaseShaderGUI.SurfaceType.Transparent);
            material.SetFloat(BlendProperty, (float)BaseShaderGUI.BlendMode.Additive);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Decal(string name, Texture2D texture)
        {
            Material material = LoadOrCreateMaterial(name, DecalShader, DecalMaterialsFolder);
            material.SetTexture(DecalBaseMapProperty, texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Skin(string name, Color color, float smoothness)
        {
            Material material = LoadOrCreateMaterial(name, DemonSkinShader, MaterialsFolder);
            material.color = color;
            material.SetFloat(SmoothnessProperty, smoothness);
            material.DisableKeyword(EmissionKeyword);
            material.SetColor(EmissionColorProperty, Color.black);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Base color and normal map on a material; the base color stays white so the texture carries the palette.
        private static Material Surface(Material material, TextureGenerator.SurfaceTextures textures, float tiling)
        {
            material.SetTexture(BaseMapProperty, textures.BaseColor);
            material.SetTexture(BumpMapProperty, textures.Normal);
            material.EnableKeyword(NormalMapKeyword);
            material.mainTextureScale = new Vector2(tiling, tiling);
            if (textures.Emission != null)
            {
                material.SetTexture(EmissionMapProperty, textures.Emission);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Lit(string name, Color color, float smoothness)
        {
            Material material = LoadOrCreateMaterial(name, LitShader, MaterialsFolder);
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

        // An existing material keeps its values but follows a shader change, as when the demon materials moved to the
        // skin shader; the property names match, so nothing is lost.
        private static Material LoadOrCreateMaterial(string name, string shaderName, string folder)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new IOException("Shader not found: " + shaderName + ". Is URP installed and is the shader in the project?");
            }

            string path = folder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.shader != shader)
                {
                    existing.shader = shader;
                    EditorUtility.SetDirty(existing);
                }

                return existing;
            }

            var material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Root with the controller and the view; under the figure root the capsule body (the core part view with a
        // trigger collider on the Demon layer for hit detection), the placeholder space and the rig the figure
        // composes parts into (DemonFigure); materials come at bind time.
        private static void GenerateDemonPrefab(PlaceholderPalette palette)
        {
            EditorAssets.EnsureFolder(PrefabsFolder);
            var root = new GameObject("Demon");
            try
            {
                root.AddComponent<CharacterController>();
                var figure = new GameObject(DemonFigure.FigureName);
                figure.transform.SetParent(root.transform, false);
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(figure.transform, false);
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

                var placeholders = new GameObject(DemonFigure.PlaceholdersName);
                placeholders.transform.SetParent(figure.transform, false);
                var rig = new GameObject(DemonFigure.RigName);
                rig.transform.SetParent(figure.transform, false);

                DemonView view = root.AddComponent<DemonView>();
                EditorAssets.SetReference(view, DemonBodyProperty, body.transform);
                EditorAssets.SetReference(view, DemonFigureProperty, figure.transform);
                EditorAssets.SetReference(view, DemonPlaceholdersProperty, placeholders.transform);
                EditorAssets.SetReference(view, DemonRigProperty, rig.transform);
                PrefabUtility.SaveAsPrefabAsset(root, DemonPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
