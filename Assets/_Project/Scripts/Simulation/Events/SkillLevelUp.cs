#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A skill reached a new level; the HUD can announce it.</summary>
    public readonly struct SkillLevelUp : ISimulationEvent
    {
        public SkillLevelUp(DemonId demon, string skillId, int newLevel)
        {
            Demon = demon;
            SkillId = skillId;
            NewLevel = newLevel;
        }

        public DemonId Demon { get; }

        public string SkillId { get; }

        public int NewLevel { get; }
    }
}
