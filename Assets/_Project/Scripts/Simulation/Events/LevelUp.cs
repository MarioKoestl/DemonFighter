#nullable enable
namespace DemonFighter.Simulation.Events
{
    /// <summary>A demon reached a new character level and has stat points to spend; the HUD announces it.</summary>
    public readonly struct LevelUp : ISimulationEvent
    {
        public LevelUp(DemonId demon, int newLevel, int unspentStatPoints)
        {
            Demon = demon;
            NewLevel = newLevel;
            UnspentStatPoints = unspentStatPoints;
        }

        public DemonId Demon { get; }

        /// <summary>The level reached; several levels from one gain report only the last.</summary>
        public int NewLevel { get; }

        /// <summary>Points waiting in the Stats tab after this level.</summary>
        public int UnspentStatPoints { get; }
    }
}
