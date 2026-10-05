#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation.Audio
{
    /// <summary>Which music a moment asks for (D-084).</summary>
    public enum MusicCue
    {
        None,
        Menu,
        Calm,
        Combat,
    }

    /// <summary>The plain rules behind the music choice, kept apart from the director so they can be tested without a scene.</summary>
    public static class MusicCues
    {
        private const float MinimumTickSeconds = 0.0001f;

        /// <summary>Combat while the player fights, calm otherwise, and calm again once the player is dead and the death screen shows.</summary>
        public static MusicCue ForRun(bool playerAlive, bool inCombat)
        {
            return playerAlive && inCombat ? MusicCue.Combat : MusicCue.Calm;
        }

        /// <summary>The combat window of the simulation in ticks, at least one.</summary>
        public static int CombatWindowTicks(float inCombatSeconds, float tickSeconds)
        {
            return Mathf.Max(1, Mathf.RoundToInt(inCombatSeconds / Mathf.Max(tickSeconds, MinimumTickSeconds)));
        }
    }
}
