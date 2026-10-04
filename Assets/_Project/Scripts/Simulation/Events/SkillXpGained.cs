#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A skill gained XP from use; the Skills tab shows the progress.</summary>
    public readonly struct SkillXpGained : ISimulationEvent
    {
        public SkillXpGained(DemonId demon, string skillId, float amount)
        {
            Demon = demon;
            SkillId = skillId;
            Amount = amount;
        }

        public DemonId Demon { get; }

        public string SkillId { get; }

        public float Amount { get; }
    }
}
