#nullable enable
using System;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Skills;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Starts a skill when the actor may: alive, not transforming, not held, not staggered, not busy, skill known,
    /// active and still granted, off cooldown, stamina available (D-023). Timings shrink with Agility; the activation
    /// leaves as an event so the view can animate and run the hit detection in the active window, and the behaviour
    /// of the skill gets its activation call (a lunge starts its leap there).
    /// </summary>
    internal sealed class UseSkillCommandHandler : ICommandHandler<UseSkillCommand>
    {
        internal const string UnknownActor = "Unknown actor";
        internal const string ActorDead = "Actor is dead";
        internal const string Transforming = "Transforming";
        internal const string Held = "Held";
        internal const string Staggered = "Staggered";
        internal const string Busy = "Already using a skill";
        internal const string UnknownSkill = "Unknown skill";
        internal const string Passive = "Not an active skill";
        internal const string PartLost = "The part granting the skill is lost";
        internal const string Cooldown = "Skill is cooling down";
        internal const string NoStamina = "Not enough stamina";

        private readonly SimulationEvents _events;
        private readonly SkillBehaviourRegistry _behaviours;

        public UseSkillCommandHandler(SimulationEvents events, SkillBehaviourRegistry behaviours)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _behaviours = behaviours ?? throw new ArgumentNullException(nameof(behaviours));
        }

        /// <inheritdoc />
        public CommandResult Handle(in UseSkillCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? demon))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            if (!demon.IsAlive)
            {
                return CommandResult.Rejected(ActorDead);
            }

            long tick = state.Tick;
            if (demon.IsTransforming(tick))
            {
                return CommandResult.Rejected(Transforming);
            }

            if (demon.IsHeld(tick))
            {
                return CommandResult.Rejected(Held);
            }

            if (demon.IsStaggered(tick))
            {
                return CommandResult.Rejected(Staggered);
            }

            if (demon.CurrentSkillUse != null)
            {
                return CommandResult.Rejected(Busy);
            }

            SkillInstance? skill = demon.FindSkill(command.SkillId);
            if (skill == null)
            {
                return CommandResult.Rejected(UnknownSkill);
            }

            if (skill.Spec.IsPassive)
            {
                return CommandResult.Rejected(Passive);
            }

            if (!skill.IsGrantedBy(demon.Body))
            {
                return CommandResult.Rejected(PartLost);
            }

            if (skill.IsOnCooldown(tick))
            {
                return CommandResult.Rejected(Cooldown);
            }

            if (!demon.TrySpendStamina(skill.StaminaCost))
            {
                return CommandResult.Rejected(NoStamina);
            }

            SkillSpec spec = skill.Spec;
            float speed = demon.Derived.AttackSpeedMultiplier;
            int windup = state.Config.TicksFor(skill.WindupSeconds / speed);
            int active = Math.Max(1, state.Config.TicksFor(skill.ActiveSeconds / speed));
            int recovery = state.Config.TicksFor(skill.RecoverySeconds / speed);
            long activeFrom = tick + windup;
            long activeUntil = activeFrom + active - 1;
            long end = activeUntil + 1 + recovery;

            var use = new SkillUse(skill, tick, activeFrom, activeUntil, end);
            demon.StartSkillUse(use);
            skill.StartCooldown(tick + state.Config.TicksFor(skill.CooldownSeconds / speed));
            _events.Publish(new SkillActivated(demon.Id, spec.Id, activeFrom, activeUntil, end));
            _behaviours.Get(spec.BehaviourId).OnActivated(new SkillActivationContext(state, _events, demon, skill, use));
            return CommandResult.Accepted;
        }
    }
}
