#nullable enable
using System;
using DemonFighter.Data;
using UnityEngine;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>What a bone chain reads each frame from the body it hangs on (D-098).</summary>
    public readonly struct ChainContext
    {
        public ChainContext(float deltaTime, float flex, Vector3 bodyUp, Vector3 bodyRight, Vector3 bodyForward, Vector3 velocity = default, float groundHeight = 0f)
        {
            DeltaTime = deltaTime;
            Flex = flex;
            BodyUp = bodyUp;
            BodyRight = bodyRight;
            BodyForward = bodyForward;
            Velocity = velocity;
            GroundHeight = groundHeight;
        }

        public float DeltaTime { get; }

        /// <summary>How far a limb reaches, 0 to 1 of its full length: bent at rest, pulled in to wind up, straight on a strike.</summary>
        public float Flex { get; }

        public Vector3 BodyUp { get; }

        public Vector3 BodyRight { get; }

        public Vector3 BodyForward { get; }

        /// <summary>How fast the body moves, in meters per second; feet step ahead of it.</summary>
        public Vector3 Velocity { get; }

        /// <summary>The height of the ground the body stands on; planted feet rest on it.</summary>
        public float GroundHeight { get; }
    }

    /// <summary>Which generated bones a part gets for its kind of motion, and what moves them (D-098, D-099); rigid parts get none.</summary>
    public static class ChainBones
    {
        public const int Tail = 6;

        // Shoulder or hip, elbow or knee, wrist or ankle, at the shares of the limb's length a body has them.
        private static readonly float[] LimbJoints = { 0f, 0.42f, 0.8f };
        private static readonly float[] LegJoints = { 0f, 0.45f, 0.85f };

        /// <summary>The chain for a part placed with this set: a tail of even bones, an arm, or a leg of a pair; null for rigid parts.</summary>
        public static PartSkeleton? SkeletonFor(PartMotion motion, in PartMeshSet placed)
        {
            switch (motion)
            {
                case PartMotion.Tail:
                    return PartSkeleton.For(placed.Intact, placed.Pivot, Tail);
                case PartMotion.Limb:
                    return PartSkeleton.Reaching(placed.Intact, placed.Pivot, LimbJoints);
                case PartMotion.Legs:
                    return placed.Pairing != PartPairing.None ? PartSkeleton.Standing(placed.Intact, placed.UpAxis, LegJoints) : null;
                default:
                    return null;
            }
        }

        /// <summary>The motion that poses the bones of a part; the legs of a pair share one gait so they step in turn.</summary>
        public static IChainMotion? MotionFor(PartMotion motion, Transform part, Transform[] bones, PartSkeleton skeleton, LegGait? gait, int side)
        {
            switch (motion)
            {
                case PartMotion.Tail:
                    return new SpringChain(bones);
                case PartMotion.Limb:
                    return bones.Length >= 3 ? new ReachChain(part, bones) : null;
                case PartMotion.Legs:
                    return bones.Length >= 3 && gait != null ? new LegChain(part, bones, skeleton.Tip, gait, side) : null;
                default:
                    return null;
            }
        }
    }

    /// <summary>The two legs of a pair: one steps while the other stands (D-099).</summary>
    public sealed class LegGait
    {
        private readonly bool[] _stepping = new bool[2];

        /// <summary>True while the other leg of the pair is in the air.</summary>
        public bool OtherStepping(int side)
        {
            return _stepping[1 - Mathf.Clamp(side, 0, 1)];
        }

        public void SetStepping(int side, bool stepping)
        {
            _stepping[Mathf.Clamp(side, 0, 1)] = stepping;
        }
    }

    /// <summary>A procedural motion that poses the generated bones of a part every frame (D-098).</summary>
    public interface IChainMotion
    {
        /// <summary>True when the chain poses the whole part itself, so the body animator leaves the part unturned.</summary>
        bool OwnsPose { get; }

        /// <summary>Poses the bones for this frame; the part itself has already been turned by the body animator.</summary>
        void Update(in ChainContext context);
    }

    /// <summary>
    /// A tail: the first bone follows the part, every later bone is a damped spring that lags behind the one before
    /// it, so a sway or a turn of the body runs down the tail as a whip. Each frame starts from the rest pose, so no
    /// error adds up.
    /// </summary>
    public sealed class SpringChain : IChainMotion
    {
        private const float Stiffness = 140f;
        private const float Damping = 13f;
        private const float MaxStep = 1f / 30f;
        private const float ResetDistance = 4f;
        private const float MaxBendDegrees = 50f;

        private readonly Transform[] _bones;
        private readonly float[] _lengths;
        private readonly Vector3[] _tips;
        private readonly Vector3[] _velocities;
        private bool _started;

        public bool OwnsPose => false;

        public SpringChain(Transform[] bones)
        {
            _bones = bones ?? throw new ArgumentNullException(nameof(bones));
            _lengths = new float[bones.Length];
            _tips = new Vector3[bones.Length];
            _velocities = new Vector3[bones.Length];
            for (int i = 1; i < bones.Length; i++)
            {
                _lengths[i] = bones[i].localPosition.magnitude;
            }
        }

        public void Update(in ChainContext context)
        {
            float dt = Mathf.Min(context.DeltaTime, MaxStep);
            for (int i = 1; i < _bones.Length; i++)
            {
                Transform bone = _bones[i];
                Transform parent = _bones[i - 1];
                bone.localRotation = Quaternion.identity;
                Vector3 start = bone.position;
                float length = _lengths[i] * bone.lossyScale.z;
                Vector3 rest = start + (parent.forward * length);
                if (!_started || dt <= 0f || (_tips[i] - rest).magnitude > length * ResetDistance)
                {
                    _tips[i] = rest;
                    _velocities[i] = Vector3.zero;
                }
                else
                {
                    Vector3 pull = ((rest - _tips[i]) * Stiffness) - (_velocities[i] * Damping);
                    _velocities[i] += pull * dt;
                    _tips[i] += _velocities[i] * dt;
                }

                Vector3 toTip = _tips[i] - start;
                if (toTip.sqrMagnitude < 0.000001f)
                {
                    continue;
                }

                // A joint bends so far and no further, so a hard swing whips the tail instead of folding it.
                toTip = Vector3.RotateTowards(parent.forward, toTip, MaxBendDegrees * Mathf.Deg2Rad, 0f);
                _tips[i] = start + (toTip.normalized * length);
                bone.rotation = Quaternion.FromToRotation(parent.forward, toTip) * parent.rotation;
            }

            _started = true;
        }
    }

    /// <summary>
    /// An arm: two-bone reach from the shoulder along the direction the part points, as far as the flex says, the
    /// elbow bending down and out. The hand follows the forearm. Each frame aims from the rest pose.
    /// </summary>
    public sealed class ReachChain : IChainMotion
    {
        private const float MinimumReach = 0.35f;
        private const float StraightLimit = 0.999f;
        private const float DroopPerBend = 0.6f;

        private readonly Transform _upper;
        private readonly Transform _lower;
        private readonly Transform _hand;
        private readonly Quaternion _upperRest;
        private readonly float _side;

        public bool OwnsPose => false;

        public ReachChain(Transform part, Transform[] bones)
        {
            if (bones == null || bones.Length < 3)
            {
                throw new ArgumentException("An arm needs three bones: upper arm, forearm and hand.", nameof(bones));
            }

            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            _upper = bones[0];
            _lower = bones[1];
            _hand = bones[2];
            _upperRest = _upper.localRotation;

            // Which flank the arm is on, from where the shoulder sits in the part's parent; the elbow points out that way.
            Vector3 shoulder = part.parent != null ? part.parent.InverseTransformPoint(_upper.position) : _upper.position;
            _side = shoulder.x >= 0f ? 1f : -1f;
        }

        public void Update(in ChainContext context)
        {
            _upper.localRotation = _upperRest;
            _lower.localRotation = Quaternion.identity;
            _hand.localRotation = Quaternion.identity;
            Vector3 shoulder = _upper.position;
            Vector3 elbowRest = _lower.position;
            Vector3 wristRest = _hand.position;
            Vector3 handRest = _hand.forward;
            float upper = Vector3.Distance(shoulder, elbowRest);
            float lower = Vector3.Distance(elbowRest, wristRest);
            Vector3 reachDirection = (wristRest - shoulder).normalized;
            float reach = Mathf.Clamp(context.Flex, MinimumReach, StraightLimit) * (upper + lower);
            // A bent arm lets its hand sink, so a relaxed arm hangs instead of holding its claws up.
            float bend = 1f - Mathf.Clamp01(context.Flex);
            Vector3 target = shoulder + (reachDirection * reach) - (context.BodyUp * (bend * DroopPerBend * (upper + lower)));

            Vector3 pole = (-context.BodyUp * 0.6f) + (context.BodyRight * (_side * 0.4f));
            Vector3 elbow = Elbow(shoulder, target, upper, lower, pole);
            _upper.rotation = Quaternion.FromToRotation(elbowRest - shoulder, elbow - shoulder) * _upper.rotation;
            Vector3 forearmNow = _hand.position - _lower.position;
            _lower.rotation = Quaternion.FromToRotation(forearmNow, target - _lower.position) * _lower.rotation;

            // The wrist keeps the hand pointing where it pointed at rest, claws ahead, however the elbow bends.
            _hand.rotation = Quaternion.FromToRotation(_hand.forward, handRest) * _hand.rotation;
        }

        /// <summary>Where the elbow goes for a reach from the shoulder to the target, bent toward the pole direction.</summary>
        public static Vector3 Elbow(Vector3 shoulder, Vector3 target, float upper, float lower, Vector3 pole)
        {
            Vector3 toTarget = target - shoulder;
            float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upper - lower) + 0.0001f, (upper + lower) * StraightLimit);
            Vector3 direction = toTarget.sqrMagnitude > 0f ? toTarget.normalized : Vector3.forward;
            float along = ((upper * upper) + (distance * distance) - (lower * lower)) / (2f * distance);
            float across = Mathf.Sqrt(Mathf.Max((upper * upper) - (along * along), 0f));
            Vector3 bend = pole - (direction * Vector3.Dot(pole, direction));
            if (bend.sqrMagnitude < 0.000001f)
            {
                bend = Vector3.Cross(direction, Vector3.right);
            }

            return shoulder + (direction * along) + (bend.normalized * across);
        }
    }

    /// <summary>
    /// A leg of a pair (D-099): the foot stays planted where it stood while the body moves on, and steps in an arc to
    /// where the hip wants it once it falls too far behind, never while the other foot is in the air. Two-bone reach
    /// from the hip to the ankle with the knee bending forward; the foot keeps its rest direction. Each frame aims
    /// from the rest pose, so no error adds up.
    /// </summary>
    public sealed class LegChain : IChainMotion
    {
        private const float StrideShare = 0.35f;
        private const float LiftShare = 0.18f;
        private const float StepSecondsPerRootMeter = 0.3f;
        private const float MinStepSeconds = 0.12f;
        private const float MaxStepSeconds = 0.45f;
        private const float Lead = 0.6f;
        private const float ResetShare = 4f;

        private readonly Transform _part;
        private readonly Transform _hip;
        private readonly Transform _knee;
        private readonly Transform _ankle;
        private readonly Quaternion _hipRest;
        private readonly Vector3 _soleLocal;
        private readonly LegGait _gait;
        private readonly int _side;
        private Vector3 _plant;
        private Vector3 _from;
        private Vector3 _to;
        private float _stepTime;
        private bool _planted;
        private bool _stepping;

        public LegChain(Transform part, Transform[] bones, Vector3 soleInMesh, LegGait gait, int side)
        {
            if (bones == null || bones.Length < 3)
            {
                throw new ArgumentException("A leg needs three bones: thigh, shin and foot.", nameof(bones));
            }

            _part = part != null ? part : throw new ArgumentNullException(nameof(part));
            _gait = gait ?? throw new ArgumentNullException(nameof(gait));
            _hip = bones[0];
            _knee = bones[1];
            _ankle = bones[2];
            _hipRest = _hip.localRotation;
            _soleLocal = soleInMesh;
            _side = side;
        }

        public bool OwnsPose => true;

        /// <summary>Where the foot stands or lands, in world space; for tests.</summary>
        public Vector3 Foot { get; private set; }

        /// <summary>True while the foot is in the air.</summary>
        public bool Stepping => _stepping;

        public void Update(in ChainContext context)
        {
            _hip.localRotation = _hipRest;
            _knee.localRotation = Quaternion.identity;
            _ankle.localRotation = Quaternion.identity;
            Vector3 hip = _hip.position;
            Vector3 kneeRest = _knee.position;
            Vector3 ankleRest = _ankle.position;
            Vector3 sole = _part.TransformPoint(_soleLocal);
            Vector3 footRest = _ankle.forward;
            float thigh = Vector3.Distance(hip, kneeRest);
            float shin = Vector3.Distance(kneeRest, ankleRest);
            float length = Vector3.Distance(hip, sole);
            float stepSeconds = Mathf.Clamp(StepSecondsPerRootMeter * Mathf.Sqrt(length), MinStepSeconds, MaxStepSeconds);

            // Where the hip wants the foot: under it at rest, on the ground, a little ahead of where the body goes.
            Vector3 flatVelocity = context.Velocity - (context.BodyUp * Vector3.Dot(context.Velocity, context.BodyUp));
            Vector3 wanted = sole + (flatVelocity * (stepSeconds * Lead));
            wanted.y = context.GroundHeight;
            if (!_planted || Flat(_plant - wanted, context.BodyUp) > length * ResetShare)
            {
                _plant = wanted;
                _planted = true;
                _stepping = false;
                _gait.SetStepping(_side, false);
            }

            if (!_stepping && !_gait.OtherStepping(_side) && Flat(_plant - wanted, context.BodyUp) > length * StrideShare)
            {
                _from = _plant;
                _to = wanted;
                _stepTime = 0f;
                _stepping = true;
                _gait.SetStepping(_side, true);
            }

            Vector3 foot = _plant;
            if (_stepping)
            {
                _stepTime += context.DeltaTime / stepSeconds;
                float t = Mathf.Clamp01(_stepTime);
                float eased = t * t * (3f - (2f * t));
                foot = Vector3.Lerp(_from, _to, eased) + (context.BodyUp * (LiftShare * length * Mathf.Sin(t * Mathf.PI)));
                if (_stepTime >= 1f)
                {
                    _plant = _to;
                    foot = _plant;
                    _stepping = false;
                    _gait.SetStepping(_side, false);
                }
            }

            Foot = foot;
            Vector3 ankleTarget = foot + (ankleRest - sole);
            Vector3 knee = ReachChain.Elbow(hip, ankleTarget, thigh, shin, context.BodyForward);
            _hip.rotation = Quaternion.FromToRotation(kneeRest - hip, knee - hip) * _hip.rotation;
            _knee.rotation = Quaternion.FromToRotation(_ankle.position - _knee.position, ankleTarget - _knee.position) * _knee.rotation;
            _ankle.rotation = Quaternion.FromToRotation(_ankle.forward, footRest) * _ankle.rotation;
        }

        private static float Flat(Vector3 offset, Vector3 up)
        {
            return (offset - (up * Vector3.Dot(offset, up))).magnitude;
        }
    }
}
