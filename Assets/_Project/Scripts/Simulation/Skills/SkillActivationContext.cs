#nullable enable
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>Everything a skill behaviour may touch when its skill starts.</summary>
    internal readonly struct SkillActivationContext
    {
        public SkillActivationContext(RunState state, SimulationEvents events, Demon actor, SkillInstance skill, SkillUse use)
        {
            State = state;
            Events = events;
            Actor = actor;
            Skill = skill;
            Use = use;
        }

        public RunState State { get; }

        public SimulationEvents Events { get; }

        public Demon Actor { get; }

        public SkillInstance Skill { get; }

        /// <summary>The use that just began, with its windup, active and recovery ticks.</summary>
        public SkillUse Use { get; }
    }
}
