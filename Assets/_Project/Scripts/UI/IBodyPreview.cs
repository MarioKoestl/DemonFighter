#nullable enable
using DemonFighter.Simulation.Anatomy;
using UnityEngine;

namespace DemonFighter.UI
{
    /// <summary>
    /// A rendered body for the mutation menu: the menu composes the body it wants to see (the own parts plus the
    /// offers the player toggles on) and Presentation draws it into the texture. Implemented in the App layer over
    /// the preview rig, so the UI never references views.
    /// </summary>
    public interface IBodyPreview
    {
        /// <summary>The texture the preview renders into; valid for the life of the run.</summary>
        RenderTexture Texture { get; }

        /// <summary>Renders only while active; the menu switches it on with the Mutate tab.</summary>
        void SetActive(bool active);

        /// <summary>Replaces the shown body; the size scales the figure the way a tier does in the world.</summary>
        void Show(Body body, float sizeMeters);

        /// <summary>Turns the figure by the given degrees around its height axis, for drag turning.</summary>
        void Rotate(float degrees);
    }
}
