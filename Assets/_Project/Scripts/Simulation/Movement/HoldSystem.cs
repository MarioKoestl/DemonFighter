#nullable enable
using System.Collections.Generic;
using System.Numerics;

namespace DemonFighter.Simulation.Movement
{
    /// <summary>
    /// Drags held demons along with their holder (D-061): every tick a grabbed demon is pushed toward the spot it was
    /// grabbed at, in the frame of the holder, so it swings around as the holder turns and walks. The hold ends when
    /// its time is up, when either side dies or when the holder is gone; the demon then moves on its own again.
    /// </summary>
    internal static class HoldSystem
    {
        /// <summary>Fastest a held demon is yanked toward its spot, so a long grab reels in rather than teleports.</summary>
        private const float MaxPullMetersPerSecond = 12f;

        /// <summary>Pushes every held demon toward its holder for this tick and releases the holds that ended.</summary>
        public static void Advance(RunState state, float tickSeconds)
        {
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (!demon.HeldBy.IsValid)
                {
                    continue;
                }

                if (!demon.IsAlive || !demon.IsHeld(state.Tick) || !state.TryGetDemon(demon.HeldBy, out Demon? holder) || !holder.IsAlive)
                {
                    demon.ReleaseHold();
                    continue;
                }

                Vector2 anchor = Anchor(holder, demon.HeldOffset);
                var delta = new Vector2(anchor.X - demon.Position.X, anchor.Y - demon.Position.Z);
                Vector2 velocity = delta / tickSeconds;
                float speed = velocity.Length();
                if (speed > MaxPullMetersPerSecond)
                {
                    velocity *= MaxPullMetersPerSecond / speed;
                }

                demon.Push(velocity, state.Tick + 1);
            }
        }

        /// <summary>The ground-plane point an offset names: so far to the right and so far ahead of the holder.</summary>
        public static Vector2 Anchor(Demon holder, Vector2 offset)
        {
            Vector2 facing = holder.FacingDirection;
            var right = new Vector2(facing.Y, -facing.X);
            var origin = new Vector2(holder.Position.X, holder.Position.Z);
            return origin + right * offset.X + facing * offset.Y;
        }
    }
}
