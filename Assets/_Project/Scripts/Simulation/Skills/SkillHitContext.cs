#nullable enable
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Combat;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>Everything a skill behaviour may touch when a hit lands.</summary>
    internal readonly struct SkillHitContext
    {
        public SkillHitContext(RunState state, DamageSystem damage, Demon attacker, Demon target, BodyPart part, SkillInstance skill)
        {
            State = state;
            Damage = damage;
            Attacker = attacker;
            Target = target;
            Part = part;
            Skill = skill;
        }

        public RunState State { get; }

        public DamageSystem Damage { get; }

        public Demon Attacker { get; }

        public Demon Target { get; }

        public BodyPart Part { get; }

        public SkillInstance Skill { get; }
    }
}
