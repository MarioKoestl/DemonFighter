#nullable enable
namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// What an AI demon is currently pursuing. M1 knows resting, wandering and walking a route; hunting, eating,
    /// fleeing, mutating and evolving join with their milestones (ARCHITECTURE, "AI").
    /// </summary>
    public enum AiGoal
    {
        /// <summary>No goal chosen yet; the first decision picks one.</summary>
        None,
        Rest,
        Wander,
        Patrol,
    }
}
