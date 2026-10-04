#nullable enable
namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// What an AI demon is currently pursuing. Resting, wandering and walking a route came with M1, hunting, eating
    /// and fleeing with M2; mutating and evolving join with their milestones (ARCHITECTURE, "AI").
    /// </summary>
    public enum AiGoal
    {
        /// <summary>No goal chosen yet; the first decision picks one.</summary>
        None,
        Rest,
        Wander,
        Patrol,

        /// <summary>Chasing a living demon and biting it when in reach.</summary>
        Hunt,

        /// <summary>Walking to a corpse or severed part and eating it.</summary>
        Eat,

        /// <summary>Sprinting away from a fight while health is low.</summary>
        Flee,
    }
}
