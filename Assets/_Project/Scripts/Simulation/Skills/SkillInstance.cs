#nullable enable
using System;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// One demon's copy of a skill (GAME_DESIGN, "Skill levels"): its level and XP grow by use, its cooldown runs per
    /// demon, and it is only usable while the part that grants it is still attached. Levels freeze when that part
    /// is lost and come back with it.
    /// </summary>
    public sealed class SkillInstance
    {
        internal SkillInstance(SkillSpec spec, int grantedByPartIndex)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            if (grantedByPartIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(grantedByPartIndex), grantedByPartIndex, "Part indices are never negative.");
            }

            GrantedByPartIndex = grantedByPartIndex;
            Level = 1;
        }

        public SkillSpec Spec { get; }

        /// <summary>Index of the body part that grants this skill.</summary>
        public int GrantedByPartIndex { get; }

        /// <summary>Skill level, starting at 1 and capped by the level curve.</summary>
        public int Level { get; private set; }

        /// <summary>XP gathered toward the next skill level.</summary>
        public float Xp { get; private set; }

        /// <summary>Tick at which the skill can be used again; -1 before the first use.</summary>
        public long CooldownUntilTick { get; private set; } = -1;

        /// <summary>Damage factor from the skill level.</summary>
        public float DamageMultiplier => 1f + Spec.DamageBonusPerLevel * (Level - 1);

        /// <summary>Stamina one use costs at the current level, never below the spec's floor.</summary>
        public float StaminaCost
        {
            get
            {
                float fraction = MathF.Max(Spec.MinStaminaCostFraction, 1f - Spec.StaminaCostReductionPerLevel * (Level - 1));
                return Spec.StaminaCost * fraction;
            }
        }

        /// <summary>True while the cooldown still runs at the given tick.</summary>
        public bool IsOnCooldown(long tick)
        {
            return tick < CooldownUntilTick;
        }

        /// <summary>True while the granting part is attached; a severed arm takes Claw and Grab with it.</summary>
        public bool IsGrantedBy(Body body)
        {
            return body.HasPart(GrantedByPartIndex) && !body.GetPart(GrantedByPartIndex).IsLost;
        }

        internal void StartCooldown(long untilTick)
        {
            CooldownUntilTick = untilTick;
        }

        /// <summary>Adds skill XP and returns how many levels it bought; nothing beyond the max level.</summary>
        internal int GainXp(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Skill XP gains are never negative.");
            }

            Xp += amount;
            int gained = 0;
            while (Level < Spec.LevelCurve.MaxLevel && Xp >= Spec.LevelCurve.XpForNextLevel(Level))
            {
                Xp -= Spec.LevelCurve.XpForNextLevel(Level);
                Level++;
                gained++;
            }

            return gained;
        }
    }
}
