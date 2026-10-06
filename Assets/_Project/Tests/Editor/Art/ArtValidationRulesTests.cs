#nullable enable
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Editor.Art;
using DemonFighter.Simulation.Content;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Art
{
    public sealed class ArtValidationRulesTests
    {
        private static readonly PartFacts Core = new PartFacts { Id = "part.core", Fate = PartFate.Destroyed, IsCore = true };
        private static readonly PartFacts Arm = new PartFacts { Id = "part.arm", Fate = PartFate.Severed };
        private static readonly PartFacts Eyes = new PartFacts { Id = "part.eyes", Fate = PartFate.Destroyed };
        private static readonly PartFacts[] Parts = { Core, Arm, Eyes };
        private static readonly MeshState[] AllStates = { MeshState.Intact, MeshState.Wounded, MeshState.Mangled, MeshState.Stump };
        private static readonly string[] AllSockets = { "Socket_Head", "Socket_LimbL", "Socket_LimbR", "Socket_Locomotion", "Socket_Hide", "Socket_Tail" };

        [Test]
        public void Check_CompleteSeveredPart_HasNoFindings()
        {
            ModelFacts arm = Model("BP_Arm", AllStates);

            ArtValidationReport report = Check(arm);

            report.Errors.Should().BeEmpty();
            report.Warnings.Should().BeEmpty();
            report.ModelsChecked.Should().Be(1);
        }

        [Test]
        public void Check_NameOutsideTheConvention_IsAnError()
        {
            ArtValidationReport report = Check(Model("Arm_Final", AllStates));

            report.Errors.Should().ContainSingle().Which.Should().Contain("BP_<Part>");
        }

        [Test]
        public void Check_UnknownPart_IsAnError()
        {
            ArtValidationReport report = Check(Model("BP_Wing", AllStates));

            report.Errors.Should().ContainSingle().Which.Should().Contain("part.wing");
        }

        [Test]
        public void Check_TrianglesOverBudget_IsAnError()
        {
            ModelFacts heavy = Model("BP_Arm", AllStates) with { Triangles = 5001, TriangleBudget = 5000 };

            ArtValidationReport report = Check(heavy);

            report.Errors.Should().ContainSingle().Which.Should().Contain("5001").And.Contain("5000");
        }

        [Test]
        public void Check_WithoutLicenseEntry_IsAnError()
        {
            ModelFacts unlicensed = Model("BP_Arm", AllStates) with { HasLicenseEntry = false };

            ArtValidationReport report = Check(unlicensed);

            report.Errors.Should().ContainSingle().Which.Should().Contain("ASSET_LICENSES");
        }

        [Test]
        public void Check_StatesSpreadOverFiles_CountTogether()
        {
            ModelFacts intact = Model("BP_Arm_Intact", new[] { MeshState.Intact });
            ModelFacts wounded = Model("BP_Arm_Wounded", new[] { MeshState.Wounded });
            ModelFacts mangled = Model("BP_Arm_Mangled", new[] { MeshState.Mangled });
            ModelFacts stump = Model("BP_Arm_Stump", new[] { MeshState.Stump });

            ArtValidationReport report = Check(intact, wounded, mangled, stump);

            report.Errors.Should().BeEmpty();
            report.Warnings.Should().BeEmpty();
        }

        [Test]
        public void Check_PartWithoutIntactMesh_IsAnError()
        {
            ArtValidationReport report = Check(Model("BP_Arm_Wounded", new[] { MeshState.Wounded }));

            report.Errors.Should().ContainSingle().Which.Should().Contain("_Intact");
        }

        [Test]
        public void Check_SeveredPartWithOnlyIntact_WarnsAboutEveryMissingState()
        {
            ArtValidationReport report = Check(Model("BP_Arm", new[] { MeshState.Intact }));

            report.Errors.Should().BeEmpty();
            report.Warnings.Should().HaveCount(3);
            report.Warnings.Should().Contain(w => w.Contains("_Wounded"));
            report.Warnings.Should().Contain(w => w.Contains("_Mangled"));
            report.Warnings.Should().Contain(w => w.Contains("_Stump"));
        }

        [Test]
        public void Check_DestroyedPartWithoutStump_DoesNotAskForOne()
        {
            ArtValidationReport report = Check(Model("BP_Eyes", new[] { MeshState.Intact, MeshState.Wounded, MeshState.Mangled }));

            report.Warnings.Should().BeEmpty();
        }

        [Test]
        public void Check_CoreWithoutSockets_Warns()
        {
            ModelFacts core = Model("BP_Core", new[] { MeshState.Intact, MeshState.Wounded, MeshState.Mangled }) with { Sockets = Array.Empty<string>() };

            ArtValidationReport report = Check(core);

            report.Warnings.Should().ContainSingle().Which.Should().Contain("Socket_");
        }

        [Test]
        public void Check_CoreMissingSomeSockets_ListsThem()
        {
            ModelFacts core = Model("BP_Core", new[] { MeshState.Intact, MeshState.Wounded, MeshState.Mangled }) with { Sockets = new[] { "Socket_Head", "Socket_LimbL" } };

            ArtValidationReport report = Check(core);

            report.Warnings.Should().ContainSingle().Which.Should().Contain("Socket_LimbR").And.Contain("Socket_Tail").And.NotContain("Socket_Head,");
        }

        [Test]
        public void Check_CompleteCore_HasNoFindings()
        {
            ModelFacts core = Model("BP_Core", new[] { MeshState.Intact, MeshState.Wounded, MeshState.Mangled }) with { Sockets = AllSockets, TriangleBudget = 8000 };

            ArtValidationReport report = Check(core);

            report.Errors.Should().BeEmpty();
            report.Warnings.Should().BeEmpty();
        }

        [Test]
        public void Check_CompleteCoreWithSockets_HasNoWarnings()
        {
            ModelFacts core = Model("BP_Core", new[] { MeshState.Intact, MeshState.Wounded, MeshState.Mangled }) with
            {
                Sockets = AllSockets,
            };

            ArtValidationReport report = Check(core);

            report.Warnings.Should().BeEmpty();
        }

        [Test]
        public void Check_TextureOverBudget_Warns()
        {
            var texture = new TextureFacts { Path = "Assets/_Project/Art/Models/BodyParts/Materials/BP_Arm_BaseColor.png", MaxSize = 2048, Budget = 1024 };

            ArtValidationReport report = ArtValidationRules.Check(Array.Empty<ModelFacts>(), new[] { texture }, Parts);

            report.Warnings.Should().ContainSingle().Which.Should().Contain("2048").And.Contain("1024");
            report.TexturesChecked.Should().Be(1);
        }

        [Test]
        public void ToText_ListsErrorsBeforeWarnings()
        {
            ArtValidationReport report = Check(Model("BP_Wing", AllStates), Model("BP_Arm", new[] { MeshState.Intact }));

            string text = report.ToText();

            text.Should().StartWith("Art validation: 2 model(s)");
            text.IndexOf("ERROR", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("WARNING", StringComparison.Ordinal));
        }

        private static ArtValidationReport Check(params ModelFacts[] models)
        {
            return ArtValidationRules.Check(models, Array.Empty<TextureFacts>(), Parts);
        }

        // A model that passes every rule for a body part: within budget, licensed, pivot at the socket, +Z away.
        private static ModelFacts Model(string name, IReadOnlyList<MeshState> states)
        {
            return new ModelFacts
            {
                Path = "Assets/_Project/Art/Models/BodyParts/" + name + ".fbx",
                Name = name,
                Triangles = 1200,
                TriangleBudget = 5000,
                States = states,
                Sockets = Array.Empty<string>(),
                HasLicenseEntry = true,
            };
        }
    }
}
