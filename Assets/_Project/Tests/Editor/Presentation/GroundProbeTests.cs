#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    /// <summary>The ground under gore: viscera and blood lie on the floor, never on a demon's controller capsule.</summary>
    public sealed class GroundProbeTests
    {
        private const float Tolerance = 0.001f;
        private const float ProbeHeight = 3f;
        private const float ProbeDepth = 30f;

        // Far from anything else the open scene may hold.
        private static readonly Vector3 Spot = new Vector3(5000f, 0f, 5000f);

        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void RemoveTheStage()
        {
            foreach (GameObject created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        // The viscera of a death landed on the dying demon's capsule and hung there once the corpse switched it off.
        [Test]
        public void TryFind_UnderADemonsController_FindsTheFloorNotTheCapsule()
        {
            Floor();
            GameObject demon = Make(new GameObject("Demon"));
            demon.transform.position = Spot;
            CharacterController controller = demon.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = 0.5f;
            Physics.SyncTransforms();

            bool found = GroundProbe.TryFind(Spot + Vector3.up, ProbeHeight, ProbeDepth, out Vector3 ground);

            found.Should().BeTrue();
            ground.y.Should().BeApproximately(0f, Tolerance, "the capsule top two meters up is no ground");
        }

        [Test]
        public void TryFind_UnderAFallenPart_FindsTheFloor()
        {
            Floor();
            GameObject part = Make(GameObject.CreatePrimitive(PrimitiveType.Cube));
            part.layer = Layers.Food;
            part.transform.position = Spot + (Vector3.up * 0.5f);
            Physics.SyncTransforms();

            GroundProbe.TryFind(Spot + Vector3.up, ProbeHeight, ProbeDepth, out Vector3 ground).Should().BeTrue();

            ground.y.Should().BeApproximately(0f, Tolerance);
        }

        [Test]
        public void TryFind_NothingBelow_KeepsThePositionAndSaysSo()
        {
            Vector3 position = Spot + (Vector3.up * 100f);

            bool found = GroundProbe.TryFind(position, ProbeHeight, ProbeDepth, out Vector3 ground);

            found.Should().BeFalse();
            ground.Should().Be(position);
        }

        // A slab twenty meters wide whose top is at the height of the spot.
        private void Floor()
        {
            GameObject floor = Make(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = Spot + (Vector3.down * 0.5f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
        }

        private GameObject Make(GameObject created)
        {
            _created.Add(created);
            return created;
        }
    }
}
