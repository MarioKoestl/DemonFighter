#nullable enable
using System;
using DemonFighter.Data;
using DemonFighter.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// Creates the Unity body for a demon from the placeholder prefab, colored by the palette, tuned by the view
    /// settings and grown from the part definitions. The run controller owns the views it gets back.
    /// </summary>
    public sealed class DemonViewFactory
    {
        private readonly DemonView _prefab;
        private readonly DemonViewSettings _settings;
        private readonly PartVisuals _visuals;

        public DemonViewFactory(DemonView prefab, PlaceholderPalette palette, DemonViewSettings settings, ContentCatalogDefinition catalog)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _visuals = new PartVisuals(catalog, palette);
        }

        /// <summary>Instantiates and binds a body for the demon under the given parent.</summary>
        public DemonView Spawn(Demon demon, Transform parent)
        {
            DemonView view = Object.Instantiate(_prefab, parent);
            view.name = demon.Spec.Name + " " + demon.Id.Value;
            view.Bind(demon, _settings, _visuals);
            return view;
        }
    }
}
