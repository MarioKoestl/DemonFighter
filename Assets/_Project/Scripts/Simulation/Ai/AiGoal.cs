#nullable enable
namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// What an AI demon is currently pursuing. Resting, wandering and walking a route came with M1, hunting, eating
    /// and fleeing with M2, mutating and evolving with M4 (ARCHITECTURE, "AI"; D-072).
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

        /// <summary>Standing still while a bought, upgraded or regrown part reshapes the body.</summary>
        Mutate,

        /// <summary>Standing still while an evolution reshapes the body.</summary>
        Evolve,
    }
}
