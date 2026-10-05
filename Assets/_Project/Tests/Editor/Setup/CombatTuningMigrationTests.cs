#nullable enable
using AwesomeAssertions;
using DemonFighter.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Setup
{
    /// <summary>Kill XP (D-090) and tiers through evolution (D-091) changed the progression; older tuning assets catch up once.</summary>
    public sealed class CombatTuningMigrationTests
    {
        private const string TuningPath = "Assets/_Project/Content/Catalog/CombatTuning.asset";

        [Test]
        public void ApplyProgressionDefaults_OldAsset_GetsTheNewProgressionOnce()
        {
            var definition = ScriptableObject.CreateInstance<CombatTuningDefinition>();
            try
            {
                definition.NeedsProgressionDefaults.Should().BeTrue("an asset without a progression version predates D-090");

                definition.ApplyProgressionDefaults();

                definition.NeedsProgressionDefaults.Should().BeFalse();
                using var serialized = new SerializedObject(definition);
                serialized.FindProperty("_killXpBase").floatValue.Should().Be(100f);
                serialized.FindProperty("_killXpPerVictimLevel").floatValue.Should().Be(0.25f);
                serialized.FindProperty("_secondEvolutionLevel").intValue.Should().Be(5, "every tier evolves at level 5 (D-091)");
                serialized.FindProperty("_levelXpPerTier").floatValue.Should().Be(1f);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ProjectAsset_HasTheNewProgression()
        {
            var definition = AssetDatabase.LoadAssetAtPath<CombatTuningDefinition>(TuningPath);

            (definition != null).Should().BeTrue("run Demon Fighter > Generate > Placeholder Assets");
            definition!.NeedsProgressionDefaults.Should().BeFalse();
            definition.ToSpec().KillXpBase.Should().BeGreaterThanOrEqualTo(100f);
        }
    }
}
