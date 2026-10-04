#nullable enable
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Combat;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>Everything a skill behaviour may touch when a hit lands.</summary>
    internal readonly struct SkillHitContext
    {
        public SkillHitContext(RunState state, SimulationEvents events, DamageSystem damage, Demon attacker, Demon target, BodyPart part, SkillInstance skill)
        {
            State = state;
            Events = events;
            Damage = damage;
            Attacker = attacker;
            Target = target;
            Part = part;
            Skill = skill;
        }

        public RunState State { get; }

        public SimulationEvents Events { get; }

        public DamageSystem Damage { get; }

        public Demon Attacker { get; }

        public Demon Target { get; }

        public BodyPart Part { get; }

        public SkillInstance Skill { get; }
    }
}
