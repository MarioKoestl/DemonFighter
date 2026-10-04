#nullable enable
using System.Numerics;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// Lunge (GAME_DESIGN, "Skills"): a short dash that covers the skill distance until the active window closes, and
    /// a Blunt hit that staggers through the strike rules and pushes the target away; a bigger target moves less.
    /// </summary>
    [SkillBehaviour(Id)]
    internal sealed class LungeBehaviour : ISkillBehaviour
    {
        public const string Id = "lunge";

        /// <inheritdoc />
        public void OnActivated(in SkillActivationContext context)
        {
            SkillUse use = context.Use;
            float seconds = (use.ActiveUntilTick + 1 - use.StartTick) * context.State.Config.TickSeconds;
            if (seconds <= 0f || context.Skill.DashMeters <= 0f)
            {
                return;
            }

            Demon actor = context.Actor;
            Vector2 direction = actor.Intent.HasFacing ? actor.Intent.Facing : actor.FacingDirection;
            actor.Push(direction * (context.Skill.DashMeters / seconds), use.ActiveUntilTick + 1);
        }

        /// <inheritdoc />
        public void OnHit(in SkillHitContext context)
        {
            context.Damage.ApplyHit(context.Attacker, context.Target, context.Part, context.Skill.Spec, context.Skill.Level);
            float seconds = context.State.Catalog.Tuning.KnockbackSeconds;
            float meters = context.Skill.KnockbackMeters;
            if (!context.Target.IsAlive || seconds <= 0f || meters <= 0f)
            {
                return;
            }

            Vector3 offset = context.Target.Position - context.Attacker.Position;
            var away = new Vector2(offset.X, offset.Z);
            float distance = away.Length();
            away = distance > 0f ? away / distance : context.Attacker.FacingDirection;

            // A bigger target is pushed less: "can knock back smaller demons" (GAME_DESIGN, "Stagger").
            float ratio = context.Attacker.SizeMeters / context.Target.SizeMeters;
            if (ratio < 1f)
            {
                meters *= ratio * ratio;
            }

            context.Target.Push(away * (meters / seconds), context.State.Tick + context.State.Config.TicksFor(seconds));
        }
    }
}
