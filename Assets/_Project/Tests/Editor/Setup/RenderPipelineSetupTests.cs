#nullable enable
using AwesomeAssertions;
using DemonFighter.Editor.Setup;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace DemonFighter.Editor.Tests.Setup
{
    public sealed class RenderPipelineSetupTests
    {
        [Test]
        public void PcRenderer_DrawsDecals()
        {
            // The generator adds the feature once and the renderer asset is committed with it (D-081).
            RenderPipelineSetup.HasDecalFeature().Should().BeTrue("run Demon Fighter > Generate > Placeholder Assets");
        }

        [Test]
        public void EnsureDecalFeature_WhenPresent_AddsNothingTwice()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RenderPipelineSetup.PcRendererPath);
            int before = renderer.rendererFeatures.Count;

            bool added = RenderPipelineSetup.EnsureDecalFeature();

            added.Should().BeFalse();
            renderer.rendererFeatures.Count.Should().Be(before);
        }

        [Test]
        public void PcRenderer_KeepsAmbientOcclusionNextToDecals()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RenderPipelineSetup.PcRendererPath);

            renderer.rendererFeatures.Should().Contain(feature => feature is ScreenSpaceAmbientOcclusion);
            renderer.rendererFeatures.Should().Contain(feature => feature is DecalRendererFeature);
        }
    }
}
