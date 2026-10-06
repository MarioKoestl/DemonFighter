#nullable enable
using AwesomeAssertions;
using DemonFighter.Presentation.Audio;
using NUnit.Framework;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class MusicCuesTests
    {
        [Test]
        public void ForRun_FightingPlayer_HearsCombat()
        {
            MusicCues.ForRun(true, true).Should().Be(MusicCue.Combat);
        }

        [Test]
        public void ForRun_RestingPlayer_HearsCalm()
        {
            MusicCues.ForRun(true, false).Should().Be(MusicCue.Calm);
        }

        [Test]
        public void ForRun_DeadPlayer_HearsCalmWhateverTheFight()
        {
            MusicCues.ForRun(false, true).Should().Be(MusicCue.Calm);
            MusicCues.ForRun(false, false).Should().Be(MusicCue.Calm);
        }

        [Test]
        public void CombatWindowTicks_FollowsTheTuningAndTheTick()
        {
            MusicCues.CombatWindowTicks(5f, 0.05f).Should().Be(100);
            MusicCues.CombatWindowTicks(0f, 0.05f).Should().Be(1);
            MusicCues.CombatWindowTicks(5f, 0f).Should().BeGreaterThan(1);
        }
    }
}
