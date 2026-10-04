#nullable enable
namespace DemonFighter.Simulation.Food
{
    /// <summary>Why a food item left the world.</summary>
    public enum FoodRemovalReason
    {
        /// <summary>Eaten to the last bit of Biomass.</summary>
        Eaten,

        /// <summary>Rotted away after the decay time.</summary>
        Decayed,
    }
}
