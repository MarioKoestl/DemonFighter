#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class WorldLightingTests
    {
        private const string SettingsPath = "Assets/_Project/Settings/WorldBuildSettings.asset";

        [Test]
        public void ApplyLightingDefaults_OldAsset_GetsTheBrighterLightOnce()
        {
            var settings = ScriptableObject.CreateInstance<WorldBuildSettings>();
            try
            {
                settings.NeedsLightingDefaults.Should().BeTrue("an asset without a lighting version predates D-087");

                settings.ApplyLightingDefaults();

                settings.NeedsLightingDefaults.Should().BeFalse();
                settings.CeilingLightIntensity.Should().BeGreaterThan(1f);
                settings.CeilingLightPitch.Should().BeInRange(30f, 89f, "the light leans a little so shapes read");
                settings.AmbientGroundFraction.Should().BeGreaterThan(0.15f);
                settings.LavaLightIntensity.Should().BeGreaterThan(8f);
                settings.LavaEmbersPerSquareMeter.Should().BePositive();
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Defaults_LightTheCaveWarmWhiteWithAStrongFill()
        {
            var settings = ScriptableObject.CreateInstance<WorldBuildSettings>();
            try
            {
                // An orange light starves the green channel, which carries most of the brightness the eye reads.
                settings.CeilingLightColor.g.Should().BeGreaterThan(0.85f * settings.CeilingLightColor.r);
                Color fill = settings.AmbientColor.linear * settings.AmbientIntensity;
                fill.grayscale.Should().BeGreaterThan(0.5f, "faces turned away from the ceiling light must stay readable");
                settings.FogColor.grayscale.Should().BeGreaterThan(0.3f, "the haze glows instead of swallowing the far cave");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ProjectAsset_HasTheBrighterLight()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldBuildSettings>(SettingsPath);

            (settings != null).Should().BeTrue("run Demon Fighter > Generate > Placeholder Assets");
            settings!.NeedsLightingDefaults.Should().BeFalse();
            settings.CeilingLightIntensity.Should().BeGreaterThan(1f);
        }
    }
}
