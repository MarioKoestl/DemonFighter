#nullable enable
using System;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Skills;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Starts a skill when the actor may: alive, not staggered, not busy, skill known and still granted, off
    /// cooldown, stamina available (D-023). Timings shrink with Agility; the activation leaves as an event so the
    /// view can animate and run the hit detection in the active window.
    /// </summary>
    internal sealed class UseSkillCommandHandler : ICommandHandler<UseSkillCommand>
    {
        internal const string UnknownActor = "Unknown actor";
        internal const string ActorDead = "Actor is dead";
        internal const string Staggered = "Staggered";
        internal const string Busy = "Already using a skill";
        internal const string UnknownSkill = "Unknown skill";
        internal const string PartLost = "The part granting the skill is lost";
        internal const string Cooldown = "Skill is cooling down";
        internal const string NoStamina = "Not enough stamina";

        private readonly SimulationEvents _events;

        public UseSkillCommandHandler(SimulationEvents events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
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
            int windup = state.Config.TicksFor(spec.WindupSeconds / speed);
            int active = Math.Max(1, state.Config.TicksFor(spec.ActiveSeconds / speed));
            int recovery = state.Config.TicksFor(spec.RecoverySeconds / speed);
            long activeFrom = tick + windup;
            long activeUntil = activeFrom + active - 1;
            long end = activeUntil + 1 + recovery;

            demon.StartSkillUse(new SkillUse(skill, tick, activeFrom, activeUntil, end));
            skill.StartCooldown(tick + state.Config.TicksFor(spec.CooldownSeconds / speed));
            _events.Publish(new SkillActivated(demon.Id, spec.Id, activeFrom, activeUntil, end));
            return CommandResult.Accepted;
        }
    }
}
