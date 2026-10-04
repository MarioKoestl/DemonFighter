#nullable enable
using System;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// One demon's copy of a skill (GAME_DESIGN, "Skill levels"): its level and XP grow by use, its cooldown runs per
    /// demon, and it is only usable while a part that grants it is attached. Levels freeze when that part is lost and
    /// come back with it. The numbers here are the spec scaled by the level and, once reached, by the perk; the
    /// damage rules apply the damage side of both themselves from the spec and the level.
    /// </summary>
    public sealed class SkillInstance
    {
        internal SkillInstance(SkillSpec spec, bool grantedByEvolution = false)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            GrantedByEvolution = grantedByEvolution;
            Level = 1;
        }

        public SkillSpec Spec { get; }

        /// <summary>True for a skill an evolution gave; no part carries it, so no wound can take it away.</summary>
        public bool GrantedByEvolution { get; }

        /// <summary>Skill level, starting at 1 and capped by the level curve.</summary>
        public int Level { get; private set; }

        /// <summary>XP gathered toward the next skill level.</summary>
        public float Xp { get; private set; }

        /// <summary>Tick at which the skill can be used again; -1 before the first use.</summary>
        public long CooldownUntilTick { get; private set; } = -1;

        /// <summary>The perk in effect, or null below its level.</summary>
        public SkillPerkSpec? Perk => Spec.PerkAt(Level);

        /// <summary>True once the perk level is reached.</summary>
        public bool HasPerk => Perk != null;

        /// <summary>XP from the current level to the next; zero at the highest level.</summary>
        public float XpForNextLevel => Level >= Spec.LevelCurve.MaxLevel ? 0f : Spec.LevelCurve.XpForNextLevel(Level);

        /// <summary>Damage factor from the skill level alone; the perk damage is applied by the damage rules.</summary>
        public float DamageMultiplier => 1f + Spec.DamageBonusPerLevel * (Level - 1);

        /// <summary>Stamina one use costs at the current level, never below the floor of the spec.</summary>
        public float StaminaCost => Spec.StaminaCost * CostFraction * (Perk?.StaminaMultiplier ?? 1f);

        /// <summary>Stamina per second a passive skill drains at the current level, never below the floor of the spec.</summary>
        public float StaminaCostPerSecond => Spec.StaminaCostPerSecond * CostFraction * (Perk?.StaminaMultiplier ?? 1f);

        /// <summary>Seconds from one use to the next at the current level.</summary>
        public float CooldownSeconds
        {
            get
            {
                float fraction = MathF.Max(Spec.MinCooldownFraction, 1f - Spec.CooldownReductionPerLevel * (Level - 1));
                return Spec.CooldownSeconds * fraction * (Perk?.CooldownMultiplier ?? 1f);
            }
        }

        public float WindupSeconds => Spec.WindupSeconds / SpeedFactor;

        public float ActiveSeconds => Spec.ActiveSeconds / SpeedFactor;

        public float RecoverySeconds => Spec.RecoverySeconds / SpeedFactor * (Perk?.RecoveryMultiplier ?? 1f);

        /// <summary>Reach in meters per meter of body size at the current level.</summary>
        public float ReachPerMeter => Spec.ReachPerMeter * (1f + Spec.ReachBonusPerLevel * (Level - 1)) * (Perk?.ReachMultiplier ?? 1f);

        public float HoldSeconds => Spec.HoldSeconds * (Perk?.EffectMultiplier ?? 1f);

        public float DashMeters => Spec.DashMeters * (Perk?.EffectMultiplier ?? 1f);

        public float KnockbackMeters => Spec.KnockbackMeters * (Perk?.EffectMultiplier ?? 1f);

        private float CostFraction => MathF.Max(Spec.MinStaminaCostFraction, 1f - Spec.StaminaCostReductionPerLevel * (Level - 1));

        private float SpeedFactor => 1f + Spec.SpeedBonusPerLevel * (Level - 1);

        /// <summary>True while the cooldown still runs at the given tick.</summary>
        public bool IsOnCooldown(long tick)
        {
            return tick < CooldownUntilTick;
        }

        /// <summary>True while any attached part grants the skill; a severed arm takes Claw with it unless another arm has it.</summary>
        public bool IsGrantedBy(Body body)
        {
            return GrantedByEvolution || body.Grants(Spec.Id);
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
