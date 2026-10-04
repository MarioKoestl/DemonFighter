#nullable enable
using DemonFighter.Simulation;

namespace DemonFighter.Input
{
    /// <summary>
    /// What the player aims at, as Presentation finds it under the crosshair. The input adapter turns a held Eat key
    /// into eat commands on that food; it never looks into Unity physics itself.
    /// </summary>
    public interface IPlayerAim
    {
        /// <summary>The food under the crosshair and within eat reach, or None.</summary>
        FoodId AimedFood { get; }
    }
}
