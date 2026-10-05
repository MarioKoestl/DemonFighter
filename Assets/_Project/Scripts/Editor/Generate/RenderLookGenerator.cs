#nullable enable
using System.IO;
using DemonFighter.Common;
using DemonFighter.Editor.Setup;
using DemonFighter.Presentation.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Builds the look of the renderer from code (D-083): the post-processing profile of the cavern (ACES tonemapping,
    /// a warm dark grade, bloom for lava and embers, vignette, film grain, a touch of chromatic aberration) and the
    /// three graphics presets, Low and Medium as pipeline and renderer assets derived from the PC assets that stay
    /// High, listed in a catalog the settings menu reads. Existing assets are kept, so tuning in the Inspector
    /// survives a rerun. Public because the generator and the command line call it.
    /// </summary>
    public static class RenderLookGenerator
    {
        internal const string MenuPath = "Demon Fighter/Generate/Render Look";
        internal const string GraphicsFolder = "Assets/_Project/Settings/Graphics";
        internal const string VolumeProfilePath = GraphicsFolder + "/VP_Cavern.asset";
        internal const string PresetCatalogPath = GraphicsFolder + "/GraphicsPresets.asset";
        internal const string LowPipelinePath = GraphicsFolder + "/URP_Low.asset";
        internal const string LowRendererPath = GraphicsFolder + "/URP_Low_Renderer.asset";
        internal const string MediumPipelinePath = GraphicsFolder + "/URP_Medium.asset";
        internal const string MediumRendererPath = GraphicsFolder + "/URP_Medium_Renderer.asset";
        internal const string HighPipelinePath = "Assets/Settings/PC_RPAsset.asset";
        internal const string LowName = "Low";
        internal const string MediumName = "Medium";
        internal const string HighName = "High";

        private const string FeaturesProperty = "m_RendererFeatures";
        private const string FeatureMapProperty = "m_RendererFeatureMap";
        private const string RendererListProperty = "m_RendererDataList";
        private const string SoftShadowsProperty = "m_SoftShadowsSupported";
        private const string AdditionalShadowsProperty = "m_AdditionalLightShadowsSupported";
        private const string ShadowAtlasProperty = "m_AdditionalLightsShadowmapResolution";
        private const string HighShadowTierProperty = "m_AdditionalLightsShadowResolutionTierHigh";
        private const int ShadowAtlasMinimum = 2048;
        private const int HighShadowTier = 512;

        // ACES maps mid-gray to about a tenth and crushes what lies below it; the exposure lifts the cave back into view (D-087).
        private const float PostExposure = 0.8f;
        private const float Contrast = 8f;

        private static readonly PresetValues Low = new PresetValues(0.8f, 1, 30f, 2, 1024, 512, false, false, false);
        private static readonly PresetValues Medium = new PresetValues(1f, 2, 50f, 4, 2048, 1024, true, true, true);

        [MenuItem(MenuPath)]
        public static void Generate()
        {
            EditorAssets.EnsureFolder(GraphicsFolder);
            EnsureVolumeProfile();
            EnsurePresets();
            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Generated the render look: " + VolumeProfilePath + " and the graphics presets in " + GraphicsFolder + ".");
        }

        /// <summary>The post-processing profile of the cavern, created once with the overrides below; afterwards the asset is the truth.</summary>
        /// <summary>The cavern profile; <paramref name="brighten"/> gives an existing one the exposure of D-087, once.</summary>
        public static VolumeProfile EnsureVolumeProfile(bool brighten = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (existing != null)
            {
                if (brighten)
                {
                    Brighten(existing);
                }

                return existing;
            }

            EditorAssets.EnsureFolder(GraphicsFolder);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            Tonemapping tonemapping = Add<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(PostExposure);
            color.contrast.Override(Contrast);
            color.colorFilter.Override(new Color(1f, 0.93f, 0.86f));
            color.saturation.Override(-10f);

            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.7f);

            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.4f);

            FilmGrain grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.25f);
            grain.response.Override(0.8f);

            ChromaticAberration aberration = Add<ChromaticAberration>(profile);
            aberration.intensity.Override(0.08f);

            EditorUtility.SetDirty(profile);
            Log.Info(LogCategory.Editor, "Created the post-processing profile " + VolumeProfilePath + ".");
            return profile;
        }

        /// <summary>The preset catalog: Low and Medium derived from the PC assets once, High being the PC assets themselves.</summary>
        public static GraphicsPresetCatalog EnsurePresets()
        {
            EditorAssets.EnsureFolder(GraphicsFolder);
            var high = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(HighPipelinePath);
            var highRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RenderPipelineSetup.PcRendererPath);
            if (high == null || highRenderer == null)
            {
                throw new IOException("Missing " + HighPipelinePath + " or " + RenderPipelineSetup.PcRendererPath + "; the URP assets of the project are gone.");
            }

            UniversalRenderPipelineAsset low = EnsurePipeline(LowPipelinePath, LowRendererPath, high, highRenderer, Low);
            UniversalRenderPipelineAsset medium = EnsurePipeline(MediumPipelinePath, MediumRendererPath, high, highRenderer, Medium);

            FitPointLightShadows(low);
            FitPointLightShadows(medium);
            FitPointLightShadows(high);
            var presets = new[] { Preset(LowName, low), Preset(MediumName, medium), Preset(HighName, high) };
            GraphicsPresetCatalog catalog = EditorAssets.LoadOrCreate<GraphicsPresetCatalog>(PresetCatalogPath);
            catalog.Configure(presets, presets.Length - 1);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // Lava and fissure lights cast point shadows, six faces each, two lights at a time (D-087). Each light asks for
        // the high tier of the pipeline; at 512 and an atlas of at least 2048, twelve faces fit, so URP neither shrinks
        // them nor logs about it every time the shadow budget hands shadows to other lights.
        private static void FitPointLightShadows(UniversalRenderPipelineAsset pipeline)
        {
            using var serialized = new SerializedObject(pipeline);
            SerializedProperty atlas = serialized.FindProperty(ShadowAtlasProperty);
            SerializedProperty tier = serialized.FindProperty(HighShadowTierProperty);
            if (atlas == null || tier == null)
            {
                throw new IOException("The URP pipeline asset has no " + ShadowAtlasProperty + " or " + HighShadowTierProperty + "; has URP changed?");
            }

            if (atlas.intValue >= ShadowAtlasMinimum && tier.intValue == HighShadowTier)
            {
                return;
            }

            atlas.intValue = Mathf.Max(atlas.intValue, ShadowAtlasMinimum);
            tier.intValue = HighShadowTier;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        private static GraphicsPreset Preset(string name, UniversalRenderPipelineAsset pipeline)
        {
            var preset = new GraphicsPreset();
            preset.Configure(name, pipeline);
            return preset;
        }

        // A copy of the PC pipeline asset with its own renderer copy and the values of the preset; kept once it exists.
        private static UniversalRenderPipelineAsset EnsurePipeline(string pipelinePath, string rendererPath, UniversalRenderPipelineAsset source, UniversalRendererData sourceRenderer, PresetValues values)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (existing != null)
            {
                return existing;
            }

            UniversalRendererData renderer = EnsureRenderer(rendererPath, sourceRenderer, values.AmbientOcclusion);
            UniversalRenderPipelineAsset pipeline = Object.Instantiate(source);
            pipeline.name = Path.GetFileNameWithoutExtension(pipelinePath);
            AssetDatabase.CreateAsset(pipeline, pipelinePath);

            using (var serialized = new SerializedObject(pipeline))
            {
                SerializedProperty renderers = serialized.FindProperty(RendererListProperty);
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                serialized.FindProperty(SoftShadowsProperty).boolValue = values.SoftShadows;
                serialized.FindProperty(AdditionalShadowsProperty).boolValue = values.AdditionalLightShadows;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            pipeline.renderScale = values.RenderScale;
            pipeline.msaaSampleCount = values.MsaaSamples;
            pipeline.shadowDistance = values.ShadowDistance;
            pipeline.shadowCascadeCount = values.ShadowCascades;
            pipeline.mainLightShadowmapResolution = values.MainLightShadowResolution;
            pipeline.additionalLightsShadowmapResolution = values.AdditionalLightShadowResolution;
            EditorUtility.SetDirty(pipeline);
            Log.Info(LogCategory.Editor, "Created the graphics preset " + pipelinePath + ".");
            return pipeline;
        }

        // A copy of the PC renderer with copies of its features, so switching ambient occlusion off here leaves the PC renderer alone.
        private static UniversalRendererData EnsureRenderer(string path, UniversalRendererData source, bool ambientOcclusion)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (existing != null)
            {
                return existing;
            }

            UniversalRendererData copy = Object.Instantiate(source);
            copy.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy, path);

            using var serialized = new SerializedObject(copy);
            SerializedProperty features = serialized.FindProperty(FeaturesProperty);
            SerializedProperty map = serialized.FindProperty(FeatureMapProperty);
            features.arraySize = source.rendererFeatures.Count;
            map.arraySize = source.rendererFeatures.Count;
            for (int i = 0; i < source.rendererFeatures.Count; i++)
            {
                ScriptableRendererFeature feature = Object.Instantiate(source.rendererFeatures[i]);
                feature.name = source.rendererFeatures[i].name;
                if (feature is ScreenSpaceAmbientOcclusion)
                {
                    feature.SetActive(ambientOcclusion);
                }

                AssetDatabase.AddObjectToAsset(feature, copy);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
                features.GetArrayElementAtIndex(i).objectReferenceValue = feature;
                map.GetArrayElementAtIndex(i).longValue = localId;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(copy);
            return copy;
        }

        private static void Brighten(VolumeProfile profile)
        {
            if (!profile.TryGet(out ColorAdjustments color))
            {
                color = Add<ColorAdjustments>(profile);
            }

            color.postExposure.Override(PostExposure);
            color.contrast.Override(Contrast);
            EditorUtility.SetDirty(profile);
            Log.Info(LogCategory.Editor, "Raised the exposure of " + VolumeProfilePath + " to " + PostExposure + " EV (D-087).");
        }

        private static T Add<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            T component = profile.Add<T>(false);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private sealed class PresetValues
        {
            public PresetValues(float renderScale, int msaaSamples, float shadowDistance, int shadowCascades, int mainLightShadowResolution, int additionalLightShadowResolution, bool softShadows, bool additionalLightShadows, bool ambientOcclusion)
            {
                RenderScale = renderScale;
                MsaaSamples = msaaSamples;
                ShadowDistance = shadowDistance;
                ShadowCascades = shadowCascades;
                MainLightShadowResolution = mainLightShadowResolution;
                AdditionalLightShadowResolution = additionalLightShadowResolution;
                SoftShadows = softShadows;
                AdditionalLightShadows = additionalLightShadows;
                AmbientOcclusion = ambientOcclusion;
            }

            public float RenderScale { get; }

            public int MsaaSamples { get; }

            public float ShadowDistance { get; }

            public int ShadowCascades { get; }

            public int MainLightShadowResolution { get; }

            public int AdditionalLightShadowResolution { get; }

            public bool SoftShadows { get; }

            public bool AdditionalLightShadows { get; }

            public bool AmbientOcclusion { get; }
        }
    }
}
