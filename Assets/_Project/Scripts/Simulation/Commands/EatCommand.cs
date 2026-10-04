#nullable enable
namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// One tick of holding Eat on a food item (GAME_DESIGN, "Skills": Eat). The sender repeats it every tick the key
    /// is held; a tick without it ends the meal.
    /// </summary>
    public readonly struct EatCommand : ICommand
    {
        public EatCommand(DemonId actor, FoodId food)
        {
            Actor = actor;
            Food = food;
        }

        /// <inheritdoc />
        public DemonId Actor { get; }

        /// <summary>The corpse or severed part to eat from.</summary>
        public FoodId Food { get; }
    }
}
