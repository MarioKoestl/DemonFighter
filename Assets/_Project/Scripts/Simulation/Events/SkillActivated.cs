#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// A demon started a skill. The view plays the motion and runs hit detection during the active ticks, then
    /// reports what it touched.
    /// </summary>
    public readonly struct SkillActivated : ISimulationEvent
    {
        public SkillActivated(DemonId actor, string skillId, long activeFromTick, long activeUntilTick, long endTick)
        {
            Actor = actor;
            SkillId = skillId;
            ActiveFromTick = activeFromTick;
            ActiveUntilTick = activeUntilTick;
            EndTick = endTick;
        }

        public DemonId Actor { get; }

        public string SkillId { get; }

        /// <summary>First tick a hit may land.</summary>
        public long ActiveFromTick { get; }

        /// <summary>Last tick a hit may land, inclusive.</summary>
        public long ActiveUntilTick { get; }

        /// <summary>First tick the demon is free again.</summary>
        public long EndTick { get; }
    }
}
