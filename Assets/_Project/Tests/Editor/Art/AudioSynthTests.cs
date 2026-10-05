#nullable enable
using System.Text;
using AwesomeAssertions;
using DemonFighter.Editor.Generate;
using NUnit.Framework;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Art
{
    public sealed class AudioSynthTests
    {
        [Test]
        public void OneShots_StayInRangeAndEndQuiet()
        {
            float[][] sounds =
            {
                AudioSynth.Bite(1), AudioSynth.Claw(2), AudioSynth.Grab(3), AudioSynth.Lunge(4), AudioSynth.TailSwing(5),
                AudioSynth.WetImpact(6), AudioSynth.Footstep(7), AudioSynth.Eat(9), AudioSynth.MutationStart(10),
                AudioSynth.MutationComplete(11), AudioSynth.Evolved(12), AudioSynth.LevelUp(13), AudioSynth.ThreatRise(14),
                AudioSynth.PlayerDeath(15), AudioSynth.DemonDeath(16), AudioSynth.Sever(17), AudioSynth.PartDestroyed(18), AudioSynth.Burn(23),
            };

            foreach (float[] sound in sounds)
            {
                sound.Length.Should().BeGreaterThan(AudioSynth.SampleRate / 10);
                float peak = 0f;
                foreach (float sample in sound)
                {
                    float.IsNaN(sample).Should().BeFalse();
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }

                peak.Should().BeInRange(0.85f, 0.95f, "every sound is normalized to the same peak");
                Mathf.Abs(sound[sound.Length - 1]).Should().BeLessThan(0.01f, "a one-shot fades out");
            }
        }

        [Test]
        public void Loops_PlayTheirLengthAndCloseWithoutAJump()
        {
            float[][] loops = { AudioSynth.CavernDrone(4f, 19), AudioSynth.LavaLoop(4f, 20), AudioSynth.MenuTrack(4f), AudioSynth.CalmTrack(4f, 21), AudioSynth.CombatTrack(4f, 22) };

            foreach (float[] loop in loops)
            {
                loop.Length.Should().Be(4 * AudioSynth.SampleRate);
                float step = Mathf.Abs(loop[0] - loop[loop.Length - 1]);
                float typical = Mathf.Abs(loop[1] - loop[0]) + Mathf.Abs(loop[loop.Length - 1] - loop[loop.Length - 2]) + 0.05f;
                step.Should().BeLessThan(typical * 3f, "the seam is no bigger than an ordinary sample step");
            }
        }

        [Test]
        public void LoopFrequency_FitsWholeCyclesIntoTheLoop()
        {
            float frequency = AudioSynth.LoopFrequency(82.5f, 12f);

            (frequency * 12f).Should().BeApproximately(Mathf.Round(frequency * 12f), 0.0001f);
            frequency.Should().BeApproximately(82.5f, 0.1f);
        }

        [Test]
        public void SameSeed_SameSound()
        {
            float[] first = AudioSynth.Bite(5);
            float[] second = AudioSynth.Bite(5);

            first.Should().Equal(second);
        }

        [Test]
        public void ToWav_WritesAValidMonoHeader()
        {
            float[] samples = { 0f, 0.5f, -0.5f, 1f };

            byte[] wav = AudioSynth.ToWav(samples, 22050);

            wav.Length.Should().Be(44 + samples.Length * 2);
            Encoding.ASCII.GetString(wav, 0, 4).Should().Be("RIFF");
            Encoding.ASCII.GetString(wav, 8, 4).Should().Be("WAVE");
            Encoding.ASCII.GetString(wav, 36, 4).Should().Be("data");
            System.BitConverter.ToInt16(wav, 22).Should().Be(1, "mono");
            System.BitConverter.ToInt32(wav, 24).Should().Be(22050);
            System.BitConverter.ToInt16(wav, 34).Should().Be(16, "bits per sample");
            System.BitConverter.ToInt32(wav, 40).Should().Be(samples.Length * 2);
            System.BitConverter.ToInt16(wav, 44 + 6).Should().Be(short.MaxValue);
        }

        [Test]
        public void CrossfadeLoop_BlendsTheOverhangIntoTheHeadAndTrimsIt()
        {
            float[] withOverhang = { 1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f };

            float[] loop = AudioSynth.CrossfadeLoop(withOverhang, 2);

            loop.Length.Should().Be(6);
            loop[0].Should().BeApproximately(1f / 3f, 0.001f);
            loop[1].Should().BeApproximately(2f / 3f, 0.001f);
            loop[2].Should().Be(1f);
            loop[5].Should().Be(0f);
        }
    }
}
