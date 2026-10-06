#nullable enable
using AwesomeAssertions;
using DemonFighter.Data;
using DemonFighter.Presentation.Demons;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class PartMeshSetTests
    {
        private Mesh _intact = null!;
        private Mesh _wounded = null!;
        private Mesh _mangled = null!;
        private Mesh _stump = null!;

        [SetUp]
        public void CreateMeshes()
        {
            _intact = new Mesh { name = "Intact" };
            _wounded = new Mesh { name = "Wounded" };
            _mangled = new Mesh { name = "Mangled" };
            _stump = new Mesh { name = "Stump" };
        }

        [TearDown]
        public void DestroyMeshes()
        {
            Object.DestroyImmediate(_intact);
            Object.DestroyImmediate(_wounded);
            Object.DestroyImmediate(_mangled);
            Object.DestroyImmediate(_stump);
        }

        [Test]
        public void MeshFor_CompleteSet_GivesEveryStateItsOwnMesh()
        {
            PartMeshSet set = Set(_intact, _wounded, _mangled, _stump);

            set.MeshFor(DamageStage.Intact).Should().BeSameAs(_intact);
            set.MeshFor(DamageStage.Wounded).Should().BeSameAs(_wounded);
            set.MeshFor(DamageStage.Mangled).Should().BeSameAs(_mangled);
            set.MeshFor(DamageStage.Lost).Should().BeSameAs(_stump);
            set.HasStump.Should().BeTrue();
        }

        [Test]
        public void MeshFor_MissingStates_FallBackToTheNextHealthierMesh()
        {
            PartMeshSet set = Set(_intact, null, null, null);

            set.MeshFor(DamageStage.Wounded).Should().BeSameAs(_intact);
            set.MeshFor(DamageStage.Mangled).Should().BeSameAs(_intact);
        }

        [Test]
        public void MeshFor_MissingMangled_UsesTheWoundedMesh()
        {
            PartMeshSet set = Set(_intact, _wounded, null, null);

            set.MeshFor(DamageStage.Mangled).Should().BeSameAs(_wounded);
        }

        [Test]
        public void MeshFor_LostWithoutStump_DrawsNothing()
        {
            PartMeshSet set = Set(_intact, _wounded, _mangled, null);

            Mesh? mesh = set.MeshFor(DamageStage.Lost);

            (mesh == null).Should().BeTrue();
            set.HasStump.Should().BeFalse();
        }

        [Test]
        public void Constructor_NonPositiveScale_FallsBackToOne()
        {
            var set = new PartMeshSet(_intact, null, null, null, null, null, 0f, Vector3.zero, Quaternion.identity);

            set.Scale.Should().Be(1f);
        }

        [Test]
        public void TryFrom_UnboundDefinition_IsFalse()
        {
            var definition = new PartMeshSetDefinition();

            bool bound = PartMeshSet.TryFrom(definition, out _);

            bound.Should().BeFalse();
            definition.HasMeshes.Should().BeFalse();
        }

        [Test]
        public void TryFrom_BoundDefinition_ReadsMeshesAndFit()
        {
            var definition = new PartMeshSetDefinition();
            definition.SetMeshes(_intact, _wounded, null, _stump, null);
            definition.SetFit(2f, new Vector3(0f, 0.1f, 0f), new Vector3(0f, 90f, 0f));

            bool bound = PartMeshSet.TryFrom(definition, out PartMeshSet set);

            bound.Should().BeTrue();
            set.Intact.Should().BeSameAs(_intact);
            set.MeshFor(DamageStage.Mangled).Should().BeSameAs(_wounded);
            set.HasStump.Should().BeTrue();
            set.HasOwnMaterial.Should().BeFalse();
            (set.Clip == null).Should().BeTrue();
            set.Scale.Should().Be(2f);
            set.Offset.Should().Be(new Vector3(0f, 0.1f, 0f));
            (set.Rotation * Vector3.forward).x.Should().BeApproximately(1f, 0.001f);
        }

        // A Meshy file carries Blender's upright turn and a scale of 100 on its object; the knobs turn and scale on top (D-088).
        [Test]
        public void TryFrom_ImportFix_SitsUnderTheKnobs()
        {
            var definition = new PartMeshSetDefinition();
            definition.SetMeshes(_intact, null, null, null, null);
            definition.SetImportFix(100f, Quaternion.Euler(-90f, 0f, 0f), new Vector3(0f, 0f, -0.0095f));
            definition.SetFit(0.0055f, Vector3.zero, new Vector3(90f, 0f, 0f));

            PartMeshSet.TryFrom(definition, out PartMeshSet set);

            set.Scale.Should().BeApproximately(0.55f, 0.0001f);
            set.Pivot.Should().Be(new Vector3(0f, 0f, -0.0095f));
            // The file turns its +Z (the tool's up) to +Y; the knob then turns +Y to +Z, along the socket.
            Vector3 toolUp = set.Rotation * Vector3.forward;
            toolUp.z.Should().BeApproximately(1f, 0.001f);
        }

        [Test]
        public void TryFrom_UnsetImportFix_ChangesNothing()
        {
            var definition = new PartMeshSetDefinition();
            definition.SetMeshes(_intact, null, null, null, null);

            PartMeshSet.TryFrom(definition, out PartMeshSet set);

            definition.ImportScale.Should().Be(1f);
            set.Scale.Should().Be(1f);
            set.Rotation.Should().Be(Quaternion.identity);
        }

        // The left arm is the mirror image of the right one: meshes, offset, turn and pivot (D-088).
        [Test]
        public void Mirrored_FlipsTheMeshesTheFitAndThePivot()
        {
            var set = new PartMeshSet(_intact, _wounded, null, _stump, null, null, 0.5f, new Vector3(0.1f, 0.2f, 0.3f), Quaternion.Euler(90f, -90f, 0f), new Vector3(0.01f, 0f, -0.0095f));
            try
            {
                PartMeshSet mirrored = set.Mirrored();

                mirrored.Intact.Should().NotBeSameAs(_intact);
                mirrored.Intact.Should().BeSameAs(MirroredMeshes.For(_intact));
                mirrored.MeshFor(DamageStage.Wounded).Should().BeSameAs(MirroredMeshes.For(_wounded));
                mirrored.HasStump.Should().BeTrue();
                mirrored.Scale.Should().Be(0.5f);
                mirrored.Offset.Should().Be(new Vector3(-0.1f, 0.2f, 0.3f));
                mirrored.Pivot.Should().Be(new Vector3(-0.01f, 0f, -0.0095f));
                Quaternion.Angle(mirrored.Rotation, Quaternion.Euler(90f, 90f, 0f)).Should().BeLessThan(0.01f);
            }
            finally
            {
                MirroredMeshes.Clear();
            }
        }

        private static PartMeshSet Set(Mesh intact, Mesh? wounded, Mesh? mangled, Mesh? stump)
        {
            return new PartMeshSet(intact, wounded, mangled, stump, null, null, 1f, Vector3.zero, Quaternion.identity);
        }
    }
}
