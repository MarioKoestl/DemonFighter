#nullable enable
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// Grab (GAME_DESIGN, "Skills"): the hit follows the strike rules, which deal no damage by default, and a smaller
    /// demon no bigger than the attacker is held for the hold time and dragged along (D-061, D-065), so a Bite or Claw
    /// can follow. Bigger demons shrug it off.
    /// </summary>
    [SkillBehaviour(Id)]
    internal sealed class GrabBehaviour : ISkillBehaviour
    {
        public const string Id = "grab";

        /// <inheritdoc />
        public void OnHit(in SkillHitContext context)
        {
            context.Damage.ApplyHit(context.Attacker, context.Target, context.Part, context.Skill.Spec, context.Skill.Level);
            if (!context.Target.IsAlive || context.Target.SizeMeters > context.Attacker.SizeMeters || context.Skill.HoldSeconds <= 0f)
            {
                return;
            }

            long untilTick = context.State.Tick + context.State.Config.TicksFor(context.Skill.HoldSeconds);
            context.Target.Hold(untilTick, context.Attacker);
            context.Events.Publish(new DemonHeld(context.Target.Id, context.Attacker.Id, untilTick));
        }
    }
}
