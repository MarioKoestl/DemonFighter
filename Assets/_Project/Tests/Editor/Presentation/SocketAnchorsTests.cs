#nullable enable
using AwesomeAssertions;
using DemonFighter.Data;
using DemonFighter.Presentation.Demons;
using DemonFighter.Simulation.Content;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class SocketAnchorsTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Normalize_PointOnAModel_BecomesBodyUnitsFromTheBase()
        {
            // A model two units tall whose base sits at y 0.2 and whose center is off by 0.1 in x.
            var bounds = new Bounds(new Vector3(0.1f, 1.2f, 0f), new Vector3(1f, 2f, 1f));

            Vector3 normalized = SocketAnchors.Normalize(new Vector3(0.4f, 1.2f, 0.5f), bounds);

            normalized.x.Should().BeApproximately(0.15f, Tolerance);
            normalized.y.Should().BeApproximately(0.5f, Tolerance);
            normalized.z.Should().BeApproximately(0.25f, Tolerance);
        }

        [Test]
        public void Normalize_FlatBounds_DoesNotDivideByZero()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);

            Vector3 normalized = SocketAnchors.Normalize(new Vector3(0f, 1f, 0f), bounds);

            float.IsInfinity(normalized.y).Should().BeFalse();
            float.IsNaN(normalized.y).Should().BeFalse();
        }

        [Test]
        public void TryFind_CopyWithItsOwnAnchor_UsesIt()
        {
            SocketAnchorDefinition[] anchors = { Anchor(SocketKind.Head, 0f, 0.6f, 0.3f), Anchor(SocketKind.Limb, 0.3f, 0.5f, 0f), Anchor(SocketKind.Limb, -0.3f, 0.5f, 0f) };

            bool found = SocketAnchors.TryFind(anchors, SocketKind.Limb, 1, out Vector3 position, out _);

            found.Should().BeTrue();
            position.x.Should().BeApproximately(-0.3f, Tolerance);
        }

        [Test]
        public void TryFind_MoreCopiesThanAnchors_MirrorsTheLastOneForOddCopies()
        {
            SocketAnchorDefinition[] anchors = { Anchor(SocketKind.Limb, 0.3f, 0.5f, 0f, euler: new Vector3(0f, 90f, 0f)) };

            SocketAnchors.TryFind(anchors, SocketKind.Limb, 1, out Vector3 second, out Quaternion secondRotation);
            SocketAnchors.TryFind(anchors, SocketKind.Limb, 2, out Vector3 third, out _);

            second.x.Should().BeApproximately(-0.3f, Tolerance);
            (secondRotation * Vector3.forward).x.Should().BeApproximately(-1f, Tolerance);
            third.x.Should().BeApproximately(0.3f, Tolerance);
        }

        [Test]
        public void TryFind_KindWithoutAnchor_IsFalse()
        {
            SocketAnchorDefinition[] anchors = { Anchor(SocketKind.Head, 0f, 0.6f, 0.3f) };

            bool found = SocketAnchors.TryFind(anchors, SocketKind.Tail, 0, out Vector3 position, out Quaternion rotation);

            found.Should().BeFalse();
            position.Should().Be(Vector3.zero);
            rotation.Should().Be(Quaternion.identity);
        }

        // The offset turns with the anchor: Z pushes both arms out of their flanks, not forward (D-088).
        [Test]
        public void Place_OffsetZ_PushesBothArmsOutOfTheBody()
        {
            var offset = new Vector3(0f, 0.05f, 0.1f);

            Vector3 right = SocketAnchors.Place(new Vector3(0.29f, 0.52f, 0.06f), Quaternion.Euler(0f, 90f, 0f), offset);
            Vector3 left = SocketAnchors.Place(new Vector3(-0.29f, 0.52f, 0.06f), Quaternion.Euler(0f, -90f, 0f), offset);

            right.x.Should().BeApproximately(0.39f, Tolerance);
            left.x.Should().BeApproximately(-0.39f, Tolerance);
            right.y.Should().BeApproximately(0.57f, Tolerance);
            left.y.Should().BeApproximately(0.57f, Tolerance);
            right.z.Should().BeApproximately(0.06f, Tolerance);
            left.z.Should().BeApproximately(0.06f, Tolerance);
        }

        [Test]
        public void Place_HeadAnchor_KeepsTheOffsetAsItIs()
        {
            Vector3 eyes = SocketAnchors.Place(new Vector3(0f, 0.62f, 0.28f), Quaternion.identity, new Vector3(0f, 0.12f, 0.02f));

            eyes.y.Should().BeApproximately(0.74f, Tolerance);
            eyes.z.Should().BeApproximately(0.3f, Tolerance);
        }

        // A Meshy arm stands upright with its origin in the middle; the pivot is its shoulder, the bottom of the mesh.
        [Test]
        public void PlaceMesh_ArmTippedOut_PutsItsShoulderOnTheAnchor()
        {
            var mesh = new Mesh();
            try
            {
                var set = new PartMeshSet(mesh, null, null, null, null, null, 0.5f, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0f, -1f, 0f));
                var anchor = new Vector3(0.29f, 0.52f, 0.06f);

                SocketAnchors.PlaceMesh(anchor, Quaternion.Euler(0f, 90f, 0f), set, out Vector3 position, out Quaternion rotation);

                Vector3 shoulder = position + (rotation * (set.Pivot * set.Scale));
                (shoulder - anchor).magnitude.Should().BeLessThan(Tolerance);
                (rotation * Vector3.up).x.Should().BeApproximately(1f, Tolerance, "tipped by X 90 the arm points out of the right flank");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        // Euler (90, -90, 0) swings the arm from pointing out to pointing forward; the mirrored left arm must follow (D-088).
        [Test]
        public void PlaceMesh_MirroredLeftArm_PointsForwardLikeTheRightOne()
        {
            var mesh = new Mesh();
            try
            {
                var set = new PartMeshSet(mesh, null, null, null, null, null, 0.5f, new Vector3(-0.05f, 0f, 0.03f), Quaternion.Euler(90f, -90f, 0f), new Vector3(0f, -1f, 0f));

                PartMeshSet mirrored = set.Mirrored();
                var leftAnchor = new Vector3(-0.29f, 0.52f, 0.06f);
                SocketAnchors.PlaceMesh(new Vector3(0.29f, 0.52f, 0.06f), Quaternion.Euler(0f, 90f, 0f), set, out Vector3 right, out Quaternion rightRotation);
                SocketAnchors.PlaceMesh(leftAnchor, Quaternion.Euler(0f, -90f, 0f), mirrored, out Vector3 left, out Quaternion leftRotation);

                (rightRotation * Vector3.up).z.Should().BeApproximately(1f, Tolerance);
                (leftRotation * Vector3.up).z.Should().BeApproximately(1f, Tolerance);
                left.x.Should().BeApproximately(-right.x, Tolerance);
                left.z.Should().BeApproximately(right.z, Tolerance);
                (left + (leftRotation * (mirrored.Pivot * mirrored.Scale)) - leftAnchor - (Quaternion.Euler(0f, -90f, 0f) * mirrored.Offset)).magnitude.Should().BeLessThan(Tolerance, "the left shoulder sits on the left anchor");
            }
            finally
            {
                MirroredMeshes.Clear();
                Object.DestroyImmediate(mesh);
            }
        }

        private static SocketAnchorDefinition Anchor(SocketKind kind, float x, float y, float z, Vector3 euler = default)
        {
            var anchor = new SocketAnchorDefinition();
            anchor.Configure(kind, new Vector3(x, y, z), euler);
            return anchor;
        }
    }
}
