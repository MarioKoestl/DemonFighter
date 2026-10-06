#nullable enable
namespace DemonFighter.Data
{
    /// <summary>
    /// How one part is drawn as a left and a right copy that move on their own (D-099), as legs do: the model holds
    /// both and the game cuts it in half at its middle, or the model holds one and the game mirrors it for the other
    /// side. The simulation still counts one part; both copies take its damage and fall with it.
    /// </summary>
    public enum PartPairing
    {
        /// <summary>One model, drawn once.</summary>
        None,

        /// <summary>The model holds both sides; each half is drawn and moved on its own.</summary>
        Split,

        /// <summary>The model holds one side; a mirror image is drawn for the other.</summary>
        Mirror,
    }
}
