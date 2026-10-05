#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Presentation.Audio;
using DemonFighter.Presentation.Rendering;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Progression;
using DemonFighter.Simulation.Worldgen;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonFighter.App
{
    /// <summary>
    /// Entry point of the application and the only object in the Bootstrap scene. Loads the content it references,
    /// builds the composition root once, survives scene loads and opens the main menu. Nothing else may use
    /// DontDestroyOnLoad (CODING_GUIDELINES).
    /// </summary>
    internal sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private ContentCatalogDefinition _catalog = null!;
        [SerializeField] private BiomeDefinition _biome = null!;
        [SerializeField] private InputActionAsset _actions = null!;
        [SerializeField] private AudioCatalogDefinition _audio = null!;
        [SerializeField] private GraphicsPresetCatalog _graphicsPresets = null!;

        private RunController? _runController;
        private SceneFlow? _sceneFlow;

        /// <summary>The run controller of this application, for Play Mode tests; null when bootstrapping failed.</summary>
        internal RunController? RunController => _runController;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (_catalog == null || _biome == null || _actions == null || _audio == null || _graphicsPresets == null)
            {
                Log.Error(LogCategory.App, "Bootstrapper is missing its catalog, biome, actions, audio or graphics presets reference; run Demon Fighter > Generate > Scenes.", this);
                return;
            }

            try
            {
                // The music outlives runs and scenes, so its player hangs under this persistent object (D-084).
                var mix = new AudioMix();
                var musicObject = new GameObject("Music");
                musicObject.transform.SetParent(transform, false);
                MusicPlayer music = musicObject.AddComponent<MusicPlayer>();
                music.Configure(mix, _audio.MusicFadeSeconds);

                // The settings file is read once and pushed into graphics, screen and audio before anything shows (D-085).
                var settings = new GameSettings();
                var applier = new SettingsApplier(settings, _graphicsPresets, mix);
                applier.ApplyAll();
                var services = new GameServices(
                    SimulationConfig.Default,
                    new SimulationEvents(),
                    _biome.ToSpec(),
                    new CavernWorldGenerator(),
                    _catalog.Build(),
                    _catalog,
                    new ShopOfferPolicy(),
                    new RunSaveService(),
                    new NoMetaProgression(),
                    new RandomOfferPolicy(),
                    settings,
                    _actions,
                    _audio,
                    _biome,
                    mix,
                    music,
                    _graphicsPresets,
                    applier);
                _runController = new RunController(services);
                _sceneFlow = new SceneFlow(_runController, services.Applier, services.Music, services.Audio.MenuTrack);
                Log.Info(LogCategory.App, "Bootstrap complete with biome " + services.Biome.Id + " and " + services.Catalog.Demons.Count + " demon kinds.");
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Bootstrap failed.", exception, this);
            }
        }

        private async void Start()
        {
            if (_sceneFlow == null)
            {
                return;
            }

            try
            {
                await _sceneFlow.ShowMainMenuAsync();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Opening the main menu failed.", exception, this);
            }
        }

        private void OnDestroy()
        {
            if (_runController != null)
            {
                _runController.EndRun();
            }
        }
    }
}
