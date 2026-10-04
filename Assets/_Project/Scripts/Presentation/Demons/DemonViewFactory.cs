#nullable enable
using System;
using DemonFighter.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Creates the Unity body for a demon from the placeholder prefab, colored by the palette and tuned by the view
    /// settings. The run controller owns the views it gets back.
    /// </summary>
    public sealed class DemonViewFactory
    {
        private readonly DemonView _prefab;
        private readonly PlaceholderPalette _palette;
        private readonly DemonViewSettings _settings;

        public DemonViewFactory(DemonView prefab, PlaceholderPalette palette, DemonViewSettings settings)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _palette = palette != null ? palette : throw new ArgumentNullException(nameof(palette));
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Instantiates and binds a body for the demon under the given parent.</summary>
        public DemonView Spawn(Demon demon, Transform parent)
        {
            DemonView view = Object.Instantiate(_prefab, parent);
            view.name = demon.Spec.Name + " " + demon.Id.Value;
            view.Bind(demon, _settings, _palette.ForDemon(demon));
            return view;
        }
    }
}
