#nullable enable
namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// The key a skill is bound to (GAME_DESIGN, "Controls"). Several granted skills may share a slot; the one with
    /// the highest priority takes it, so Claw replaces Bite on the primary attack once an arm exists.
    /// </summary>
    public enum SkillSlot
    {
        None,
        Primary,
        Secondary,
        Lunge,
        TailSwing,
        Sprint,
    }
}
