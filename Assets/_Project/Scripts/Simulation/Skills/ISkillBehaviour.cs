#nullable enable
namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// What a skill does when it connects. Most skills are the data-driven melee strike; skills with their own rules
    /// (Grab, Lunge) get their own class, tagged with <see cref="SkillBehaviourAttribute"/>.
    /// </summary>
    internal interface ISkillBehaviour
    {
        /// <summary>Applies the effect of one landed hit.</summary>
        void OnHit(in SkillHitContext context);
    }
}
