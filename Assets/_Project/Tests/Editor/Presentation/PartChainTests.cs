#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Presentation.Demons;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    /// <summary>Bones made from a model that has none, and the motions that pose them (D-098).</summary>
    public sealed class PartChainTests
    {
        private const float Tolerance = 0.001f;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void For_Rod_RunsFromThePivotToTheFarthestPoint()
        {
            PartSkeleton skeleton = PartSkeleton.For(Rod(), Vector3.zero, 3)!;

            skeleton.Length.Should().BeApproximately(1f, 0.02f);
            Vector3.Dot(skeleton.Direction, Vector3.up).Should().BeGreaterThan(0.99f);
            skeleton.Joint(1).y.Should().BeApproximately(skeleton.Length / 3f, Tolerance);
        }

        [Test]
        public void WeightOf_FollowsTheNearestBonesAlongTheChain()
        {
            PartSkeleton skeleton = PartSkeleton.For(Rod(), Vector3.zero, 3)!;

            skeleton.WeightOf(Vector3.zero).boneIndex0.Should().Be(0);
            skeleton.WeightOf(Vector3.zero).weight0.Should().BeApproximately(1f, Tolerance);
            BoneWeight tip = skeleton.WeightOf(Vector3.up * skeleton.Length);
            (tip.boneIndex0 == 2 ? tip.weight0 : tip.weight1).Should().BeApproximately(1f, Tolerance);
            BoneWeight between = skeleton.WeightOf(Vector3.up * (skeleton.Length / 3f));
            (between.weight0 + between.weight1).Should().BeApproximately(1f, Tolerance);
            between.weight1.Should().BeInRange(0.2f, 0.8f, "a point between two bones follows both");
        }

        // The bones created under the part sit exactly where the bind poses expect them at rest, so nothing moves until posed.
        [Test]
        public void CreateBones_MatchTheBindPoses()
        {
            PartSkeleton skeleton = PartSkeleton.For(Rod(), Vector3.zero, 3)!;
            Transform part = Make("Part").transform;

            Transform[] bones = skeleton.CreateBones(part);
            Matrix4x4[] poses = skeleton.BindPoses();

            for (int i = 0; i < bones.Length; i++)
            {
                Matrix4x4 product = poses[i] * bones[i].localToWorldMatrix;
                (product.MultiplyPoint3x4(Vector3.one) - Vector3.one).magnitude.Should().BeLessThan(0.0001f, "bone " + i + " sits on its bind pose");
            }
        }

        [Test]
        public void Elbow_KeepsBothBoneLengthsAndBendsTowardThePole()
        {
            var shoulder = new Vector3(0f, 1f, 0f);
            var target = new Vector3(0f, 1f, 1.2f);

            Vector3 elbow = ReachChain.Elbow(shoulder, target, 0.8f, 0.8f, Vector3.down);

            Vector3.Distance(shoulder, elbow).Should().BeApproximately(0.8f, Tolerance);
            Vector3.Distance(elbow, target).Should().BeApproximately(0.8f, Tolerance);
            elbow.y.Should().BeLessThan(1f, "the elbow bends down, toward the pole");
        }

        [Test]
        public void Elbow_TargetOutOfReach_StretchesTheArmStraight()
        {
            Vector3 elbow = ReachChain.Elbow(Vector3.zero, new Vector3(0f, 0f, 5f), 1f, 1f, Vector3.down);

            elbow.z.Should().BeApproximately(1f, 0.05f);
            Mathf.Abs(elbow.y).Should().BeLessThan(0.1f);
        }

        // An arm at full reach lies straight; pulled in, its wrist comes closer to the shoulder and the elbow drops.
        [Test]
        public void ReachChain_Flex_BendsTheElbowDownAndPullsTheWristIn()
        {
            Transform part = Make("Arm").transform;
            PartSkeleton skeleton = PartSkeleton.For(Rod(), Vector3.zero, 3)!;
            Transform[] bones = skeleton.CreateBones(part);
            part.rotation = Quaternion.Euler(90f, 0f, 0f);
            var chain = new ReachChain(part, bones);

            chain.Update(new ChainContext(0.02f, 0.999f, Vector3.up, Vector3.right, Vector3.forward));
            float straight = Vector3.Distance(bones[0].position, bones[2].position);
            chain.Update(new ChainContext(0.02f, 0.6f, Vector3.up, Vector3.right, Vector3.forward));
            float bent = Vector3.Distance(bones[0].position, bones[2].position);

            bent.Should().BeLessThan(straight * 0.8f);
            bones[1].position.y.Should().BeLessThan(bones[0].position.y, "the elbow sinks below the shoulder of an arm held forward");
        }

        // A turn at the root runs down a tail late, and settles back to rest.
        [Test]
        public void SpringChain_LagsBehindATurnAndSettles()
        {
            Transform part = Make("Tail").transform;
            Transform[] bones = PartSkeleton.For(Rod(), Vector3.zero, 4)!.CreateBones(part);
            var chain = new SpringChain(bones);
            var context = new ChainContext(0.02f, 1f, Vector3.up, Vector3.right, Vector3.forward);
            chain.Update(context);

            part.rotation = Quaternion.Euler(0f, 0f, 40f);
            chain.Update(context);
            float lag = Vector3.Angle(bones[0].forward, bones[1].forward);
            for (int i = 0; i < 300; i++)
            {
                chain.Update(context);
            }

            lag.Should().BeGreaterThan(5f, "the second bone has not caught up yet");
            Vector3.Angle(bones[0].forward, bones[3].forward).Should().BeLessThan(1f, "the tail settles straight again");
        }

        [Test]
        public void SpringChain_HardTurn_NeverFoldsAJointPastItsLimit()
        {
            Transform part = Make("Tail").transform;
            Transform[] bones = PartSkeleton.For(Rod(), Vector3.zero, 4)!.CreateBones(part);
            var chain = new SpringChain(bones);
            var context = new ChainContext(0.02f, 1f, Vector3.up, Vector3.right, Vector3.forward);
            chain.Update(context);

            part.rotation = Quaternion.Euler(0f, 0f, 170f);
            chain.Update(context);

            for (int i = 1; i < bones.Length; i++)
            {
                Vector3.Angle(bones[i - 1].forward, bones[i].forward).Should().BeLessThanOrEqualTo(50.5f, "joint " + i);
            }
        }

        [Test]
        public void Standing_RunsFromTheTopOfTheModelToItsSole()
        {
            PartSkeleton skeleton = PartSkeleton.Standing(Rod(), Vector3.up, new[] { 0f, 0.45f, 0.85f })!;

            skeleton.Root.y.Should().BeApproximately(1f, 0.01f, "the hip is at the top");
            skeleton.Tip.y.Should().BeApproximately(0f, 0.01f, "the sole is at the bottom");
            skeleton.Joint(1).y.Should().BeApproximately(0.55f, 0.01f, "the knee sits 45 percent down the leg");
        }

        // Carried forward at a walk, a planted foot stays where it is, then steps in an arc to catch up, and lands on the ground.
        [Test]
        public void LegChain_Walking_PlantsTheFootAndStepsInAnArc()
        {
            Transform body = Make("Body").transform;
            LegChain leg = Leg(body, 0f, new LegGait(), 0);
            var velocity = new Vector3(0f, 0f, 1f);
            bool stepped = false;
            bool lifted = false;
            bool slid = false;
            Vector3 planted = Vector3.zero;
            bool wasStepping = true;
            for (int frame = 0; frame < 100; frame++)
            {
                body.position += velocity * 0.02f;
                leg.Update(new ChainContext(0.02f, 1f, Vector3.up, Vector3.right, Vector3.forward, velocity, 0f));
                stepped |= leg.Stepping;
                lifted |= leg.Stepping && leg.Foot.y > 0.05f;
                if (!leg.Stepping)
                {
                    slid |= !wasStepping && (leg.Foot - planted).magnitude > 0.0001f;
                    planted = leg.Foot;
                    leg.Foot.y.Should().BeApproximately(0f, 0.0001f, "a planted foot rests on the ground");
                }

                wasStepping = leg.Stepping;
            }

            stepped.Should().BeTrue("two meters of walking take steps");
            lifted.Should().BeTrue("a step lifts the foot");
            slid.Should().BeFalse("a planted foot does not slide");
            leg.Foot.z.Should().BeGreaterThan(1.2f, "the foot keeps up with the body");
        }

        [Test]
        public void LegChain_PairSharingAGait_NeverStepsWithBothFeetAtOnce()
        {
            Transform body = Make("Body").transform;
            var gait = new LegGait();
            LegChain right = Leg(body, 0.2f, gait, 0);
            LegChain left = Leg(body, -0.2f, gait, 1);
            var velocity = new Vector3(0f, 0f, 2f);
            for (int frame = 0; frame < 150; frame++)
            {
                body.position += velocity * 0.02f;
                var context = new ChainContext(0.02f, 1f, Vector3.up, Vector3.right, Vector3.forward, velocity, 0f);
                right.Update(context);
                left.Update(context);
                (right.Stepping && left.Stepping).Should().BeFalse("frame " + frame);
            }
        }

        // A leg one unit tall under a body: a standing rod with its hip at the top, at a side offset.
        private LegChain Leg(Transform body, float side, LegGait gait, int index)
        {
            Transform part = Make("Leg").transform;
            part.SetParent(body, false);
            part.localPosition = new Vector3(side, 0f, 0f);
            PartSkeleton skeleton = PartSkeleton.Standing(Rod(), Vector3.up, new[] { 0f, 0.45f, 0.85f })!;
            return new LegChain(part, skeleton.CreateBones(part), skeleton.Tip, gait, index);
        }

        private GameObject Make(string name)
        {
            var created = new GameObject(name);
            _created.Add(created);
            return created;
        }

        // A thin rod one unit tall standing on the origin, readable like the part models.
        private Mesh Rod()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int sides = 8;
            const int rings = 11;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * 0.05f, ring / (float)(rings - 1), Mathf.Sin(angle) * 0.05f));
                }
            }

            for (int ring = 0; ring < rings - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int a = (ring * sides) + side;
                    int b = (ring * sides) + ((side + 1) % sides);
                    triangles.AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            _created.Add(mesh);
            return mesh;
        }
    }
}
