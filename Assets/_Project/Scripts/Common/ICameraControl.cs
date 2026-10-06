#nullable enable
using UnityEngine;

namespace DemonFighter.Common
{
    /// <summary>
    /// What input may ask of the player camera. The adapter forwards mouse deltas and the view toggle here and knows
    /// nothing about Cinemachine.
    /// </summary>
    public interface ICameraControl
    {
        /// <summary>Turns the view by a mouse delta in pixels.</summary>
        void AddLook(Vector2 deltaPixels);

        /// <summary>Swaps between third and first person.</summary>
        void ToggleView();

        /// <summary>True in first person; attacks then always go where the camera looks (D-093).</summary>
        bool IsFirstPerson { get; }
    }
}
