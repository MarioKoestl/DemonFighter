#nullable enable
namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// How far a skill reaches from one demon to another (ARCHITECTURE, "Movement, collision and hits"): the reach of
    /// the skill per meter of the attacker, stretched by its senses (D-058), plus a share of the target's size for the
    /// radius of its body and a little slack, so a touching hitbox is never refused by rounding. Hit reports and the
    /// aim marker agree because both ask here.
    /// </summary>
    public static class SkillReach
    {
        /// <summary>Share of the target's size that counts as its body radius.</summary>
        public const float TargetRadiusPerMeter = 0.3f;

        /// <summary>Slack on top of the reach.</summary>
        public const float ToleranceMeters = 0.5f;

        /// <summary>Center-to-center ground distance up to which the attacker reaches the target with the skill.</summary>
        public static float Meters(Demon attacker, Demon target, SkillInstance skill)
        {
            return skill.ReachPerMeter * attacker.SizeMeters * attacker.ReachMultiplier + target.SizeMeters * TargetRadiusPerMeter + ToleranceMeters;
        }
    }
}
