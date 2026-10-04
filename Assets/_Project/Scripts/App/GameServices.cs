#nullable enable
using System;
using DemonFighter.Data;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Worldgen;
using UnityEngine.InputSystem;

namespace DemonFighter.App
{
    /// <summary>
    /// Composition root. Created once by the bootstrap scene, lives for the whole application and is handed to the
    /// objects that need it. Nothing reaches it through a static (ARCHITECTURE, principle 6). The content catalog
    /// replaces the single biome in M3.
    /// </summary>
    internal sealed class GameServices
    {
        public GameServices(
            SimulationConfig simulationConfig,
            SimulationEvents events,
            BiomeSpec biome,
            IWorldGenerator worldGenerator,
            ContentCatalog catalog,
            ContentCatalogDefinition catalogDefinition,
            IMutationOfferPolicy offerPolicy,
            InputActionAsset actions)
        {
            SimulationConfig = simulationConfig ?? throw new ArgumentNullException(nameof(simulationConfig));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Biome = biome ?? throw new ArgumentNullException(nameof(biome));
            WorldGenerator = worldGenerator ?? throw new ArgumentNullException(nameof(worldGenerator));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            CatalogDefinition = catalogDefinition != null ? catalogDefinition : throw new ArgumentNullException(nameof(catalogDefinition));
            OfferPolicy = offerPolicy ?? throw new ArgumentNullException(nameof(offerPolicy));
            Actions = actions != null ? actions : throw new ArgumentNullException(nameof(actions));
        }

        /// <summary>Tick timing every run starts with.</summary>
        public SimulationConfig SimulationConfig { get; }

        /// <summary>The event bus Presentation, UI and audio subscribe to.</summary>
        public SimulationEvents Events { get; }

        /// <summary>The only biome of v1, loaded from its asset at bootstrap.</summary>
        public BiomeSpec Biome { get; }

        /// <summary>Builds the world of a run from its seed.</summary>
        public IWorldGenerator WorldGenerator { get; }

        /// <summary>Every spec the simulation can refer to, built once from the catalog asset.</summary>
        public ContentCatalog Catalog { get; }

        /// <summary>The catalog asset itself; views read the placeholder visuals of parts from its definitions.</summary>
        public ContentCatalogDefinition CatalogDefinition { get; }

        /// <summary>Which mutations the menu lists: the shop in v1 (GAME_DESIGN, "Mutation").</summary>
        public IMutationOfferPolicy OfferPolicy { get; }

        /// <summary>The input actions asset the player input adapter reads.</summary>
        public InputActionAsset Actions { get; }
    }
}
