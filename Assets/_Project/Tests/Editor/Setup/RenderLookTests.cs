#nullable enable
using AwesomeAssertions;
using DemonFighter.Editor.Generate;
using DemonFighter.Editor.Setup;
using DemonFighter.Presentation.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DemonFighter.Editor.Tests.Setup
{
    public sealed class RenderLookTests
    {
        // The generator creates these assets once; the project ships with them (D-083).
        [Test]
        public void CavernProfile_GradesTonemapsAndBlooms()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RenderLookGenerator.VolumeProfilePath);

            (profile != null).Should().BeTrue("run Demon Fighter > Generate > Placeholder Assets");
            profile!.TryGet(out Tonemapping tonemapping).Should().BeTrue();
            tonemapping.mode.value.Should().Be(TonemappingMode.ACES);
            profile.Has<Bloom>().Should().BeTrue();
            profile.Has<Vignette>().Should().BeTrue();
            profile.TryGet(out ColorAdjustments color).Should().BeTrue();
            color.postExposure.value.Should().BeGreaterThan(0.5f, "ACES maps mid-gray to a tenth; the exposure lifts the cave back into view (D-087)");
            profile.Has<FilmGrain>().Should().BeTrue();
            profile.Has<MotionBlur>().Should().BeFalse("motion blur stays off");
        }

        [Test]
        public void Presets_RunLowMediumHigh_WithHighBeingThePcAssets()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GraphicsPresetCatalog>(RenderLookGenerator.PresetCatalogPath);
            var pcPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RenderLookGenerator.HighPipelinePath);

            (catalog != null).Should().BeTrue("run Demon Fighter > Generate > Placeholder Assets");
            catalog!.Presets.Should().HaveCount(3);
            catalog.Presets[0].Name.Should().Be(RenderLookGenerator.LowName);
            catalog.Presets[1].Name.Should().Be(RenderLookGenerator.MediumName);
            catalog.Presets[2].Name.Should().Be(RenderLookGenerator.HighName);
            ReferenceEquals(catalog.Presets[2].Pipeline, pcPipeline).Should().BeTrue();
            catalog.DefaultIndex.Should().Be(2);
            catalog.Find("medium").Should().BeSameAs(catalog.Presets[1]);
            catalog.Find("unknown").Should().BeSameAs(catalog.Presets[2]);
        }

        [Test]
        public void Presets_CostLessTheLowerTheyGo()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GraphicsPresetCatalog>(RenderLookGenerator.PresetCatalogPath);
            UniversalRenderPipelineAsset low = catalog!.Presets[0].Pipeline;
            UniversalRenderPipelineAsset medium = catalog.Presets[1].Pipeline;
            UniversalRenderPipelineAsset high = catalog.Presets[2].Pipeline;

            low.renderScale.Should().BeLessThan(medium.renderScale);
            low.shadowDistance.Should().BeLessThan(medium.shadowDistance);
            low.shadowCascadeCount.Should().BeLessThan(medium.shadowCascadeCount);
            low.mainLightShadowmapResolution.Should().BeLessThan(high.mainLightShadowmapResolution);
            low.supportsSoftShadows.Should().BeFalse();
            medium.supportsSoftShadows.Should().BeTrue();
        }

        [Test]
        public void Presets_HaveTheirOwnRenderersWithAmbientOcclusionOffOnLow()
        {
            var lowRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RenderLookGenerator.LowRendererPath);
            var mediumRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RenderLookGenerator.MediumRendererPath);
            var pcRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RenderPipelineSetup.PcRendererPath);

            (lowRenderer != null && mediumRenderer != null).Should().BeTrue("run Demon Fighter > Generate > Placeholder Assets");
            lowRenderer!.rendererFeatures.Should().HaveCount(pcRenderer.rendererFeatures.Count);
            lowRenderer.rendererFeatures.Should().NotContain(pcRenderer.rendererFeatures, "the copies must not share the PC features");
            AmbientOcclusion(lowRenderer).isActive.Should().BeFalse();
            AmbientOcclusion(mediumRenderer!).isActive.Should().BeTrue();
            AmbientOcclusion(pcRenderer).isActive.Should().BeTrue();
            lowRenderer.rendererFeatures.Should().Contain(feature => feature is DecalRendererFeature);
        }

        private static ScriptableRendererFeature AmbientOcclusion(UniversalRendererData renderer)
        {
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (feature is ScreenSpaceAmbientOcclusion)
                {
                    return feature;
                }
            }

            throw new System.InvalidOperationException("No ambient occlusion feature on " + renderer.name + ".");
        }
    }
}
