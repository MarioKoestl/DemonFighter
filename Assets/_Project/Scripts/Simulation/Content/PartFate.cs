#nullable enable
namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// What happens to a part at zero HP (GAME_DESIGN, "Damage model"): limbs and tails are severed and become food,
    /// hide and eyes are destroyed and vanish. A core at zero HP is death, handled apart from this.
    /// </summary>
    public enum PartFate
    {
        Severed,
        Destroyed,
    }
}
