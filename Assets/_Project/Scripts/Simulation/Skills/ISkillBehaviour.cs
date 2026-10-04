#nullable enable
namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// What a skill does when it starts and when it connects. Most skills are the data-driven melee strike; skills
    /// with their own rules (Grab, Lunge) get their own class, tagged with <see cref="SkillBehaviourAttribute"/>.
    /// Passive skills such as Sprint have no behaviour; systems read their data directly.
    /// </summary>
    internal interface ISkillBehaviour
    {
        /// <summary>Runs when the skill starts; a lunge begins its leap here. Most skills do nothing until they hit.</summary>
        void OnActivated(in SkillActivationContext context)
        {
        }

        /// <summary>Applies the effect of one landed hit.</summary>
        void OnHit(in SkillHitContext context);
    }
}
