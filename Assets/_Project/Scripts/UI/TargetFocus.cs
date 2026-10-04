#nullable enable
using DemonFighter.Simulation;

namespace DemonFighter.UI
{
    /// <summary>
    /// What the crosshair of the player rests on and what the Analyze key locked (D-065, D-066): the HUD reads it
    /// every frame through a callback the App layer composes from the combat presenter, so UI never references views.
    /// </summary>
    public readonly struct TargetFocus
    {
        public TargetFocus(DemonId focused, int partIndex, bool inReach, DemonId locked)
        {
            Focused = focused;
            PartIndex = partIndex;
            InReach = inReach;
            Locked = locked;
        }

        /// <summary>The demon under the crosshair, or None.</summary>
        public DemonId Focused { get; }

        /// <summary>Index of the body part under the crosshair; -1 when none.</summary>
        public int PartIndex { get; }

        /// <summary>True when the primary skill of the player reaches the focused demon from where it stands.</summary>
        public bool InReach { get; }

        /// <summary>The demon the Analyze key locked, or None.</summary>
        public DemonId Locked { get; }

        /// <summary>Nothing under the crosshair and nothing locked.</summary>
        public static TargetFocus None => new TargetFocus(DemonId.None, -1, false, DemonId.None);
    }
}
