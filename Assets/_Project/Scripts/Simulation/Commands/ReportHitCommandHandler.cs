#nullable enable
using System;
using System.Numerics;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Combat;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Skills;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Accepts a hit only when the actor's skill is in its active window, has not hit yet, and the target part is a
    /// living part within reach and arc; then the skill's behaviour applies it and the skill gains XP. Unity cannot
    /// make a hit count that the rules refuse.
    /// </summary>
    internal sealed class ReportHitCommandHandler : ICommandHandler<ReportHitCommand>
    {
        internal const string UnknownActor = "Unknown actor";
        internal const string ActorDead = "Actor is dead";
        internal const string NoActiveSkill = "No skill in its active window";
        internal const string AlreadyHit = "This use already hit";
        internal const string UnknownTarget = "Unknown target";
        internal const string SelfTarget = "Cannot hit yourself";
        internal const string TargetDead = "Target is dead";
        internal const string UnknownPart = "Unknown part";
        internal const string PartLost = "Part is already lost";
        internal const string OutOfReach = "Out of reach";
        internal const string OutsideArc = "Outside the attack arc";

        /// <summary>One tick of grace, because a report detected at the last active tick arrives one tick later.</summary>
        private const long ActiveGraceTicks = 1;

        /// <summary>Slack on top of the arc for the same reason.</summary>
        private const float ArcToleranceDegrees = 15f;

        private readonly DamageSystem _damage;
        private readonly SkillBehaviourRegistry _behaviours;
        private readonly SimulationEvents _events;

        public ReportHitCommandHandler(DamageSystem damage, SkillBehaviourRegistry behaviours, SimulationEvents events)
        {
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
            _behaviours = behaviours ?? throw new ArgumentNullException(nameof(behaviours));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc />
        public CommandResult Handle(in ReportHitCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? actor))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            if (!actor.IsAlive)
            {
                return CommandResult.Rejected(ActorDead);
            }

            SkillUse? use = actor.CurrentSkillUse;
            long tick = state.Tick;
            if (use == null || tick < use.ActiveFromTick || tick > use.ActiveUntilTick + ActiveGraceTicks)
            {
                return CommandResult.Rejected(NoActiveSkill);
            }

            if (use.HitLanded)
            {
                return CommandResult.Rejected(AlreadyHit);
            }

            if (command.Target == actor.Id)
            {
                return CommandResult.Rejected(SelfTarget);
            }

            if (!state.TryGetDemon(command.Target, out Demon? target))
            {
                return CommandResult.Rejected(UnknownTarget);
            }

            if (!target.IsAlive)
            {
                return CommandResult.Rejected(TargetDead);
            }

            if (!target.Body.HasPart(command.PartIndex))
            {
                return CommandResult.Rejected(UnknownPart);
            }

            BodyPart part = target.Body.GetPart(command.PartIndex);
            if (part.IsLost)
            {
                return CommandResult.Rejected(PartLost);
            }

            Vector2 toTarget = new Vector2(target.Position.X - actor.Position.X, target.Position.Z - actor.Position.Z);
            float distance = toTarget.Length();
            float reach = SkillReach.Meters(actor, target, use.Skill);
            if (distance > reach)
            {
                return CommandResult.Rejected(OutOfReach);
            }

            if (distance > 0f)
            {
                // The intended facing counts when there is one: a body still turning toward its aim is aiming already.
                Vector2 facing = actor.Intent.HasFacing ? actor.Intent.Facing : actor.FacingDirection;
                float cosine = Vector2.Dot(facing, toTarget / distance);
                float angle = MathF.Acos(Math.Clamp(cosine, -1f, 1f)) * (180f / MathF.PI);
                if (angle > use.Skill.Spec.ArcDegrees * 0.5f + ArcToleranceDegrees)
                {
                    return CommandResult.Rejected(OutsideArc);
                }
            }

            use.MarkHit();
            ISkillBehaviour behaviour = _behaviours.Get(use.Skill.Spec.BehaviourId);
            behaviour.OnHit(new SkillHitContext(state, _events, _damage, actor, target, part, use.Skill));

            // Harder targets teach more (GAME_DESIGN, "Skill levels"): the same tier gap rule as for kills.
            float xp = use.Skill.Spec.SkillXpPerHit * state.Catalog.Tuning.RewardFactor(actor.Tier, target.Tier);
            if (xp > 0f)
            {
                int levels = use.Skill.GainXp(xp);
                _events.Publish(new SkillXpGained(actor.Id, use.Skill.Spec.Id, xp));
                if (levels > 0)
                {
                    _events.Publish(new SkillLevelUp(actor.Id, use.Skill.Spec.Id, use.Skill.Level));
                }
            }

            return CommandResult.Accepted;
        }
    }
}
