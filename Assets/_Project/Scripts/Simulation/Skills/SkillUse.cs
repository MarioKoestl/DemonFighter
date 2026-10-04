#nullable enable
using System;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// A skill being carried out: windup, then the active ticks in which one hit may land, then recovery. The active
    /// window is data on the skill, not baked into an animation (ASSET_PIPELINE, "Animation").
    /// </summary>
    public sealed class SkillUse
    {
        internal SkillUse(SkillInstance skill, long startTick, long activeFromTick, long activeUntilTick, long endTick)
        {
            if (activeFromTick < startTick || activeUntilTick < activeFromTick || endTick < activeUntilTick)
            {
                throw new ArgumentException("Skill use ticks must be ordered: start, active from, active until, end.");
            }

            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            StartTick = startTick;
            ActiveFromTick = activeFromTick;
            ActiveUntilTick = activeUntilTick;
            EndTick = endTick;
        }

        public SkillInstance Skill { get; }

        public long StartTick { get; }

        /// <summary>First tick a hit may land.</summary>
        public long ActiveFromTick { get; }

        /// <summary>Last tick a hit may land, inclusive.</summary>
        public long ActiveUntilTick { get; }

        /// <summary>First tick the demon is free again.</summary>
        public long EndTick { get; }

        /// <summary>True once this use hit something; one hit per use.</summary>
        public bool HitLanded { get; private set; }

        /// <summary>True during the active ticks.</summary>
        public bool IsActive(long tick)
        {
            return tick >= ActiveFromTick && tick <= ActiveUntilTick;
        }

        /// <summary>True once recovery is done.</summary>
        public bool IsOver(long tick)
        {
            return tick >= EndTick;
        }

        internal void MarkHit()
        {
            HitLanded = true;
        }
    }
}
