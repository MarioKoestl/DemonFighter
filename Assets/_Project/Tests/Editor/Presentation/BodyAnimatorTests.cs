#nullable enable
using AwesomeAssertions;
using DemonFighter.Data;
using DemonFighter.Presentation.Demons;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class BodyAnimatorTests
    {
        private const float Tolerance = 0.002f;
        private const float MaxSpeed = 4f;
        private const float Size = 1f;
        private const float Step = 0.02f;

        private BodyAnimationTuning _tuning = null!;
        private BodyAnimator _animator = null!;

        [SetUp]
        public void CreateAnimator()
        {
            _tuning = new BodyAnimationTuning();
            _animator = new BodyAnimator(_tuning);
        }

        [Test]
        public void FigureScale_AtRest_BreathesAroundOne()
        {
            Advance(_tuning.BreathSeconds * 0.25f, 0f);

            Vector3 scale = _animator.FigureScale();

            scale.y.Should().BeApproximately(1f + _tuning.BreathAmplitude, Tolerance);
            scale.x.Should().BeApproximately(1f + _tuning.BreathAmplitude * 0.5f, Tolerance);
            scale.z.Should().BeApproximately(1f - _tuning.BreathAmplitude * 0.5f, Tolerance);
        }

        [Test]
        public void FigureScale_Running_StretchesAlongTheRunAndSquashesUp()
        {
            Advance(3f, MaxSpeed);

            Vector3 scale = _animator.FigureScale();

            _animator.SpeedFraction.Should().BeApproximately(1f, Tolerance);
            scale.z.Should().BeGreaterThan(1f + _tuning.StretchPerSpeed * 0.8f);
            scale.y.Should().BeLessThan(1f - _tuning.StretchPerSpeed * 0.4f);
            scale.z.Should().BeGreaterThan(scale.y);
        }

        [Test]
        public void FigureBob_IsZeroAtRestAndPositiveOnTheMove()
        {
            Advance(1f, 0f);
            float resting = _animator.FigureBob();
            Advance(1.37f, MaxSpeed);
            float moving = _animator.FigureBob();

            resting.Should().Be(0f);
            moving.Should().BeGreaterThan(0f);
        }

        [Test]
        public void PartRotation_Legs_StandStillAndSwingAgainstEachOtherOnTheMove()
        {
            Advance(1f, 0f);
            Quaternion still = _animator.PartRotation(PartMotion.Legs, 0);
            Advance(2.13f, MaxSpeed);
            float left = Pitch(_animator.PartRotation(PartMotion.Legs, 0));
            float right = Pitch(_animator.PartRotation(PartMotion.Legs, 1));

            Quaternion.Angle(still, Quaternion.identity).Should().BeLessThan(0.01f);
            Mathf.Abs(left).Should().BeGreaterThan(1f);
            right.Should().BeApproximately(-left, 0.1f);
        }

        [Test]
        public void Attack_Bite_OpensTheJawsThenSnapsShutAndSettles()
        {
            _animator.Attack(SkillMotion.Bite, 0.2f, 0.2f, 0.3f);

            Advance(0.1f, 0f);
            float open = Pitch(_animator.PartRotation(PartMotion.Jaws, 0));
            Advance(0.29f, 0f);
            float snapped = Pitch(_animator.PartRotation(PartMotion.Jaws, 0));
            Advance(0.4f, 0f);
            Quaternion settled = _animator.PartRotation(PartMotion.Jaws, 0);

            open.Should().BeLessThan(-_tuning.JawOpenDegrees * 0.3f);
            snapped.Should().BeGreaterThan(_tuning.JawSnapDegrees * 0.5f);
            Quaternion.Angle(settled, Quaternion.identity).Should().BeLessThan(0.01f);
            _animator.Attacking.Should().Be(SkillMotion.None);
        }

        [Test]
        public void Attack_Bite_SwellsAndNodsTheBody()
        {
            _animator.Attack(SkillMotion.Bite, 0.2f, 0.2f, 0.2f);

            Advance(0.3f, 0f);

            _animator.FigureScale().x.Should().BeGreaterThan(1f + _tuning.AttackContraction * 0.5f);
            Pitch(_animator.FigureRotation()).Should().BeGreaterThan(_tuning.BiteNodDegrees * 0.5f);
        }

        [Test]
        public void Attack_Swipe_SwingsTheArmAndLeavesTheJawsAlone()
        {
            _animator.Attack(SkillMotion.Swipe, 0.2f, 0.2f, 0.3f);

            Advance(0.1f, 0f);
            float raised = Pitch(_animator.PartRotation(PartMotion.Limb, 0));
            Quaternion jaws = _animator.PartRotation(PartMotion.Jaws, 0);
            Advance(0.29f, 0f);
            float struck = Pitch(_animator.PartRotation(PartMotion.Limb, 0));

            raised.Should().BeLessThan(-_tuning.ArmRaiseDegrees * 0.3f);
            struck.Should().BeGreaterThan(_tuning.ArmStrikeDegrees * 0.5f);
            Quaternion.Angle(jaws, Quaternion.identity).Should().BeLessThan(0.01f);
        }

        [Test]
        public void Attack_Lunge_StretchesTheBodyForward()
        {
            _animator.Attack(SkillMotion.Lunge, 0.1f, 0.3f, 0.2f);

            Advance(0.3f, 0f);

            Vector3 scale = _animator.FigureScale();
            scale.z.Should().BeGreaterThan(1f + _tuning.LungeStretch * 0.5f);
        }

        [Test]
        public void PartRotation_Tail_SwaysSidewaysAndWhipsOnATailSwing()
        {
            Advance(_tuning.TailSwaySeconds * 0.25f, 0f);
            float sway = Yaw(_animator.PartRotation(PartMotion.Tail, 0));
            _animator.Attack(SkillMotion.TailSwing, 0.1f, 0.1f, 0.1f);
            Advance(0.15f, 0f);
            float whip = Yaw(_animator.PartRotation(PartMotion.Tail, 0));

            sway.Should().BeApproximately(_tuning.TailSwayDegrees, 0.5f);
            Mathf.Abs(whip).Should().BeGreaterThan(_tuning.TailSwingDegrees * 0.5f);
        }

        [Test]
        public void Hit_WobblesTheBodyThenSettles()
        {
            _animator.Hit(1f);

            Advance(_tuning.HitWobbleSeconds / 6f, 0f);
            Quaternion shaken = _animator.FigureRotation();
            Advance(_tuning.HitWobbleSeconds, 0f);
            Quaternion settled = _animator.FigureRotation();

            Quaternion.Angle(shaken, Quaternion.identity).Should().BeGreaterThan(1f);
            Quaternion.Angle(settled, Quaternion.identity).Should().BeLessThan(0.01f);
        }

        [Test]
        public void Transform_ThrobsForTheDurationThenStops()
        {
            _animator.Transform(1f);

            Advance(1f / 6f, 0f);
            float throbbing = _animator.FigureScale().x;
            Advance(1f, 0f);
            float after = _animator.FigureScale().x;

            throbbing.Should().BeGreaterThan(1f + _tuning.TransformationPulse * 0.8f);
            after.Should().BeLessThan(1f + _tuning.BreathAmplitude + Tolerance);
        }

        [Test]
        public void PartRotation_UnmovingParts_StayAtRest()
        {
            _animator.Attack(SkillMotion.Bite, 0.1f, 0.1f, 0.1f);
            Advance(0.15f, MaxSpeed);

            Quaternion rotation = _animator.PartRotation(PartMotion.None, 0);

            Quaternion.Angle(rotation, Quaternion.identity).Should().BeLessThan(0.01f);
        }

        private void Advance(float seconds, float speed)
        {
            for (float elapsed = 0f; elapsed < seconds - Step * 0.5f; elapsed += Step)
            {
                _animator.Advance(Step, speed, MaxSpeed, Size);
            }
        }

        private static float Pitch(Quaternion rotation)
        {
            float pitch = rotation.eulerAngles.x;
            return pitch > 180f ? pitch - 360f : pitch;
        }

        private static float Yaw(Quaternion rotation)
        {
            float yaw = rotation.eulerAngles.y;
            return yaw > 180f ? yaw - 360f : yaw;
        }
    }
}
