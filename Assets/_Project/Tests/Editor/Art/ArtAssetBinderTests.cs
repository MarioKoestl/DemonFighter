#nullable enable
using AwesomeAssertions;
using DemonFighter.Editor.Art;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Art
{
    public sealed class ArtAssetBinderTests
    {
        private const float Tolerance = 0.001f;

        // The core was bound before the binder read the file's own turn, and Mario compensated with Euler X -90 (D-088).
        [Test]
        public void MigratedEuler_KnobThatUndidTheFileTurn_BecomesZero()
        {
            Vector3 migrated = ArtAssetBinder.MigratedEuler(new Vector3(-90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f));

            migrated.Should().Be(Vector3.zero);
        }

        [Test]
        public void MigratedEuler_KeepsTheLook()
        {
            var knob = new Vector3(0f, 180f, 0f);
            Quaternion import = Quaternion.Euler(-90f, 0f, 0f);

            Vector3 migrated = ArtAssetBinder.MigratedEuler(knob, import);

            Quaternion.Angle(Quaternion.Euler(migrated) * import, Quaternion.Euler(knob)).Should().BeLessThan(Tolerance);
        }

        // Mario's arm: 1.9 units tall in Meshy, origin in the middle, Blender's -90 turn and centimeter scale on its object.
        [Test]
        public void ImportFix_MeshyArmWithItsOriginInTheMiddle_PivotsAtTheShoulderAndScalesToOne()
        {
            var raw = new Bounds(Vector3.zero, new Vector3(0.0055f, 0.0052f, 0.019f));
            Matrix4x4 file = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90f, 0f, 0f), Vector3.one * 100f);

            ArtAssetBinder.ImportFix(raw, file, out float scale, out Vector3 pivot);

            (0.019f * scale).Should().BeApproximately(1f, Tolerance, "the longest side becomes one unit");
            pivot.x.Should().BeApproximately(0f, Tolerance);
            pivot.y.Should().BeApproximately(0f, Tolerance);
            pivot.z.Should().BeApproximately(-0.0095f, 0.00001f, "the bottom of the upright arm is the raw mesh's lowest Z");
        }

        [Test]
        public void ImportFix_CoreWithItsOriginAtTheBottom_KeepsItThere()
        {
            var raw = new Bounds(new Vector3(0f, 0f, 0.005f), new Vector3(0.0073f, 0.0062f, 0.01f));
            Matrix4x4 file = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90f, 0f, 0f), Vector3.one * 100f);

            ArtAssetBinder.ImportFix(raw, file, out float scale, out Vector3 pivot);

            scale.Should().BeApproximately(100f, 0.01f);
            pivot.magnitude.Should().BeLessThan(0.00001f);
        }

        [Test]
        public void Clean_BringsAnglesIntoTheSignedRangeAndDropsNoise()
        {
            Vector3 cleaned = ArtAssetBinder.Clean(new Vector3(270f, 359.99997f, 4.06e-13f));

            cleaned.x.Should().BeApproximately(-90f, Tolerance);
            cleaned.y.Should().Be(0f);
            cleaned.z.Should().Be(0f);
        }
    }
}
