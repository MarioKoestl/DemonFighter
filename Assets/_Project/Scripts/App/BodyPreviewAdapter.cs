#nullable enable
using System;
using DemonFighter.Presentation.Demons;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.UI;
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// Hands the preview rig of Presentation to the UI behind its own interface, so UI keeps its reference graph
    /// (ARCHITECTURE, "Assemblies and folders"). Calls after the rig died with its scene are ignored.
    /// </summary>
    internal sealed class BodyPreviewAdapter : IBodyPreview
    {
        private readonly BodyPreviewRig _rig;

        public BodyPreviewAdapter(BodyPreviewRig rig)
        {
            _rig = rig != null ? rig : throw new ArgumentNullException(nameof(rig));
        }

        /// <inheritdoc />
        public RenderTexture Texture => _rig.Texture;

        /// <inheritdoc />
        public void SetActive(bool active)
        {
            if (_rig != null)
            {
                _rig.SetActive(active);
            }
        }

        /// <inheritdoc />
        public void Show(Body body, float sizeMeters)
        {
            if (_rig != null)
            {
                _rig.Show(body, sizeMeters);
            }
        }

        /// <inheritdoc />
        public void Rotate(float degrees)
        {
            if (_rig != null)
            {
                _rig.Rotate(degrees);
            }
        }
    }
}
