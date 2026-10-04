#nullable enable
namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// The plain melee hit that Bite, Claw and Tail Swing share: damage, bleeding and stagger exactly as the skill
    /// data says, through the damage rules.
    /// </summary>
    [SkillBehaviour(Id)]
    internal sealed class MeleeStrikeBehaviour : ISkillBehaviour
    {
        public const string Id = "melee-strike";

        /// <inheritdoc />
        public void OnHit(in SkillHitContext context)
        {
            context.Damage.ApplyHit(context.Attacker, context.Target, context.Part, context.Skill.Spec, context.Skill.Level);
        }
    }
}
